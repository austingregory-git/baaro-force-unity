using UnityEngine;

namespace BaaroForce.Passives
{
    public class AutumnalGrowth : PassiveAbility
    {
        public AutumnalGrowth()
            : base("Autumnal Growth",
                  "[Regen] 1 + 0.25 x [Level] health points per turn",
                  PassiveAbilityType.StartOfTurn)
        {
        }

        public override bool Execute(PassiveOnTurnContext context)
        {
            int bonus = Mathf.FloorToInt(1f + 0.25f * context.CharacterLevel);
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