using UnityEngine;

namespace BaaroForce.Passives
{
    public class AutumnalGrowth : PassiveAbility
    {
        public AutumnalGrowth()
            : base("Autumnal Growth",
                  "[Regen] 1 + 0.25 x [Level] health points per turn",
                  PassiveAbilityType.START_OF_TURN)
        {
        }

        public override bool Execute(PassiveAbilityContext context)
        {
            int bonus = Mathf.FloorToInt(1f + 0.25f * context.CharacterLevel);
            context.Character.characterStats.healthPoints += bonus;
            context.Character.characterStats.healthPoints = Mathf.Min(
                context.Character.characterStats.healthPoints,
                context.Character.characterStats.maxHealthPoints);

            Debug.Log($"[AutumnalGrowth] '{context.Character.characterName}' gained {bonus} HP.  " +
                      $"HP: {context.Character.characterStats.healthPoints}" +
                      $"/{context.Character.characterStats.maxHealthPoints}");
            return true;
        }
    }
}