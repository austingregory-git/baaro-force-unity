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
                Realm.Earth, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.Grass,    30 },
                    { TerrainTile.TerrainType.Forest,   25 },
                    { TerrainTile.TerrainType.Mountain, 20 },
                    { TerrainTile.TerrainType.Plains,   15 },
                    { TerrainTile.TerrainType.Water,     5 },
                    { TerrainTile.TerrainType.Swamp,     3 },
                    { TerrainTile.TerrainType.Desert,    2 },
                    { TerrainTile.TerrainType.Snow,      0 },
                    { TerrainTile.TerrainType.Volcano,   0 },
                    { TerrainTile.TerrainType.Void,      0 },
                }
            },
            {
                Realm.Fire, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.Volcano,  30 },
                    { TerrainTile.TerrainType.Desert,   25 },
                    { TerrainTile.TerrainType.Mountain, 15 },
                    { TerrainTile.TerrainType.Plains,   10 },
                    { TerrainTile.TerrainType.Grass,     8 },
                    { TerrainTile.TerrainType.Swamp,     5 },
                    { TerrainTile.TerrainType.Forest,    5 },
                    { TerrainTile.TerrainType.Water,     2 },
                    { TerrainTile.TerrainType.Snow,      0 },
                    { TerrainTile.TerrainType.Void,      0 },
                }
            },
            {
                Realm.Water, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.Water,    35 },
                    { TerrainTile.TerrainType.Swamp,    20 },
                    { TerrainTile.TerrainType.Plains,   15 },
                    { TerrainTile.TerrainType.Grass,    10 },
                    { TerrainTile.TerrainType.Forest,    8 },
                    { TerrainTile.TerrainType.Snow,      5 },
                    { TerrainTile.TerrainType.Mountain,  4 },
                    { TerrainTile.TerrainType.Desert,    2 },
                    { TerrainTile.TerrainType.Volcano,   1 },
                    { TerrainTile.TerrainType.Void,      0 },
                }
            },
            {
                Realm.Wind, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.Plains,   30 },
                    { TerrainTile.TerrainType.Grass,    20 },
                    { TerrainTile.TerrainType.Snow,     15 },
                    { TerrainTile.TerrainType.Mountain, 15 },
                    { TerrainTile.TerrainType.Desert,   10 },
                    { TerrainTile.TerrainType.Forest,    5 },
                    { TerrainTile.TerrainType.Water,     3 },
                    { TerrainTile.TerrainType.Swamp,     2 },
                    { TerrainTile.TerrainType.Volcano,   0 },
                    { TerrainTile.TerrainType.Void,      0 },
                }
            },
            {
                Realm.Dark, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.Void,     35 },
                    { TerrainTile.TerrainType.Swamp,    25 },
                    { TerrainTile.TerrainType.Forest,   15 },
                    { TerrainTile.TerrainType.Mountain, 10 },
                    { TerrainTile.TerrainType.Water,     5 },
                    { TerrainTile.TerrainType.Volcano,   5 },
                    { TerrainTile.TerrainType.Plains,    3 },
                    { TerrainTile.TerrainType.Grass,     2 },
                    { TerrainTile.TerrainType.Desert,    0 },
                    { TerrainTile.TerrainType.Snow,      0 },
                }
            },
            {
                Realm.Light, new Dictionary<TerrainTile.TerrainType, int>
                {
                    { TerrainTile.TerrainType.Plains,   30 },
                    { TerrainTile.TerrainType.Snow,     25 },
                    { TerrainTile.TerrainType.Grass,    20 },
                    { TerrainTile.TerrainType.Water,    10 },
                    { TerrainTile.TerrainType.Mountain,  8 },
                    { TerrainTile.TerrainType.Forest,    5 },
                    { TerrainTile.TerrainType.Desert,    2 },
                    { TerrainTile.TerrainType.Swamp,     0 },
                    { TerrainTile.TerrainType.Volcano,   0 },
                    { TerrainTile.TerrainType.Void,      0 },
                }
            },
        };

        public static Dictionary<TerrainTile.TerrainType, int> GetWeights(Realm realm)
        {
            return weights[realm];
        }
    }
}
