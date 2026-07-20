using BaaroForce.Statuses;

namespace BaaroForce.Formulas
{
    /// <summary>
    /// Describes the predicted outcome of using a spell (or basic attack) against a
    /// specific target, as deltas — without applying anything. The HUD renderer applies
    /// these against whatever the target's *live* stats currently are (via
    /// <c>CharacterStats.PeekDamage</c>/<c>PeekHeal</c>) to compute the actual predicted
    /// bar/number values, so this stays a pure description of "what this action does"
    /// independent of who it's aimed at.
    /// </summary>
    public sealed class ActionPreview
    {
        /// <summary>Pre-shield damage this action would deal (0 = none).</summary>
        public int RawDamage;
        /// <summary>HP this action would restore, pre-max-HP-clamp (0 = none).</summary>
        public int RawHeal;
        /// <summary>Change to MaxHealthPoints (e.g. Grit).</summary>
        public int MaxHpDelta;
        /// <summary>ShieldPoints this action would add (e.g. Ball Form).</summary>
        public int ShieldGain;
        /// <summary>Signed change to AttackBonus (+Rally, -Fear).</summary>
        public int AttackBonusDelta;
        /// <summary>Name of a status effect this action would apply, or null.</summary>
        public string StatusEffectName;
        public StatusEffect.StatusEffectType? StatusEffectKind;

        /// <summary>Sentinel for "this action has nothing to preview".</summary>
        public static readonly ActionPreview None = new ActionPreview();

        public bool HasEffect =>
            this != None && (RawDamage != 0 || RawHeal != 0 || MaxHpDelta != 0 ||
                              ShieldGain != 0 || AttackBonusDelta != 0 || StatusEffectName != null);
    }
}
