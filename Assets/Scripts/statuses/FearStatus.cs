using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Fear status — the afflicted unit's attack is reduced while fearful.
    /// When the effect expires or is dispelled the penalty is fully reversed.
    /// </summary>
    public class FearStatus : StatusEffect
    {
        private int _attackPenalty;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="attackPenalty">How much to subtract from the target's attackBonus.</param>
        public FearStatus(int durationTurns, int attackPenalty)
            : base(
                name:        "Fear",
                description: $"Attack reduced by {attackPenalty} for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Debuff)
        {
            this._attackPenalty = attackPenalty;
        }

        public override void OnApply(CharacterStats stats)
        {
            stats.AttackBonus -= _attackPenalty;
        }

        public override void OnTurnStart(CharacterStats stats)
        {
            // Fear does not deal per-turn damage; it simply persists.
        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.AttackBonus += _attackPenalty;   // restore what OnApply removed
        }

        /// <summary>Re-applying Fear adds the new cast's penalty onto the existing one,
        /// rather than replacing it.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is FearStatus fear)
            {
                _attackPenalty += fear._attackPenalty;
                stats.AttackBonus -= fear._attackPenalty;
                Description = $"Attack reduced by {_attackPenalty} for {RemainingTurns} turns.";
            }
        }
    }
}
