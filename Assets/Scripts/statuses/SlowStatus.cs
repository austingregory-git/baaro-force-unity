using BaaroForce.Characters;
using UnityEngine;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Slow status — reduces the afflicted unit's movement points for the duration.
    /// The amount removed is a fraction of Movement at the moment the status is first
    /// applied; the exact (floored) amount taken is restored on expiry/dispel — see
    /// OnRemove — so Movement can't drift from its base value across repeated
    /// apply/remove cycles.
    /// </summary>
    public class SlowStatus : StatusEffect
    {
        private float _movementReductionPercent;
        /// <summary>Movement as it stood before any Slow reduction — captured once on the
        /// first OnApply so later stacks compute their percentage against the true base
        /// rather than an already-reduced value.</summary>
        private int _baseMovement;
        private int _appliedReduction;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="movementReductionPercent">Fraction (0-1) of current Movement to remove, e.g. 0.5 for a 50% slow.</param>
        public SlowStatus(int durationTurns, float movementReductionPercent = 0.5f)
            : base(
                name:        "Slow",
                description: $"Reduces movement by {Mathf.RoundToInt(movementReductionPercent * 100)}% for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Debuff)
        {
            this._movementReductionPercent = movementReductionPercent;
        }

        public override void OnApply(CharacterStats stats)
        {
            _baseMovement     = stats.Movement;
            _appliedReduction = Mathf.FloorToInt(_baseMovement * _movementReductionPercent);
            stats.Movement -= _appliedReduction;
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.Movement += _appliedReduction;   // restore what OnApply/Stack removed
        }

        /// <summary>Re-applying Slow adds the new cast's percentage onto the existing one
        /// (two 50% slows fully halt movement), capped at 100%, and only removes the extra
        /// Movement the bigger percentage now accounts for — <see cref="_baseMovement"/>
        /// keeps the math anchored to pre-Slow Movement instead of compounding.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is SlowStatus slow)
            {
                _movementReductionPercent = Mathf.Min(1f, _movementReductionPercent + slow._movementReductionPercent);
                int newReduction = Mathf.FloorToInt(_baseMovement * _movementReductionPercent);
                stats.Movement -= (newReduction - _appliedReduction);
                _appliedReduction = newReduction;
                Description = $"Reduces movement by {Mathf.RoundToInt(_movementReductionPercent * 100)}% for {RemainingTurns} turns.";
            }
        }
    }
}
