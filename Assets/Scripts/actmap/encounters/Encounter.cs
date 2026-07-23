using System;
using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.ActMap.Encounters
{
    /// <summary>
    /// One entry in an <see cref="EncounterRegistry"/> pool — a realm/tier-scoped, hand-authored
    /// enemy roster consumed by MapGenerator when a Fight/Elite/Boss node is entered.
    /// <see cref="Enemies"/> is a list of factory functions (same convention as
    /// CharacterRegistry/NpcRegistry — each draw gets a fresh instance) rather than a
    /// strength budget, so the exact composition of an encounter is a deliberate authoring
    /// choice, not a random draw from the whole Npc roster.
    /// </summary>
    public class Encounter
    {
        public string Name { get; }
        public Realm Realm { get; }
        public EncounterPoolTier Tier { get; }
        public MapSize GridSize { get; }
        public List<Func<Npc>> Enemies { get; }

        /// <summary>Resources path to a hand-drawn .map file (e.g. "Maps/wolf_den" for
        /// Assets/Resources/Maps/wolf_den.map) — see MapLayoutParser for the file format. Null
        /// (the default) means this encounter still generates its map procedurally from
        /// <see cref="GridSize"/>/Realm the way every encounter did before hand-drawn maps
        /// existed. When set, MapGenerator builds the map from the file instead; <see
        /// cref="GridSize"/> is then ignored (the file's own WIDTH/HEIGHT wins), and <see
        /// cref="Enemies"/> is only used as a fallback if the file's [UNITS] section doesn't
        /// place any enemies itself.</summary>
        public string MapFile { get; }

        public Encounter(string name, Realm realm, EncounterPoolTier tier, MapSize gridSize,
            List<Func<Npc>> enemies, string mapFile = null)
        {
            Name = name;
            Realm = realm;
            Tier = tier;
            GridSize = gridSize;
            Enemies = enemies;
            MapFile = mapFile;
        }
    }
}
