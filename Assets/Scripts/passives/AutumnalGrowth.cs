using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Formulas;

namespace BaaroForce.Passives
{
    public class AutumnalGrowth : PassiveAbility
    {
        public AutumnalGrowth()
            : base("Autumnal Growth",
                  "[Regen] {0} health points per turn",
                  PassiveAbilityType.StartOfTurn)
        {
        }

        public override ScalingValue[] ComputeValues(Character owner) =>
            new[]
            {
                new ScalingValue("Regen")
                    .Add("Base", 1)
                    .Add($"Level ({owner.Level} × 0.25, floored)", Mathf.FloorToInt(0.25f * owner.Level))
            };

        public override bool Execute(PassiveOnTurnContext context)
        {
            int bonus = ComputeValues(context.Character)[0].Total;
            context.Character.CharacterStats.HealthPoints += bonus;
            context.Character.CharacterStats.HealthPoints = Mathf.Min(
                context.Character.CharacterStats.HealthPoints,
                context.Character.CharacterStats.MaxHealthPoints);

            Debug.Log($"[AutumnalGrowth] '{context.Character.CharacterName}' gained {bonus} HP.  " +
                      $"HP: {context.Character.CharacterStats.HealthPoints}" +
                      $"/{context.Character.CharacterStats.MaxHealthPoints}");
            return true;
        }
    }
}