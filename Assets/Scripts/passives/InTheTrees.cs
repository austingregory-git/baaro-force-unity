using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Statuses;

namespace BaaroForce.Passives
{
    /// <summary>
    /// In The Trees — Shopu's signature passive ability.
    /// At the start of each combat, become [Invisible] for 3 turns.
    /// Invisibility prevents enemies from targeting the character with attacks or spells.
    /// Invisibility is removed if the character attacks or casts a spell.
    /// </summary>
    public class InTheTrees : PassiveAbility
    {
        private const int InvisibilityDurationTurns = 3;

        public InTheTrees()
            : base("In The Trees",
                  "At the start of each combat, become [Invisible] for 3 turns.",
                  PassiveAbilityType.StartOfCombat)
        {
        }

        public override bool Execute(PassiveOnTurnContext context)
        {
            context.Character.ApplyStatus(new InvisibleStatus(InvisibilityDurationTurns));
            Debug.Log($"[InTheTrees] '{context.Character.CharacterName}' fades into the trees, " +
                      $"becoming invisible for {InvisibilityDurationTurns} turns.");
            return true;
        }
    }
}
