using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.UI;

namespace BaaroForce.Passives
{
    public class BurningShield : PassiveAbility
    {
        public BurningShield()
            : base("Burning Shield",
                  "Melee attackers take {0} [Fire] damage.",
                  PassiveAbilityType.OnReceivingAttack)
        {
        }

        public override ScalingValue[] ComputeValues(Character owner) =>
            new[]
            {
                new ScalingValue("Damage")
                    .Add("Base", 1)
                    .Add($"Level ({owner.Level} × 0.25, floored)", Mathf.FloorToInt(0.25f * owner.Level))
            };

        public override bool Execute(PassiveOnReceivingAttackContext context)
        {
            int value = ComputeValues(context.ReceivingCharacter)[0].Total;
            int dealt = context.Attacker.CharacterStats.TakeDamage(value);
            FloatingCombatTextSystem.Instance?.ShowDamage(context.Attacker, dealt, SpellType.Fire);

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