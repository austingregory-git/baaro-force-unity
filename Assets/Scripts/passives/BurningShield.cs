using UnityEngine;

namespace BaaroForce.Passives
{
    public class BurningShield : PassiveAbility
    {
        public BurningShield()
            : base("Burning Shield",
                  "Melee attackers take 1 + 0.25 x [Level] [Fire] damage.",
                  PassiveAbilityType.OnReceivingAttack)
        {
        }

        public override bool Execute(PassiveOnReceivingAttackContext context)
        {
            int value = Mathf.FloorToInt(1f + 0.25f * context.ReceivingCharacter.Level);
            context.Attacker.CharacterStats.TakeDamage(value);

            if (context.Attacker.CharacterStats.HealthPoints <= 0)
            {
                Debug.Log($"[BurningShield] '{context.Attacker.CharacterName}' has been defeated!");
                context.AttackerTile.RemoveUnit();
            }

            Debug.Log($"[BurningShield] '{context.Attacker.CharacterName}' took {value} fire damage.  " +
                      $"HP: {context.Attacker.CharacterStats.HealthPoints}" +
                      $"/{context.Attacker.CharacterStats.MaxHealthPoints}");
            return true;
        }
    }
}