namespace BaaroForce.ActMap.Encounters
{
    /// <summary>Which encounter pool a Fight/Elite/Boss node draws from — see the Act 1
    /// design doc's "Fight Pools" section for the pacing this maps to.</summary>
    public enum EncounterPoolTier
    {
        /// <summary>Fight #1 only — 2 encounters per realm, enemy level 1.</summary>
        Normal1,
        /// <summary>Fights #2 and #3 — 3 encounters per realm, enemy level 2/3.</summary>
        Normal2,
        /// <summary>The single Elite fight — 2 encounters per realm, enemy level 4.</summary>
        Elite1,
        /// <summary>The single Boss fight — 2 encounters per realm, enemy level 5.</summary>
        Boss1
    }
}
