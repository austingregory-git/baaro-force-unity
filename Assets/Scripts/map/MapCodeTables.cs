using System;
using System.Collections.Generic;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>
    /// Short, hand-writable codes for everything a .map file's three grids can reference —
    /// see MapLayoutParser for the file format itself. Centralized here (rather than per-file
    /// legends) so every .map file stays terse and codes stay consistent across all of them;
    /// this is the cheat sheet to keep next to you while authoring a map by hand.
    /// </summary>
    public static class MapCodeTables
    {
        // ------------------------------------------------------------------ //
        // [TERRAIN] — one of these, every cell, no blanks allowed.            //
        // ------------------------------------------------------------------ //
        public static readonly Dictionary<string, TerrainTile.TerrainType> Terrain =
            new Dictionary<string, TerrainTile.TerrainType>
        {
            { "GR", TerrainTile.TerrainType.Grass    },
            { "FO", TerrainTile.TerrainType.Forest   },
            { "MT", TerrainTile.TerrainType.Mountain },
            { "WA", TerrainTile.TerrainType.Water    },
            { "DE", TerrainTile.TerrainType.Desert   },
            { "SW", TerrainTile.TerrainType.Swamp    },
            { "VO", TerrainTile.TerrainType.Volcano  },
            { "SN", TerrainTile.TerrainType.Snow     },
            { "PL", TerrainTile.TerrainType.Plains   },
            { "VD", TerrainTile.TerrainType.Void     },
            { "AS", TerrainTile.TerrainType.Ash      },
            { "LA", TerrainTile.TerrainType.Lava     },
            { "CR", TerrainTile.TerrainType.Creek    },
            { "OC", TerrainTile.TerrainType.Ocean    },
            { "ME", TerrainTile.TerrainType.Meadow   },
            { "TU", TerrainTile.TerrainType.Tundra   },
        };

        // ------------------------------------------------------------------ //
        // [OBJECTS] — ".." (EmptyCode) means no object on that tile.          //
        // ------------------------------------------------------------------ //
        public const string EmptyCode = "..";

        public static readonly Dictionary<string, TileObjectType> Objects =
            new Dictionary<string, TileObjectType>
        {
            { "BO", TileObjectType.Boulder },
            { "CT", TileObjectType.Crate   },
            { "RB", TileObjectType.Rubble  },
        };

        // ------------------------------------------------------------------ //
        // [UNITS] — ".." means empty, "PZ" marks a player deployment tile,   //
        // anything else must be a registered Npc code below.                  //
        // ------------------------------------------------------------------ //
        public const string PlayerDeployCode = "PZ";

        public static readonly Dictionary<string, Func<Npc>> Units =
            new Dictionary<string, Func<Npc>>
        {
            { "WO", () => new Wolf()          },
            { "AW", () => new AlphaWolf()     },
            { "MG", () => new MozeemGuardian()},
            { "MA", () => new MozeemArcher()  },
            { "ME", () => new MozeemElder()   },
        };
    }
}
