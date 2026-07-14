namespace BaaroForce.Spells
{
    /// <summary>
    /// Declares who a spell can be aimed at.
    /// TurnManager uses this to choose the highlight colour and to decide which
    /// tiles are selectable during the targeting phase.
    /// </summary>
    public enum SpellTargetType
    {
        /// <summary>Targets enemy units only — red highlight.</summary>
        Enemy,

        /// <summary>Targets friendly party members only — green highlight.</summary>
        Ally,

        /// <summary>Can target any occupied tile — purple highlight.</summary>
        Both,

        /// <summary>Targets the caster; no tile selection is required.</summary>
        Self,
        /// <summary>Targets an area of effect around the chosen tile — yellow highlight.</summary>
        Area
    }
}
