namespace BaaroForce.Characters
{
    /// <summary>
    /// Defines how an NPC makes decisions during its turn.
    /// Each value maps to a concrete <see cref="NpcAI"/> subclass.
    /// </summary>
    public enum NpcPersonality
    {
        /// <summary>Always closes in and attacks as hard as possible.
        /// Spells first, then attacks, then advances toward the nearest enemy.</summary>
        Aggressive,

        /// <summary>Reserved — will favour high-value targets and conserve resources.</summary>
        Smart,

        /// <summary>Reserved — will flee when low on health and avoid direct confrontation.</summary>
        Scared,
    }
}
