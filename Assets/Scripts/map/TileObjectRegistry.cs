using System.Collections.Generic;

namespace BaaroForce.Map
{
    /// <summary>Gameplay-relevant data for a single TileObjectType — same shape/purpose as
    /// TerrainInfo, just for the independent object layer. Starting numbers, easy to retune.</summary>
    public class TileObjectInfo
    {
        public string DisplayName;
        public string Description;
        /// <summary>Whether a unit can move onto a tile occupied by this object, regardless of
        /// the terrain underneath — see MapTile.IsPassable, the single choke point that
        /// combines this with TerrainInfoRegistry.IsPassable.</summary>
        public bool IsPassable = true;
    }

    public static class TileObjectRegistry
    {
        private static readonly Dictionary<TileObjectType, TileObjectInfo> info =
            new Dictionary<TileObjectType, TileObjectInfo>
        {
            { TileObjectType.None,    new TileObjectInfo { DisplayName = "None",    Description = "Nothing here." } },
            { TileObjectType.Boulder, new TileObjectInfo { DisplayName = "Boulder", Description = "A slab of rock too large to move around.", IsPassable = false } },
            { TileObjectType.Crate,   new TileObjectInfo { DisplayName = "Crate",   Description = "A stacked supply crate." } },
            { TileObjectType.Rubble,  new TileObjectInfo { DisplayName = "Rubble",  Description = "Loose debris underfoot." } },
        };

        public static TileObjectInfo Get(TileObjectType type) => info[type];

        public static bool IsPassable(TileObjectType type) => Get(type).IsPassable;
    }
}
