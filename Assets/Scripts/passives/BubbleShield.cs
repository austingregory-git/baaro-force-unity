using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Statuses;

namespace BaaroForce.Passives
{
    /// <summary>
    /// Bubble Shield — Guppy's signature passive ability.
    /// At the start of each combat, gain a protective bubble that absorbs the next
    /// instance of damage taken, however long that takes to arrive.
    /// </summary>
    public class BubbleShield : PassiveAbility
    {
        public BubbleShield()
            : base("Bubble Shield",
                  "At the start of each combat, gain a protective bubble that absorbs the next instance of damage.",
                  PassiveAbilityType.StartOfCombat)
        {
        }

        public override bool Execute(PassiveOnTurnContext context)
        {
            context.Character.ApplyStatus(new BubbleShieldStatus());
            Debug.Log($"[BubbleShield] '{context.Character.CharacterName}' gains a protective bubble.");
            return true;
        }
    }
}
