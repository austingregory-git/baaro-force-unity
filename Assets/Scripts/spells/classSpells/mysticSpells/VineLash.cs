using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Vine Lash — Deal 2 + (0.5 * SpellPower) + (0.5 * Level) damage to an enemy and [Slow] them for 2 turns.
    /// Slow reduces the target's movement speed by 50% for the duration of the effect.
    /// </summary>
    public class VineLash: ClassSpell
    {
        private const int SlowDurationTurns = 2;
        private const float SlowMovementReductionPercent = 0.5f;

        public VineLash() : base(
            characterClass: ClassRegistry.Get("Mystic"),
            name:        "Vine Lash",
            description: "Deal {0} damage to an enemy and [Slow] them for 2 turns.",
            manaCost:        1,
            actionPointCost: 1,
            range:       2,
            area:        0,
            cooldown:    1,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Earth)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage")
                .Add("Base", 2)
                .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 0.5, floored)", Mathf.FloorToInt(caster.CharacterStats.SpellPowerBonus * 0.5f))
                .Add($"Level Bonus ({caster.Level} × 0.5, floored)", Mathf.FloorToInt(caster.Level * 0.5f));
            return new[] { damage };
        }


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
                Debug.LogWarning("[Vine Lash] No target found on the selected tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Earth, "Vine Lash");
            target.ApplyStatus(new SlowStatus(SlowDurationTurns, SlowMovementReductionPercent));

            Debug.Log($"[Vine Lash] '{context.Caster.CharacterName}' dealt {damage} damage to '{target.CharacterName}' and applied [Slow] for {SlowDurationTurns} turns. " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
