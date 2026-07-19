using BaaroForce.Characters;

namespace BaaroForce.Statuses
{
    /// <summary>
    /// Base class for all status effects that can be applied to a unit.
    ///
    /// Effects operate on <see cref="CharacterStats"/> directly rather than on a
    /// specific unit type, so the same subclass can be applied to both player
    /// characters and Npcs without duplication.
    ///
    /// Lifecycle:
    ///   OnApply      — called once when the effect is first applied.
    ///   OnTurnStart  — called at the start of the afflicted unit's turn.
    ///   OnRemove     — called when the effect expires or is dispelled.
    ///   Tick()       — decrements RemainingTurns; returns true when expired.
    /// </summary>
    public abstract class StatusEffect
    {
        public string Name            { get; protected set; }
        public string Description     { get; protected set; }
        public StatusEffectType EffectType { get; protected set; }

        /// <summary>
        /// Turns remaining before this effect expires.
        /// A value of -1 means the effect is permanent until manually dispelled.
        /// </summary>
        public int RemainingTurns     { get; protected set; }

        public bool IsExpired => RemainingTurns == 0;

        protected StatusEffect(string name, string description, int durationTurns, StatusEffectType effectType)
        {
            Name           = name;
            Description    = description;
            RemainingTurns = durationTurns;
            EffectType = effectType;
        }

        /// <summary>Apply the initial stat modification.</summary>
        public abstract void OnApply(CharacterStats stats);

        /// <summary>Per-turn effect (damage over time, healing, etc.).</summary>
        public abstract void OnTurnStart(CharacterStats stats);

        /// <summary>Reverse any stat modifications made in OnApply.</summary>
        public abstract void OnRemove(CharacterStats stats);

        /// <summary>
        /// Decrements the remaining-turns counter.
        /// Returns true when the effect has just expired (RemainingTurns hit 0).
        /// </summary>
        public bool Tick()
        {
            if (RemainingTurns < 0) return false;   // permanent
            if (RemainingTurns > 0) RemainingTurns--;
            return RemainingTurns == 0;
        }

        public enum StatusEffectType
        {
            Buff,
            Debuff,
            Custom
        }
    }
}
