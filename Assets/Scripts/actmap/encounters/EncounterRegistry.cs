using System;
using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Map;
using BaaroForce.Utils;

namespace BaaroForce.ActMap.Encounters
{
    /// <summary>
    /// Central registry of <see cref="Encounter"/> pools, keyed by (Realm, Tier) per the Act 1
    /// design doc's "Fight Pools" section. The doc calls for 2-3 unique encounters per realm
    /// per pool (50+ total); only a handful of Npcs exist in <see cref="NpcRegistry"/> today,
    /// so most realms fall back to a shared default pool per tier rather than having dedicated
    /// entries. Earth has real entries since that's where the current Npc roster (Wolf,
    /// Mozeem*) thematically lives — their rosters below are a placeholder mix of the existing
    /// Npc types, not a final balance pass; swap them out as real per-encounter composition is
    /// authored. Add dedicated per-realm pools here as that content grows — <see cref="GetRandom"/>
    /// transparently prefers a realm-specific pool the moment one exists.
    /// </summary>
    public static class EncounterRegistry
    {
        private static readonly Dictionary<(Realm, EncounterPoolTier), List<Encounter>> _pools =
            new Dictionary<(Realm, EncounterPoolTier), List<Encounter>>
            {
                { (Realm.Earth, EncounterPoolTier.Normal1), new List<Encounter>
                    {
                        // First hand-drawn map in the game — see Assets/Resources/Maps/wolf_den.map
                        // and MapLayoutParser for the file format. Enemies/deployment zone come
                        // from the file's own [UNITS] section, so the "enemies" list below is a
                        // dormant fallback only (used if the file ever fails to load/parse).
                        new Encounter("Wolf Den", Realm.Earth, EncounterPoolTier.Normal1, MapSize.Small,
                            enemies: new List<Func<Npc>>
                            { () => new Wolf(),
                              () => new Wolf(),
                              () => new Wolf(),
                              () => new AlphaWolf() },
                            mapFile: "Maps/wolf_den"),
                        new Encounter("Mozeem Outpost", Realm.Earth, EncounterPoolTier.Normal1, MapSize.Small,
                            enemies: new List<Func<Npc>> { () => new MozeemGuardian(), () => new MozeemArcher() }),
                    } },
                { (Realm.Earth, EncounterPoolTier.Normal2), new List<Encounter>
                    {
                        new Encounter("Wolf Pack", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small,
                            enemies: new List<Func<Npc>> { 
                                () => new Wolf(), 
                                () => new Wolf(), 
                                () => new Wolf(),
                                () => new AlphaWolf() }),
                        new Encounter("Mozeem Patrol", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small,
                            enemies: new List<Func<Npc>> { () => new MozeemGuardian(), () => new MozeemArcher(), () => new MozeemArcher() }),
                        new Encounter("Mozeem Watch Camp", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small,
                            enemies: new List<Func<Npc>> { () => new MozeemGuardian(), () => new MozeemGuardian(), () => new MozeemElder() }),
                    } },
                { (Realm.Earth, EncounterPoolTier.Elite1), new List<Encounter>
                    {
                        new Encounter("Mozeem Guardian's Stand", Realm.Earth, EncounterPoolTier.Elite1, MapSize.Medium,
                            enemies: new List<Func<Npc>>
                            {
                                () => new MozeemGuardian(), () => new MozeemGuardian(),
                                () => new MozeemElder(), () => new MozeemArcher(),
                            }),
                    } },
                { (Realm.Earth, EncounterPoolTier.Boss1), new List<Encounter>
                    {
                        new Encounter("The Mozeem Elder's Sanctum", Realm.Earth, EncounterPoolTier.Boss1, MapSize.Large,
                            enemies: new List<Func<Npc>>
                            {
                                () => new MozeemElder(), () => new MozeemGuardian(), () => new MozeemGuardian(),
                                () => new MozeemArcher(), () => new MozeemArcher(),
                            }),
                    } },
            };

        // Shared fallback pools, used whenever a realm has no dedicated entry for a tier.
        private static readonly Dictionary<EncounterPoolTier, List<Encounter>> _defaultPools =
            new Dictionary<EncounterPoolTier, List<Encounter>>
            {
                { EncounterPoolTier.Normal1, new List<Encounter>
                    {
                        new Encounter("Wandering Beasts", Realm.Earth, EncounterPoolTier.Normal1, MapSize.Small,
                            enemies: new List<Func<Npc>> { 
                                () => new Wolf(), 
                                () => new Wolf(),
                                () => new Wolf(),
                                () => new AlphaWolf(),
                            }),
                    } },
                { EncounterPoolTier.Normal2, new List<Encounter>
                    {
                        new Encounter("Roving Pack", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small,
                            enemies: new List<Func<Npc>> { () => new Wolf(), () => new Wolf(), () => new MozeemArcher() }),
                        new Encounter("Ambush Party", Realm.Earth, EncounterPoolTier.Normal2, MapSize.Small,
                            enemies: new List<Func<Npc>> { () => new MozeemGuardian(), () => new MozeemArcher(), () => new Wolf() }),
                    } },
                { EncounterPoolTier.Elite1, new List<Encounter>
                    {
                        new Encounter("Elite Warband", Realm.Earth, EncounterPoolTier.Elite1, MapSize.Medium,
                            enemies: new List<Func<Npc>> { () => new MozeemGuardian(), () => new MozeemElder(), () => new MozeemArcher() }),
                    } },
                { EncounterPoolTier.Boss1, new List<Encounter>
                    {
                        new Encounter("The Warlord's Last Stand", Realm.Earth, EncounterPoolTier.Boss1, MapSize.Large,
                            enemies: new List<Func<Npc>>
                            {
                                () => new MozeemElder(), () => new MozeemGuardian(),
                                () => new MozeemGuardian(), () => new MozeemArcher(),
                            }),
                    } },
            };

        // An encounter drawn here won't be offered again until every other encounter in
        // that same (realm, tier) — or shared default-tier — pool has had a turn. Keyed
        // separately from _pools/_defaultPools so a realm falling back to the default pool
        // for one tier doesn't share history with a realm that has real entries for it.
        private static readonly Dictionary<(Realm, EncounterPoolTier), HashSet<string>> _shownByRealmPool =
            new Dictionary<(Realm, EncounterPoolTier), HashSet<string>>();
        private static readonly Dictionary<EncounterPoolTier, HashSet<string>> _shownByDefaultTier =
            new Dictionary<EncounterPoolTier, HashSet<string>>();

        private static HashSet<string> ShownSet<TKey>(Dictionary<TKey, HashSet<string>> byKey, TKey key)
        {
            if (!byKey.TryGetValue(key, out HashSet<string> set))
                byKey[key] = set = new HashSet<string>();
            return set;
        }

        /// <summary>Clears the shown-encounter cycle for every pool. Call at the start of a
        /// new run so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetShownHistory()
        {
            _shownByRealmPool.Clear();
            _shownByDefaultTier.Clear();
        }

        /// <summary>Returns a random encounter for <paramref name="realm"/>/<paramref name="tier"/>,
        /// falling back to the shared default pool for that tier if the realm has no dedicated entries.</summary>
        public static Encounter GetRandom(Realm realm, EncounterPoolTier tier)
        {
            bool hasRealmPool = _pools.TryGetValue((realm, tier), out List<Encounter> realmPool) && realmPool.Count > 0;
            List<Encounter> pool = hasRealmPool ? realmPool : _defaultPools[tier];
            HashSet<string> shown = hasRealmPool
                ? ShownSet(_shownByRealmPool, (realm, tier))
                : ShownSet(_shownByDefaultTier, tier);

            return WeightedCyclePicker.PickOne(pool, identity: e => e.Name, weight: _ => 1f, shown);
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
