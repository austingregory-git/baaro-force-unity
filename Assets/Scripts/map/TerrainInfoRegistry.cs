using System.Collections.Generic;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>Gameplay-relevant data for a single TerrainType — drives both the tile
    /// info panel and real mechanics (movement cost via TurnManager.StepCost, regen via
    /// TurnManager's turn-start hooks, passability via TurnManager's pathfinding).</summary>
    public class TerrainInfo
    {
        public string DisplayName;
        public string Description;
        /// <summary>Movement points spent to step onto a tile of this type.</summary>
        public int MovementCost = 1;
        /// <summary>HP healed at the start of a unit's turn while standing on this type.</summary>
        public int RegenPerTurn = 0;
        /// <summary>Whether a unit can move onto a tile of this type by default. See
        /// TerrainInfoRegistry.IsPassable — the place a future flying/movement-type
        /// override plugs in without touching pathfinding call sites.</summary>
        public bool IsPassable = true;
    }

    /// <summary>
    /// Per-TerrainType gameplay data, keyed the same way as RealmTerrainWeights/
    /// TerrainTextureRegistry. Starting numbers — easy to retune since it's one table.
    /// </summary>
    public static class TerrainInfoRegistry
    {
        private static readonly Dictionary<TerrainTile.TerrainType, TerrainInfo> info =
            new Dictionary<TerrainTile.TerrainType, TerrainInfo>
        {
            { TerrainTile.TerrainType.Grass,    new TerrainInfo { DisplayName = "Grass",    Description = "Open, easy footing." } },
            { TerrainTile.TerrainType.Forest,   new TerrainInfo { DisplayName = "Forest",   Description = "Dense undergrowth slows movement.", MovementCost = 2 } },
            { TerrainTile.TerrainType.Mountain, new TerrainInfo { DisplayName = "Mountain", Description = "Sheer rock — impassable on foot.", IsPassable = false } },
            { TerrainTile.TerrainType.Water,    new TerrainInfo { DisplayName = "Water",    Description = "Deep water — impassable on foot.", IsPassable = false } },
            { TerrainTile.TerrainType.Desert,   new TerrainInfo { DisplayName = "Desert",   Description = "Loose, sun-baked sand." } },
            { TerrainTile.TerrainType.Swamp,    new TerrainInfo { DisplayName = "Swamp",    Description = "Thick mud saps momentum.", MovementCost = 2 } },
            { TerrainTile.TerrainType.Volcano,  new TerrainInfo { DisplayName = "Volcano",  Description = "Scorched volcanic rock." } },
            { TerrainTile.TerrainType.Snow,     new TerrainInfo { DisplayName = "Snow",     Description = "Deep drifts slow every step.", MovementCost = 2 } },
            { TerrainTile.TerrainType.Plains,   new TerrainInfo { DisplayName = "Plains",   Description = "Flat, open grassland." } },
            { TerrainTile.TerrainType.Void,     new TerrainInfo { DisplayName = "Void",     Description = "Reality frays at the edges." } },
            { TerrainTile.TerrainType.Ash,      new TerrainInfo { DisplayName = "Ash",      Description = "Fine ash blankets the ground." } },
            { TerrainTile.TerrainType.Lava,     new TerrainInfo { DisplayName = "Lava",     Description = "Molten rock — dangerous to cross." } },
            { TerrainTile.TerrainType.Creek,    new TerrainInfo { DisplayName = "Creek",    Description = "Shallow but slow going.", MovementCost = 2, RegenPerTurn = 1 } },
            { TerrainTile.TerrainType.Ocean,    new TerrainInfo { DisplayName = "Ocean",    Description = "Deep, open water — impassable on foot.", IsPassable = false } },
            { TerrainTile.TerrainType.Meadow,   new TerrainInfo { DisplayName = "Meadow",   Description = "A peaceful, restorative clearing.", RegenPerTurn = 1 } },
            { TerrainTile.TerrainType.Tundra,   new TerrainInfo { DisplayName = "Tundra",   Description = "Frozen, wind-scoured ground." } },
        };

        public static TerrainInfo Get(TerrainTile.TerrainType type) => info[type];

        /// <summary>Whether <paramref name="mover"/> can enter a tile of type <paramref
        /// name="type"/>. Single choke point for terrain passability — currently just
        /// TerrainInfo.IsPassable, but the place a future flying/movement-type override
        /// (e.g. mover.CanFly) will plug in without touching pathfinding call sites.</summary>
        public static bool IsPassable(TerrainTile.TerrainType type, Character mover) =>
            Get(type).IsPassable;
    }
}
