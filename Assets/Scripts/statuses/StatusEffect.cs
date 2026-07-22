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

        /// <summary>
        /// Number of times this effect has been applied on top of itself (starts at 1).
        /// Charge-based effects (Dodge, Bubble Shield, Aim, Empower) bank one consumable use
        /// per stack — see <see cref="ConsumeStack"/> and Character.TryConsumeX. Magnitude
        /// effects (Poison, Regen, Rally, ...) instead fold the incoming cast's amount into
        /// their own per-turn value in their <see cref="Stack"/> override; Stacks still
        /// increments alongside that so it can be surfaced in the UI.
        /// </summary>
        public int Stacks { get; protected set; } = 1;

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
        /// Merges a newly-cast <paramref name="incoming"/> instance of this same effect
        /// (matched by <see cref="Name"/>) into this already-active instance — see
        /// <see cref="Character.ApplyStatus"/>, which calls this instead of the old
        /// remove-then-replace whenever a status is re-applied on top of itself.
        ///
        /// Default behavior: bump <see cref="Stacks"/> and refresh the timer to the incoming
        /// cast's duration (permanent effects, <c>RemainingTurns == -1</c>, are left alone) —
        /// this alone is enough "stacking" for presence-only effects with no magnitude to
        /// grow (Root, Silence, Invisible: re-casting just resets the clock).
        ///
        /// Override to also fold the incoming instance's magnitude into this one — add its
        /// poison/heal/attack-bonus/etc. delta to this instance's own field *and* apply that
        /// same delta to <paramref name="stats"/> (see PoisonStatus, RegenStatus, RallyStatus,
        /// FearStatus, ImprovedArrowsStatus, SlowStatus for examples). Always call
        /// <c>base.Stack(incoming, stats)</c> first so Stacks/duration still update.
        /// </summary>
        public virtual void Stack(StatusEffect incoming, CharacterStats stats)
        {
            Stacks++;
            if (incoming.RemainingTurns >= 0)
                RemainingTurns = incoming.RemainingTurns;
        }

        /// <summary>Removes a single banked charge from a stacked charge-based effect
        /// (Dodge, Bubble Shield, Aim, Empower) without touching stats — the caller
        /// (Character.TryConsumeX) is responsible for fully removing the effect via
        /// OnRemove once <see cref="Stacks"/> reaches zero.</summary>
        public void ConsumeStack()
        {
            if (Stacks > 0) Stacks--;
        }

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
