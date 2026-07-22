using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.ActMap.Encounters
{
    /// <summary>
    /// One entry in an <see cref="EncounterRegistry"/> pool — realm/tier-scoped encounter
    /// config consumed by MapGenerator when a Fight/Elite/Boss node is entered. Composition
    /// is expressed as a target strength for <see cref="Characters.EnemyPackBuilder"/> (as
    /// today) rather than a fixed enemy list, since the Npc roster is still small; a named
    /// "Name" is kept so future distinct encounters (fixed compositions) can replace the
    /// strength-based ones per realm without changing callers.
    /// </summary>
    public class Encounter
    {
        public string Name { get; }
        public Realm Realm { get; }
        public EncounterPoolTier Tier { get; }
        public MapSize GridSize { get; }
        public int TargetStrength { get; }

        /// <summary>
        /// Enemy level is deliberately not stored here — the same Normal2 pool is used for
        /// both Fight #2 (level 2) and Fight #3 (level 3), so the caller (ActMapController)
        /// supplies the level for the specific node being resolved.
        /// </summary>
        public Encounter(string name, Realm realm, EncounterPoolTier tier, MapSize gridSize, int targetStrength)
        {
            Name = name;
            Realm = realm;
            Tier = tier;
            GridSize = gridSize;
            TargetStrength = targetStrength;
        }
    }
}
