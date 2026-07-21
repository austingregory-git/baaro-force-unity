using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Deal 3 + 1/2 × TotalAttack damage to an enemy and silence them for 2 turns.
    /// Silence prevents the target from casting spells for the duration of the effect.
    /// </summary>
    public class Shiv : ClassSpell
    {
        private const int SilenceDurationTurns = 2;

        public Shiv() : base(
            characterClass: ClassRegistry.Get("Rogue"),
            name:        "Shiv",
            description: "Deal {0} damage to an enemy and apply [Silence] for 2 turns.",
            manaCost:        2,
            actionPointCost: 1,
            range:       1,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            int totalAttack = caster.CharacterStats.TotalAttack;
            var damage = new ScalingValue("Damage")
                .Add("Base", 3)
                .Add($"Total Attack ({totalAttack} × 0.5, floored)", Mathf.FloorToInt(totalAttack * 0.5f));
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview
            {
                RawDamage        = ComputeValues(caster)[0].Total,
                StatusEffectName = "Silence",
                StatusEffectKind = StatusEffect.StatusEffectType.Debuff,
            };

        public override bool Execute(SpellContext context)
        {
            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[Shiv] No enemy on the target tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Physical, "Shiv", physical: true);

            target.ApplyStatus(new SilenceStatus(SilenceDurationTurns));

            Debug.Log($"[Shiv] '{context.Caster.CharacterName}' stabs '{target.CharacterName}' for {damage} damage " +
                      $"and silences them for {SilenceDurationTurns} turns.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
