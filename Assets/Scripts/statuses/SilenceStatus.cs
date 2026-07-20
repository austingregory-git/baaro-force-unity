using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Silence status — the afflicted unit cannot cast spells for the duration.
    /// Enforced by TurnManager (see Character.IsSilenced) before entering spell-targeting
    /// mode and before an Npc's AI is allowed to resolve a queued spell. Basic attacks are
    /// unaffected.
    /// </summary>
    public class SilenceStatus : StatusEffect
    {
        public SilenceStatus(int durationTurns)
            : base(
                name:        "Silence",
                description: $"Cannot cast spells for {durationTurns} turns.",
                durationTurns: durationTurns,
                effectType: StatusEffectType.Debuff)
        {
        }

        public override void OnApply(CharacterStats stats)
        {
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
        }
    }
}
