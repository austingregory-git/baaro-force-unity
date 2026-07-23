using BaaroForce.Characters;
using BaaroForce.Formulas;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Star Shot — Hans's signature archery spell.
    ///
    /// Infinite range — deals damage equal to the caster's TotalAttack to a single enemy,
    /// anywhere on the battlefield.
    /// </summary>
    public class StarShot : CharacterSpell
    {
        /// <summary>Comfortably larger than the biggest map's diagonal, so the range check
        /// in TurnManager/AggressiveNpcAI never excludes a tile — the spell's actual "infinite
        /// range" behaviour.</summary>
        private const int InfiniteRange = 99;

        public StarShot()
            : base(
                name:        "Star Shot",
                description: "Infinite range - Deals {0} damage to an enemy.",
                manaCost:        2,
                actionPointCost: 1,
                range:       InfiniteRange,
                area:        0,
                cooldown:    2,
                targetType:  SpellTargetType.Enemy,
                type:        SpellType.Earth)
        {
        }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[] { new ScalingValue("Damage").AddTotalAttack(caster.CharacterStats) };

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
                Debug.LogWarning("[StarShot] No valid target on the target tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Earth, "StarShot");

            Debug.Log($"[StarShot] '{context.Caster.CharacterName}' fires a star shot at " +
                      $"'{target.CharacterName}' for {damage} [Earth] damage.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
