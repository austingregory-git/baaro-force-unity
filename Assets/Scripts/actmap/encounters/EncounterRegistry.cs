using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.ActMap.Encounters
{
    /// <summary>
    /// Central registry of <see cref="Encounter"/> pools, keyed by (Realm, Tier) per the Act 1
    /// design doc's "Fight Pools" section. The doc calls for 2-3 unique encounters per realm
    /// per pool (50+ total); only a handful of Npcs exist in <see cref="NpcRegistry"/> today
    /// (see EnemyPackBuilder, which composes a pack purely by cumulative Npc.StrengthIndex —
    /// it doesn't filter by realm), so most realms fall back to a shared default pool per
    /// tier rather than having dedicated entries. Earth has real entries since that's where
    /// the current Npc roster (Wolf, Mozeem*) thematically lives. Add dedicated per-realm
    /// pools here as real per-realm enemy content is authored — <see cref="GetRandom"/>
    /// transparently prefers a realm-specific pool the moment one exists.
    /// </summary>
    public static class EncounterRegistry
    {
        private static readonly Dictionary<(Realm, EncounterPoolTier), List<Encounter>> _pools =
            new Dictionary<(Realm, EncounterPoolTier), List<Encounter>>
            {
                { (Realm.Earth, EncounterPoolTier.Normal1), new List<Encounter>
                    {
                        new Encounter("Wolf Den",        Realm.Earth, EncounterPoolTier.Normal1, MapSize.Small, targetStrength: 2),
                        new Encounter("Mozeem Outpost",  Realm.Earth, EncounterPoolTier.Normal1, MapSize.Small, targetStrength: 2),
                    } },
                { (Realm.Earth, EncounterPoolTier.Normal2), new List<Encounter>
                    {
                        new Encounter("Wolf Pack",         Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small, targetStrength: 6),
                        new Encounter("Mozeem Patrol",     Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small, targetStrength: 6),
                        new Encounter("Mozeem Watch Camp", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small, targetStrength: 6),
                    } },
                { (Realm.Earth, EncounterPoolTier.Elite1), new List<Encounter>
                    {
                        new Encounter("Mozeem Guardian's Stand", Realm.Earth, EncounterPoolTier.Elite1, MapSize.Medium, targetStrength: 12),
                    } },
                { (Realm.Earth, EncounterPoolTier.Boss1), new List<Encounter>
                    {
                        new Encounter("The Mozeem Elder's Sanctum", Realm.Earth, EncounterPoolTier.Boss1, MapSize.Large, targetStrength: 20),
                    } },
            };

        // Shared fallback pools, used whenever a realm has no dedicated entry for a tier.
        // Target strengths account for Npc.StrengthIndex being BaseStrengthIndex * Level (see
        // EnemyPackBuilder) — e.g. 6 at level 2 yields ~3 enemies, at level 3 yields ~2.
        private static readonly Dictionary<EncounterPoolTier, List<Encounter>> _defaultPools =
            new Dictionary<EncounterPoolTier, List<Encounter>>
            {
                { EncounterPoolTier.Normal1, new List<Encounter>
                    {
                        new Encounter("Wandering Beasts", Realm.Earth, EncounterPoolTier.Normal1, MapSize.Small, targetStrength: 2),
                    } },
                { EncounterPoolTier.Normal2, new List<Encounter>
                    {
                        new Encounter("Roving Pack",  Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small, targetStrength: 6),
                        new Encounter("Ambush Party", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small, targetStrength: 6),
                    } },
                { EncounterPoolTier.Elite1, new List<Encounter>
                    {
                        new Encounter("Elite Warband", Realm.Earth, EncounterPoolTier.Elite1, MapSize.Medium, targetStrength: 12),
                    } },
                { EncounterPoolTier.Boss1, new List<Encounter>
                    {
                        new Encounter("The Warlord's Last Stand", Realm.Earth, EncounterPoolTier.Boss1, MapSize.Large, targetStrength: 20),
                    } },
            };

        /// <summary>Returns a random encounter for <paramref name="realm"/>/<paramref name="tier"/>,
        /// falling back to the shared default pool for that tier if the realm has no dedicated entries.</summary>
        public static Encounter GetRandom(Realm realm, EncounterPoolTier tier)
        {
            List<Encounter> pool = _pools.TryGetValue((realm, tier), out List<Encounter> realmPool) && realmPool.Count > 0
                ? realmPool
                : _defaultPools[tier];

            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        /// <summary>Registers an additional encounter under its own (Realm, Tier) pool.</summary>
        public static void Register(Encounter encounter)
        {
            if (encounter == null) return;
            var key = (encounter.Realm, encounter.Tier);
            if (!_pools.ContainsKey(key))
                _pools[key] = new List<Encounter>();
            _pools[key].Add(encounter);
        }
    }
}
