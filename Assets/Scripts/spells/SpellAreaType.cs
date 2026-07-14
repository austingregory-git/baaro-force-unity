namespace BaaroForce.Spells
{
    /// <summary>
    /// Declares the area type of a spell.
    /// TurnManager uses this to determine how to highlight the affected tiles and
    /// how to calculate the area of effect during the targeting phase.
    /// </summary>
    public enum SpellAreaType
    {
        /// <summary>Horizontal line — tiles in a horizontal line from the chosen tile are affected.</summary>
        HorizontalLine,
        /// <summary>Vertical line — tiles in a vertical line from the chosen tile are affected.</summary>
        VerticalLine,
        /// <summary>Random area — a random set of tiles around the chosen tile are affected.</summary>
        Random,

        /// <summary>Cone-shaped area — tiles in a cone pattern from the chosen tile are affected.</summary>
        Cone,

        /// <summary>Circle-shaped area — tiles in a circular pattern around the chosen tile are affected.</summary>
        Circle,

        /// <summary>Square-shaped area — tiles in a square pattern around the chosen tile are affected.</summary>
        Square,
        /// <summary>Cross-shaped area — tiles in a cross pattern around the chosen tile are affected.</summary>
        Cross,
        /// <summary>Custom area — the affected tiles are determined by custom logic.</summary>
        Custom,
        /// <summary>No area — the spell affects only the chosen tile.</summary>
        None

    }
}
