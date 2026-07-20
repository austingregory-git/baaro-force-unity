using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Dodge status — the next physical attack against the afflicted unit deals no damage.
    /// Consumed the instant it absorbs an attack (see Character.TryConsumeDodge); otherwise
    /// persists indefinitely, since it represents "the next hit" rather than a timed effect.
    /// </summary>
    public class DodgeStatus : StatusEffect
    {
        public DodgeStatus()
            : base(
                name:        "Dodge",
                description: "Dodges the next physical attack against this unit.",
                durationTurns: -1,
                effectType: StatusEffectType.Buff)
        {
        }

        public override void OnApply(CharacterStats stats)
        {
            // No stat modification — this is a one-shot marker consumed by TakePhysicalDamage.
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
        }
    }
}
