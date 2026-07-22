using BaaroForce.Characters;
using UnityEngine;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Regen status — the afflicted unit regenerates health each turn.
    /// When the effect expires or is dispelled the regeneration stops.
    /// </summary>
    public class RegenStatus : StatusEffect
    {
        private int _healAmount;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="healAmount">How much health to regenerate each turn.</param>
        public RegenStatus(int durationTurns, int healAmount)
            : base(
                name:        "Regen",
                description: $"Regenerates {healAmount} health for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Buff)
        {
            this._healAmount = healAmount;
        }

        public override void OnApply(CharacterStats stats)
        {
            // No immediate effect on apply
        }

        public override void OnTurnStart(CharacterStats stats)
        {
            stats.HealthPoints += _healAmount;
            stats.HealthPoints = Mathf.Min(stats.HealthPoints, stats.MaxHealthPoints);
        }

        public override void OnRemove(CharacterStats stats)
        {
            // No effect on remove
        }

        /// <summary>Re-applying Regen adds the new cast's heal-per-turn onto the existing
        /// tick amount, rather than replacing it.</summary>
        public override void Stack(StatusEffect incoming, CharacterStats stats)
        {
            base.Stack(incoming, stats);
            if (incoming is RegenStatus regen)
            {
                _healAmount += regen._healAmount;
                Description = $"Regenerates {_healAmount} health for {RemainingTurns} turns.";
            }
        }
    }
}
