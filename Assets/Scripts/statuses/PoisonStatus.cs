using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Poison status — deals damage at the start of the afflicted unit's turn.
    /// Permanent (lasts the entire battle) until dispelled or overwritten — see
    /// <see cref="Character.ApplyStatus"/>, which replaces a same-named effect on re-application.
    /// </summary>
    public class PoisonStatus : StatusEffect
    {
        private int _poisonAmount;

        /// <param name="poisonAmount">How much damage to deal at the start of each turn.</param>
        public PoisonStatus(int poisonAmount)
            : base(
                name:        "Poison",
                description: $"Deals {poisonAmount} damage at the start of each turn.",
                durationTurns: -1,
                effectType: StatusEffectType.Debuff)
        {
            this._poisonAmount = poisonAmount;
        }

        public override void OnApply(CharacterStats stats)
        {
            // No immediate effect on apply — the first tick happens at the target's next turn.
        }

        public override void OnTurnStart(CharacterStats stats)
        {
            stats.TakeDamage(_poisonAmount);
        }

        public override void OnRemove(CharacterStats stats)
        {
            // No effect on remove
        }

        /// <summary>Re-applying Poison adds the new cast's damage onto the existing tick
        /// amount (1 + 1 poison = 2 damage/turn), rather than replacing it.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is PoisonStatus poison)
            {
                _poisonAmount += poison._poisonAmount;
                Description = $"Deals {_poisonAmount} damage at the start of each turn.";
            }
        }
    }
}
