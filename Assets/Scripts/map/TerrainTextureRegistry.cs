using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Map
{
    /// <summary>
    /// Resources paths for each TerrainType's tile texture.
    /// Textures live under Assets/Resources/Terrain/ and are loaded via
    /// Resources.Load, same convention as Character.CharacterSpriteKit's sprite paths.
    /// </summary>
    public static class TerrainTextureRegistry
    {
        private static readonly Dictionary<TerrainTile.TerrainType, string> paths =
            new Dictionary<TerrainTile.TerrainType, string>
        {
            { TerrainTile.TerrainType.Grass,    "Terrain/Grass_512" },
            { TerrainTile.TerrainType.Forest,   "Terrain/Forest_512" },
            { TerrainTile.TerrainType.Mountain, "Terrain/Mountain_512" },
            { TerrainTile.TerrainType.Water,    "Terrain/Water_512" },
            { TerrainTile.TerrainType.Desert,   "Terrain/Desert_512" },
            { TerrainTile.TerrainType.Swamp,    "Terrain/Swamp_512" },
            { TerrainTile.TerrainType.Volcano,  "Terrain/Volcano_512" },
            { TerrainTile.TerrainType.Snow,     "Terrain/Snow_512" },
            { TerrainTile.TerrainType.Plains,   "Terrain/Plains_512" },
            { TerrainTile.TerrainType.Void,     "Terrain/Void_512" },
            { TerrainTile.TerrainType.Ash,      "Terrain/Ash_512" },
            { TerrainTile.TerrainType.Lava,     "Terrain/Lava_512" },
            { TerrainTile.TerrainType.Tundra,   "Terrain/Tundra_512" },
            { TerrainTile.TerrainType.Creek,    "Terrain/Creek_512" },
            { TerrainTile.TerrainType.Ocean,    "Terrain/Ocean_512" },
            { TerrainTile.TerrainType.Meadow,   "Terrain/Meadow_512" },
        };

        /// <summary>Loads (and Resources-caches) the tile texture for a terrain type.</summary>
        public static Texture2D GetTexture(TerrainTile.TerrainType type)
        {
            return Resources.Load<Texture2D>(paths[type]);
        }
    }
}
