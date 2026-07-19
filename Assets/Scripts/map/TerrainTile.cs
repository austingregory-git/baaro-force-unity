using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Map
{
    public class TerrainTile
    {
        public int X { get; set; }
        public int Y { get; set; }
        public TerrainType Terrain { get; set; }

        public TerrainTile(int x, int y, TerrainType terrainType)
        {
            this.X = x;
            this.Y = y;
            this.Terrain = terrainType;
        }
        public enum TerrainType
        {
            Grass,
            Forest,
            Mountain,
            Water,
            Desert,
            Swamp,
            Volcano,
            Snow,
            Plains,
            Void,
            Ash,
            Lava,
            Creek,
            Ocean,
            Meadow,
            Tundra,
        }
    }
}
