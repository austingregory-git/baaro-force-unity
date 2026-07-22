using BaaroForce.Characters;
using BaaroForce.Spells;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Empower status — multiplies the damage of the afflicted unit's next basic attack
    /// and makes it deal a (pre-rolled) elemental damage type instead of Physical/Magical.
    /// Consumed the instant it boosts an attack (see Character.TryConsumeEmpower/
    /// TurnManager.ResolveBasicAttack); otherwise persists indefinitely, since it
    /// represents "the next attack" rather than a timed effect — same reasoning as AimStatus.
    /// </summary>
    public class EmpowerStatus : StatusEffect
    {
        /// <summary>Factor the next basic attack's damage is multiplied by.</summary>
        public readonly int Multiplier;

        /// <summary>Elemental damage type the next basic attack deals instead of Physical/Magical.</summary>
        public readonly SpellType DamageType;

        public EmpowerStatus(int multiplier, SpellType damageType)
            : base(
                name:        "Empower",
                description: $"Multiplies the damage of the next basic attack by {multiplier} and makes it deal [{damageType}] damage.",
                durationTurns: -1,
                effectType: StatusEffectType.Buff)
        {
            this.Multiplier = multiplier;
            this.DamageType = damageType;
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
