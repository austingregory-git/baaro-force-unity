using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Regen status — the afflicted unit regenerates health each turn.
    /// When the effect expires or is dispelled the regeneration stops.
    /// </summary>
    public class RegenStatus : StatusEffect
    {
        private readonly int healAmount;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="healAmount">How much health to regenerate each turn.</param>
        public RegenStatus(int durationTurns, int healAmount = 2)
            : base(
                name:        "Regen",
                description: $"Regenerates {healAmount} health for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.BUFF)
        {
            this.healAmount = healAmount;
        }

        public override void OnApply(CharacterStats stats)
        {
            // No immediate effect on apply
        }

        public override void OnTurnStart(CharacterStats stats)
        {
            stats.healthPoints += healAmount;
        }

        public override void OnRemove(CharacterStats stats)
        {
            // No effect on remove
        }
    }
}
