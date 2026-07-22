using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Aim status — multiplies the damage of the afflicted unit's next basic attack.
    /// Consumed the instant it boosts an attack (see Character.TryConsumeAim/
    /// TurnManager.ResolveBasicAttack); otherwise persists indefinitely, since it
    /// represents "the next attack" rather than a timed effect.
    /// </summary>
    public class AimStatus : StatusEffect
    {
        /// <summary>Factor the next basic attack's damage is multiplied by.</summary>
        public readonly int Multiplier;

        public AimStatus(int multiplier)
            : base(
                name:        "Aim",
                description: $"Multiplies the damage of the next basic attack by {multiplier}.",
                durationTurns: -1,
                effectType: StatusEffectType.Buff)
        {
            this.Multiplier = multiplier;
        }

        public override void OnApply(CharacterStats stats)
        {
            // No stat modification — this is a one-shot marker consumed by ResolveBasicAttack.
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
        }
    }
}
