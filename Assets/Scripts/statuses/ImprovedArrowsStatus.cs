using BaaroForce.Characters;
using UnityEngine;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Improved Arrows status — the afflicted unit's bonus attack power is increased for
    /// the duration. When the effect expires or is dispelled the boost is removed.
    /// </summary>
    public class ImprovedArrowsStatus : StatusEffect
    {
        private int _attackBoost;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="attackBoost">How much to increase bonus attack power by.</param>
        public ImprovedArrowsStatus(int durationTurns, int attackBoost)
            : base(
                name:        "Improved Arrows",
                description: $"Increases bonus attack power by {attackBoost} for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Buff)
        {
            this._attackBoost = attackBoost;
        }

        public override void OnApply(CharacterStats stats)
        {
            stats.AttackBonus += _attackBoost;
            Debug.Log($"[ImprovedArrowsStatus] Applied. Attack bonus increased by {_attackBoost}. New attack: {stats.TotalAttack}");
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.AttackBonus -= _attackBoost;   // restore what OnApply added
            Debug.Log($"[ImprovedArrowsStatus] Removed. Attack bonus decreased by {_attackBoost}. New attack: {stats.TotalAttack}");
        }

        /// <summary>Re-applying Improved Arrows adds the new cast's attack boost onto the
        /// existing one, rather than replacing it.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is ImprovedArrowsStatus improved)
            {
                _attackBoost += improved._attackBoost;
                stats.AttackBonus += improved._attackBoost;
                Description = $"Increases bonus attack power by {_attackBoost} for {RemainingTurns} turns.";
                Debug.Log($"[ImprovedArrowsStatus] Stacked. Attack bonus increased by {improved._attackBoost} (total {_attackBoost}). New attack: {stats.TotalAttack}");
            }
        }
    }
}
