using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.Map
{
    /// <summary>
    /// Integer weights for each TerrainType per Realm.
    /// Higher weight = more likely to be chosen during procedural generation.
    /// </summary>
    public static class RealmTerrainWeights
    {
        private static readonly Dictionary<Realm, Dictionary<TerrainTile.TerrainType, int>> weights =
            new Dictionary<Realm, Dictionary<TerrainTile.TerrainType, int>>
        {
            {
                Realm.EARTH, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.GRASS,    30 },
                    { TerrainTile.TerrainType.FOREST,   25 },
                    { TerrainTile.TerrainType.MOUNTAIN, 20 },
                    { TerrainTile.TerrainType.PLAINS,   15 },
                    { TerrainTile.TerrainType.WATER,     5 },
                    { TerrainTile.TerrainType.SWAMP,     3 },
                    { TerrainTile.TerrainType.DESERT,    2 },
                    { TerrainTile.TerrainType.SNOW,      0 },
                    { TerrainTile.TerrainType.VOLCANO,   0 },
                    { TerrainTile.TerrainType.VOID,      0 },
                }
            },
            {
                Realm.FIRE, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.VOLCANO,  30 },
                    { TerrainTile.TerrainType.DESERT,   25 },
                    { TerrainTile.TerrainType.MOUNTAIN, 15 },
                    { TerrainTile.TerrainType.PLAINS,   10 },
                    { TerrainTile.TerrainType.GRASS,     8 },
                    { TerrainTile.TerrainType.SWAMP,     5 },
                    { TerrainTile.TerrainType.FOREST,    5 },
                    { TerrainTile.TerrainType.WATER,     2 },
                    { TerrainTile.TerrainType.SNOW,      0 },
                    { TerrainTile.TerrainType.VOID,      0 },
                }
            },
            {
                Realm.WATER, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.WATER,    35 },
                    { TerrainTile.TerrainType.SWAMP,    20 },
                    { TerrainTile.TerrainType.PLAINS,   15 },
                    { TerrainTile.TerrainType.GRASS,    10 },
                    { TerrainTile.TerrainType.FOREST,    8 },
                    { TerrainTile.TerrainType.SNOW,      5 },
                    { TerrainTile.TerrainType.MOUNTAIN,  4 },
                    { TerrainTile.TerrainType.DESERT,    2 },
                    { TerrainTile.TerrainType.VOLCANO,   1 },
                    { TerrainTile.TerrainType.VOID,      0 },
                }
            },
            {
                Realm.WIND, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.PLAINS,   30 },
                    { TerrainTile.TerrainType.GRASS,    20 },
                    { TerrainTile.TerrainType.SNOW,     15 },
                    { TerrainTile.TerrainType.MOUNTAIN, 15 },
                    { TerrainTile.TerrainType.DESERT,   10 },
                    { TerrainTile.TerrainType.FOREST,    5 },
                    { TerrainTile.TerrainType.WATER,     3 },
                    { TerrainTile.TerrainType.SWAMP,     2 },
                    { TerrainTile.TerrainType.VOLCANO,   0 },
                    { TerrainTile.TerrainType.VOID,      0 },
                }
            },
            {
                Realm.DARK, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.VOID,     35 },
                    { TerrainTile.TerrainType.SWAMP,    25 },
                    { TerrainTile.TerrainType.FOREST,   15 },
                    { TerrainTile.TerrainType.MOUNTAIN, 10 },
                    { TerrainTile.TerrainType.WATER,     5 },
                    { TerrainTile.TerrainType.VOLCANO,   5 },
                    { TerrainTile.TerrainType.PLAINS,    3 },
                    { TerrainTile.TerrainType.GRASS,     2 },
                    { TerrainTile.TerrainType.DESERT,    0 },
                    { TerrainTile.TerrainType.SNOW,      0 },
                }
            },
            {
                Realm.LIGHT, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.PLAINS,   30 },
                    { TerrainTile.TerrainType.SNOW,     25 },
                    { TerrainTile.TerrainType.GRASS,    20 },
                    { TerrainTile.TerrainType.WATER,    10 },
                    { TerrainTile.TerrainType.MOUNTAIN,  8 },
                    { TerrainTile.TerrainType.FOREST,    5 },
                    { TerrainTile.TerrainType.DESERT,    2 },
                    { TerrainTile.TerrainType.SWAMP,     0 },
                    { TerrainTile.TerrainType.VOLCANO,   0 },
                    { TerrainTile.TerrainType.VOID,      0 },
                }
            },
        };

        public static Dictionary<TerrainTile.TerrainType, int> GetWeights(Realm realm)
        {
            return weights[realm];
        }
    }
}
