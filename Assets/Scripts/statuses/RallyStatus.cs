using BaaroForce.Characters;
using UnityEngine;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Rally status — the afflicted unit gains increased attack each turn.
    /// When the effect expires or is dispelled the attack boost is removed.
    /// </summary>
    public class RallyStatus : StatusEffect
    {
        private int _attackBoost;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="attackBoost">How much to increase attack by.</param>
        public RallyStatus(int durationTurns, int attackBoost = 2)
            : base(
                name:        "Rally",
                description: $"Increases attack by {attackBoost} for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Buff)
        {
            this._attackBoost = attackBoost;
        }

        public override void OnApply(CharacterStats stats)
        {
            stats.AttackBonus += _attackBoost;
            Debug.Log($"[RallyStatus] Applied. Attack increased by {_attackBoost}. New attack: {stats.TotalAttack}");
        }

        public override void OnTurnStart(CharacterStats stats)
        {

        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.AttackBonus -= _attackBoost;   // restore what OnApply added
            Debug.Log($"[RallyStatus] Removed. Attack decreased by {_attackBoost}. New attack: {stats.TotalAttack}");
        }

        /// <summary>Re-applying Rally adds the new cast's attack boost onto the existing
        /// one, rather than replacing it.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is RallyStatus rally)
            {
                _attackBoost += rally._attackBoost;
                stats.AttackBonus += rally._attackBoost;
                Description = $"Increases attack by {_attackBoost} for {RemainingTurns} turns.";
                Debug.Log($"[RallyStatus] Stacked. Attack increased by {rally._attackBoost} (total {_attackBoost}). New attack: {stats.TotalAttack}");
            }
        }
    }
}
