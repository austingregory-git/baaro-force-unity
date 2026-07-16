using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Shield status — the afflicted unit gains shield each turn.
    /// When the effect expires or is dispelled the shield stops.
    /// </summary>
    public class ShieldStatus : StatusEffect
    {
        private readonly int shieldAmount;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="shieldAmount">How much shield to grant each turn.</param>
        public ShieldStatus(int durationTurns, int shieldAmount)
            : base(
                name:        "Shield",
                description: $"Grants {shieldAmount} shield for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.BUFF)
        {
            this.shieldAmount = shieldAmount;
        }

        public override void OnApply(CharacterStats stats)
        {
            stats.shieldPoints += shieldAmount;
        }

        public override void OnTurnStart(CharacterStats stats)
        {
            // No per-turn effect; shield is granted on apply and persists until removed.
        }

        public override void OnRemove(CharacterStats stats)
        {
            // No effect on remove
        }
    }
}
