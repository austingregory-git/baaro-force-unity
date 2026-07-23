using System;
using System.Collections.Generic;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>A parsed .map file — see MapLayoutParser. Width/Height are always equal today
    /// (the parser enforces a square grid, matching the rest of the pipeline's assumption of
    /// one MapSize-style dimension); kept as two separate fields anyway so a future non-square
    /// grid only needs parser changes, not a MapLayout shape change.</summary>
    public class MapLayout
    {
        public int Width;
        public int Height;

        /// <summary>[x, z] — every cell filled, no gaps.</summary>
        public TerrainTile.TerrainType[,] Terrain;

        /// <summary>[x, z] — TileObjectType.None where the file had "..".</summary>
        public TileObjectType[,] Objects;

        /// <summary>Tiles marked "PZ" in the [UNITS] grid — the player deployment zone for this
        /// map. Empty if the author didn't mark any, in which case MapGenerator falls back to
        /// DeploymentManager's usual fixed-rectangle zone.</summary>
        public List<(int x, int z)> DeploymentTiles = new List<(int x, int z)>();

        /// <summary>Enemy spawns explicitly placed in the [UNITS] grid, in file (row-major)
        /// order. Empty if the author didn't place any, in which case MapGenerator falls back
        /// to the Encounter's own Enemies list and DeploymentManager's random far-half
        /// placement, same as a procedurally generated map.</summary>
        public List<(int x, int z, Func<Npc> factory)> EnemySpawns = new List<(int x, int z, Func<Npc>)>();
    }
}
