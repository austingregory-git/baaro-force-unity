using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Ball Form — Winston's signature dark-magic spell.
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
    public class BallForm : CharacterSpell
    {
        public BallForm()
            : base(
                name:        "Ball Form",
                description: "Gain {0} [Shield].",
                manaCost:        2,
                actionPointCost: 1,
                range:       0,
                area:        0,
                cooldown:    2,
                targetType:  SpellTargetType.Self)
        {
        }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Shield")
                    .Add("Base", 3)
                    .Add("Level", caster.Level)
            };

        public override bool Execute(SpellContext context)
        {
            // Apply Shield — increases caster's shield for 3 + level turns.
            int shieldAmount = ComputeValues(context.Caster)[0].Total;
            context.Caster.CharacterStats.ShieldPoints += shieldAmount;

            Debug.Log($"[BallForm] '{context.Caster.CharacterName}' gains {shieldAmount} shield points.");

            return true;
        }

    }
}
