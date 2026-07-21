using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Bubble Shield status — absorbs the next instance of damage (physical or magical)
    /// this unit would take. Consumed the instant it absorbs damage (see
    /// Character.TryConsumeBubbleShield); has no duration otherwise — it persists
    /// indefinitely until spent.
    /// </summary>
    public class BubbleShieldStatus : StatusEffect
    {
        public BubbleShieldStatus()
            : base(
                name:        "Bubble Shield",
                description: "Prevents the next instance of damage this unit would take.",
                durationTurns: -1,
                effectType: StatusEffectType.Buff)
        {
        }

        public override void OnApply(CharacterStats stats)
        {
            // No stat modification — this is a one-shot marker consumed by Character.TakeDamage.
        }

        public override void OnTurnStart(CharacterStats stats)
        {
        }

        public override void OnRemove(CharacterStats stats)
        {
        }
    }
}
