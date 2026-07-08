using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTile
{
    public int x { get; set; }
    public int y { get; set; }
    public TerrainType terrainType { get; set; }

    public TerrainTile(int x, int y, TerrainType terrainType)
    {
        this.x = x;
        this.y = y;
        this.terrainType = terrainType;
    }
    public enum TerrainType
    {
        GRASS,
        FOREST,
        MOUNTAIN,
        WATER,
        DESERT,
        SWAMP,
        VOLCANO,
        SNOW,
        PLAINS,
        VOID,
        ASH,
        LAVA,
        CREEK,
        OCEAN,
        MEADOW,
        TUNDRA,
    }
}
