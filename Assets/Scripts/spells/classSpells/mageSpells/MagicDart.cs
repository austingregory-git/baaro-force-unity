using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Magic Dart — deal 3 + 1.5*SpellPower damage (type is based on character's realm) to a single target within 3 tiles.
    ///
    /// Damage type matches one of the caster's realms. Single-realm casters always deal
    /// that realm's damage type; multi-realm casters roll a random realm each cast. The
    /// roll is resolved once per cast (on first description/preview lookup) and reused for
    /// every subsequent lookup and for Execute, so the tooltip never lies about what a
    /// pending cast will do. It's cleared after Execute so the next cast rolls afresh.
    /// </summary>
    public class MagicDart : ClassSpell
    {
        private SpellType? _resolvedType;

        public MagicDart() : base(
            characterClass: ClassRegistry.Get("Mage"),
            name:        "Magic Dart",
            description: "Deal {0} damage of a random elemental type.",
            manaCost:        3,
            actionPointCost: 1,
            range:       3,
            area:        0,
            cooldown:    0,
            targetType:  SpellTargetType.Enemy)
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
            var damage = new ScalingValue("Damage")
                .Add("Base", 3)
                .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 1.5, floored)", Mathf.FloorToInt(caster.CharacterStats.SpellPowerBonus * 1.5f));
            return new[] { damage };
        }

        public override string GetSummary(Character caster) =>
            ScalingDescriptionFormatter.GetSummary(DescriptionFor(ResolveType(caster)), ComputeValues(caster));

        public override string GetDetailedDescription(Character caster) =>
            ScalingDescriptionFormatter.GetDetailedDescription(DescriptionFor(ResolveType(caster)), ComputeValues(caster));

        private static string DescriptionFor(SpellType type) => $"Deal {{0}} [{type}] damage.";

        /// <summary>Highlights using the resolved (locked-in) roll for this cast, so the
        /// selectable-range/preview colour always matches what the tooltip already promised —
        /// same reasoning as <see cref="ResolveType"/> itself.</summary>
        public override SpellType? GetHighlightType(Character caster) => ResolveType(caster);

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            bool casterIsNpc = context.Caster is Npc;

            // From an Npc's perspective the enemy is a player Character; from a
            // player Character's perspective the enemy is an Npc.
            Character target = casterIsNpc
                ? context.TargetTile?.OccupyingCharacter
                : context.TargetTile?.OccupyingNpc;

            if (target == null)
            {
                Debug.LogWarning("[MagicDart] No target found on the selected tile.");
                return false;
            }

            SpellType type = ResolveType(context.Caster);
            int damage     = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, type, "Magic Dart");

            Debug.Log($"[Magic Dart] '{context.Caster.CharacterName}' dealt {damage} {type} damage to '{target.CharacterName}'. " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}/{target.CharacterStats.MaxHealthPoints}");

            // Clear the cached roll so the next cast picks a fresh damage type.
            _resolvedType = null;
            return true;
        }
    }
}
