using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Death Stare — Winston's signature dark-magic spell.
    ///
    /// Locks eyes with a single enemy within range 3, inflicting Fear for a duration
    /// scaled by the caster's level and dealing a small hit of dark damage.
    ///
    /// Level scaling:
    ///   Fear duration  = floor(1 + 0.25 × level)    (1 turn at level 1)
    ///   Damage         = floor(1 + 0.50 × level)    (1 damage at level 1)
    ///
    /// Fear effect: reduces the target's effective attack (via attackBonus) for
    /// the duration.  The penalty is reversed when the effect expires.
    /// </summary>
    public class DeathStare : CharacterSpell
    {
        public DeathStare()
            : base(
                name:        "Death Stare",
                description: "Apply [Fear] 1 to an enemy for {0} turns and deal {1} [Dark] damage.",
                manaCost:        2,
                actionPointCost: 1,
                range:       3,
                area:        0,
                cooldown:    1,
                targetType:  SpellTargetType.Enemy,
                type:        SpellType.Dark)
        {
        }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            int level = caster.Level;
            var fearDuration = new ScalingValue("Fear Duration")
                .Add("Base", 1)
                .Add($"Level ({level} × 0.25, floored)", Mathf.FloorToInt(0.25f * level));
            var damage = new ScalingValue("Damage")
                .Add("Base", 2)
                .Add($"Level ({level} × 0.5, floored)", Mathf.FloorToInt(0.5f * level));
            return new[] { fearDuration, damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target)
        {
            ScalingValue[] values = ComputeValues(caster);
            return new ActionPreview
            {
                RawDamage         = values[1].Total,
                AttackBonusDelta  = -(caster is Npc ? 2 : 1),
                StatusEffectName  = "Fear",
                StatusEffectKind  = StatusEffect.StatusEffectType.Debuff,
            };
        }

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
                Debug.LogWarning("[DeathStare] No valid target on the target tile.");
                return false;
            }

            ScalingValue[] values = ComputeValues(context.Caster);
            int fearDuration = values[0].Total;
            int damage       = values[1].Total;

            // Apply Fear — reduces target's attack for N turns.  Npc-cast Death
            // Stare hits harder (attackPenalty 2) than the player-cast version (1).
            var fear = new FearStatus(durationTurns: fearDuration, attackPenalty: casterIsNpc ? 2 : 1);
            target.ApplyStatus(fear);

            // Deal dark damage.
            DealDamage(target, context.TargetTile, damage, SpellType.Dark, "DeathStare");

            Debug.Log($"[DeathStare] '{context.Caster.CharacterName}' casts Death Stare on " +
                      $"'{target.CharacterName}'.  Damage: {damage}, " +
                      $"Fear: {fearDuration} turn(s).  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
