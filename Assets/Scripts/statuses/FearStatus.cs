using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Fear status — the afflicted unit's attack is reduced while fearful.
    /// When the effect expires or is dispelled the penalty is fully reversed.
    /// </summary>
    public class FearStatus : StatusEffect
    {
        private readonly int attackPenalty;

        /// <param name="durationTurns">How many of the target's turns the effect lasts.</param>
        /// <param name="attackPenalty">How much to subtract from the target's attackBonus.</param>
        public FearStatus(int durationTurns, int attackPenalty)
            : base(
                name:        "Fear",
                description: $"Attack reduced by {attackPenalty} for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.DEBUFF)
        {
            this.attackPenalty = attackPenalty;
        }

        public override void OnApply(CharacterStats stats)
        {
            stats.attackBonus -= attackPenalty;
        }

        public override void OnTurnStart(CharacterStats stats)
        {
            // Fear does not deal per-turn damage; it simply persists.
        }

        public override void OnRemove(CharacterStats stats)
        {
            stats.attackBonus += attackPenalty;   // restore what OnApply removed
        }
    }
}
