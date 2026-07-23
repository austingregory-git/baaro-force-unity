using System;
using System.Collections.Generic;
using BaaroForce.ActMap.Encounters;
using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.ActMap
{
    /// <summary>
    /// Fight/Elite/Boss node configuration written by <c>ActMapController</c> right before
    /// loading <c>MapScene</c>, and read by <c>MapGenerator.Start()</c> in place of its
    /// Inspector defaults — the same override pattern already used for Realm. Also read by
    /// <c>TurnManager</c> at fight-end to scale loot/XP (<see cref="Tier"/>).
    /// </summary>
    public class PendingEncounter
    {
        public Realm Realm { get; set; }
        public EncounterPoolTier Tier { get; set; }
        public MapSize MapSize { get; set; }
        public int EnemyLevel { get; set; }

        /// <summary>The chosen Encounter's hand-authored enemy roster (see
        /// <see cref="Encounter.Enemies"/>) — spawned as-is by MapGenerator, one Npc per
        /// factory, each leveled up via its ApplyLevel.</summary>
        public List<Func<Npc>> Enemies { get; set; }

        /// <summary>Passthrough of <see cref="Encounter.MapFile"/> — null means MapGenerator
        /// builds this fight's map procedurally as it always has; set means it loads and parses
        /// that hand-drawn layout instead. See MapLayoutParser.</summary>
        public string MapFile { get; set; }
    }
}
