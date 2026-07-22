using BaaroForce.Characters;
using UnityEngine;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Haste status — increases the afflicted unit's movement points for the duration.
    /// The opposite of <see cref="SlowStatus"/>: by default doubles Movement (a 100%
    /// increase). The amount added is a fraction of Movement at the moment the status is
    /// first applied; the exact (floored) amount added is removed on expiry/dispel — see
    /// OnRemove — so Movement can't drift from its base value across repeated
    /// apply/remove cycles.
    /// </summary>
    public class HasteStatus : StatusEffect
    {
        private float _movementIncreasePercent;
        /// <summary>Movement as it stood before any Haste increase — captured once on the
        /// first OnApply so later stacks compute their percentage against the true base
        /// rather than an already-increased value.</summary>
        private int _baseMovement;
        private int _appliedIncrease;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="movementIncreasePercent">Fraction of current Movement to add, e.g. 1.0 (default) to double it.</param>
        public HasteStatus(int durationTurns, float movementIncreasePercent = 1.0f)
            : base(
                name:        "Haste",
                description: $"Increases movement by {Mathf.RoundToInt(movementIncreasePercent * 100)}% for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Buff)
        {
            this._movementIncreasePercent = movementIncreasePercent;
        }

        public override void OnApply(CharacterStats stats)
        {
            _baseMovement     = stats.Movement;
            _appliedIncrease  = Mathf.FloorToInt(_baseMovement * _movementIncreasePercent);
            stats.Movement += _appliedIncrease;
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.Movement -= _appliedIncrease;   // restore what OnApply/Stack added
        }

        /// <summary>Re-applying Haste adds the new cast's percentage onto the existing one
        /// (two 100% hastes triple movement), anchored to <see cref="_baseMovement"/> so the
        /// math doesn't compound off an already-increased value.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is HasteStatus haste)
            {
                _movementIncreasePercent += haste._movementIncreasePercent;
                int newIncrease = Mathf.FloorToInt(_baseMovement * _movementIncreasePercent);
                stats.Movement += (newIncrease - _appliedIncrease);
                _appliedIncrease = newIncrease;
                Description = $"Increases movement by {Mathf.RoundToInt(_movementIncreasePercent * 100)}% for {RemainingTurns} turns.";
            }
        }
    }
}
