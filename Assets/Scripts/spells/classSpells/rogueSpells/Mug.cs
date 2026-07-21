using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.GameController;
using BaaroForce.Map;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Mug an enemy for TotalAttack damage and steal some gold.
    /// steal random quantity of gold, range is 5-10 * (0.5 * Level), floored
    /// </summary>
    public class Mug : ClassSpell
    {
        private const int MinGoldRoll = 5;
        private const int MaxGoldRoll = 10; // inclusive

        public Mug() : base(
            characterClass: ClassRegistry.Get("Rogue"),
            name:        "Mug",
            description: "Mug an enemy for {0} damage and steal some gold. Can only be used once per fight.",
            manaCost:        0,
            actionPointCost: 1,
            range:       1,
            area:        0,
            cooldown:    0,
            targetType:  SpellTargetType.Enemy,
            oncePerFight: true,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[] { new ScalingValue("Damage").AddTotalAttack(caster.CharacterStats) };


        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[Mug] No enemy on the target tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Physical, "Mug", physical: true);

            int goldStolen = RollGold(context.Caster);
            PartyManager.Instance?.Party?.AddGold(goldStolen);
            FloatingCombatTextSystem.Instance?.ShowGold(context.Caster, goldStolen);

            Debug.Log($"[Mug] '{context.Caster.CharacterName}' mugs '{target.CharacterName}' for {damage} damage " +
                      $"and steals {goldStolen} gold.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }

        /// <summary>Rolls a random 5-10 quantity, scaled by half the caster's level and floored.</summary>
        private static int RollGold(Character caster) =>
            Mathf.FloorToInt(UnityEngine.Random.Range(MinGoldRoll, MaxGoldRoll + 1) * 0.5f * caster.Level);
    }
}
