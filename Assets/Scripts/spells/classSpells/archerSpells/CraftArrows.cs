using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Craft Arrows - Craft arrows to improve your bonus attack power by {0} for 3 turns. This does not cost an action point. This can only be used once per fight.
    /// Applies the ImprovedArrowsStatus effect to the caster, which increases their bonus attack power by {0} for 3 turns.
    /// </summary>
    public class CraftArrows : ClassSpell
    {
        public CraftArrows() : base(
            characterClass: ClassRegistry.Get("Archer"),
            name:        "Craft Arrows",
            description: "Craft arrows to improve your bonus attack power by {0} for 3 turns. This does not cost an action point. This can only be used once per fight.",
            manaCost:        2,
            actionPointCost: 0,
            range:       0,
            area:        0,
            cooldown:    -1,
            targetType:  SpellTargetType.Self,
            type:        SpellType.Buff,
            oncePerFight: true)
        { }

        /// <summary>How many turns the attack-power boost lasts.</summary>
        private const int BuffDurationTurns = 3;

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var attack = new ScalingValue("Attack Bonus");
            attack.Add("Base", 2);
            attack.Add("Level", Mathf.FloorToInt(caster.Level * 0.25f));
            return new[] { attack };
        }

        /// <summary>Self-only — <paramref name="target"/> is always the caster.</summary>
        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { AttackBonusDelta = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            int bonus = ComputeValues(context.Caster)[0].Total;
            context.Caster.ApplyStatus(new ImprovedArrowsStatus(durationTurns: BuffDurationTurns, attackBoost: bonus));

            Debug.Log($"[Craft Arrows] '{context.Caster.CharacterName}' crafts arrows, " +
                      $"increasing bonus attack power by {bonus} for {BuffDurationTurns} turns.");

            return true;
        }
    }
}