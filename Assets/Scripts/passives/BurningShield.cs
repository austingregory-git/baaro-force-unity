using UnityEngine;

namespace BaaroForce.Passives
{
    public class BurningShield : PassiveAbility
    {
        public BurningShield()
            : base("Burning Shield",
                  "Melee attackers take 1 + 0.25 x [Level] [Fire] damage.",
                  PassiveAbilityType.ON_RECEIVING_ATTACK)
        {
        }

        public override bool Execute(PassiveOnReceivingAttackContext context)
        {
            int value = Mathf.FloorToInt(1f + 0.25f * context.ReceivingCharacter.Level);
            context.Attacker.characterStats.healthPoints -= value;

            if (context.Attacker.characterStats.healthPoints <= 0)
            {
                Debug.Log($"[BurningShield] '{context.Attacker.characterName}' has been defeated!");
                context.AttackerTile.RemoveUnit();
            }

            Debug.Log($"[BurningShield] '{context.Attacker.characterName}' took {value} fire damage.  " +
                      $"HP: {context.Attacker.characterStats.healthPoints}" +
                      $"/{context.Attacker.characterStats.maxHealthPoints}");
            return true;
        }
    }
}