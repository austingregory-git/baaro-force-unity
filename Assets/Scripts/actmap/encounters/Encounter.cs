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

        public Encounter(string name, Realm realm, EncounterPoolTier tier, MapSize gridSize, List<Func<Npc>> enemies)
        {
            Name = name;
            Realm = realm;
            Tier = tier;
            GridSize = gridSize;
            Enemies = enemies;
        }
    }
}
