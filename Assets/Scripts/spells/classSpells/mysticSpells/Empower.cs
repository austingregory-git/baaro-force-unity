using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Empower — multiply the damage of your next basic attack by 2 + (0.25 * Level) and make it deal a random elemental damage type.
    ///
    /// Damage type is rolled once per cast (on first description/preview lookup) and reused
    /// for every subsequent lookup and for Execute, same reasoning as Magic Dart's
    /// ResolveType — so the tooltip never lies about what the buffed attack will do. It's
    /// cleared after Execute so the next cast rolls afresh.
    /// </summary>
    public class Empower : ClassSpell
    {
        private SpellType? _resolvedType;

        public Empower() : base(
            characterClass: ClassRegistry.Get("Mystic"),
            name:        "Empower",
            description: "Empower your weapon, multiplying the damage of your next basic attack by {0} and making it deal a random elemental damage type. This does not cost an action point.",
            manaCost:        2,
            actionPointCost: 0,
            range:       0,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Self,
            type:        SpellType.Buff)
        { }

        /// <summary>Resolves (and caches) the damage type for the cast currently in
        /// progress, so description, preview and Execute all agree.</summary>
        private SpellType ResolveType(Character caster)
        {
            if (!_resolvedType.HasValue)
                _resolvedType = caster.GetRandomSpellType();
            return _resolvedType.Value;
        }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var multiplier = new ScalingValue("Damage")
                .Add("Base", 2)
                .Add("Level", Mathf.FloorToInt(caster.Level * 0.25f));
            return new[] { multiplier };
        }

        public override string GetSummary(Character caster) =>
            ScalingDescriptionFormatter.GetSummary(DescriptionFor(ResolveType(caster)), ComputeValues(caster));

        public override string GetDetailedDescription(Character caster) =>
            ScalingDescriptionFormatter.GetDetailedDescription(DescriptionFor(ResolveType(caster)), ComputeValues(caster));

        private static string DescriptionFor(SpellType type) =>
            $"Empower your weapon, multiplying the damage of your next basic attack by {{0}} and making it deal [{type}] damage. This does not cost an action point.";

        /// <summary>Highlights using the resolved (locked-in) roll for this cast — see <see cref="ResolveType"/>.</summary>
        public override SpellType? GetHighlightType(Character caster) => ResolveType(caster);

        /// <summary>Self-only — <paramref name="target"/> is always the caster.</summary>
        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview
            {
                StatusEffectName = "Empower",
                StatusEffectKind = StatusEffect.StatusEffectType.Buff,
            };

        public override bool Execute(SpellContext context)
        {
            int multiplier = ComputeValues(context.Caster)[0].Total;
            SpellType type = ResolveType(context.Caster);
            context.Caster.ApplyStatus(new EmpowerStatus(multiplier, type));

            Debug.Log($"[Empower] '{context.Caster.CharacterName}' empowers their weapon, " +
                      $"multiplying the damage of their next basic attack by {multiplier} and making it deal [{type}] damage.");

            // Clear the cached roll so the next cast picks a fresh damage type.
            _resolvedType = null;
            return true;
        }
    }
}
