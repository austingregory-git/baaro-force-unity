using System;
using System.Collections.Generic;
using BaaroForce.Map;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Static helpers that calculate which tiles are affected by each <see cref="SpellAreaType"/>.
    ///
    /// Every method receives the caster tile, the tile the player aimed at, the AoE half-width
    /// (<c>area</c> from <see cref="Spell.Area"/> — 0 = single tile, 1 = ±1 = 3 tiles, …), and
    /// the full grid so boundary checks can be performed.
    ///
    /// Use <see cref="GetAreaTiles"/> as the single dispatch point; add a case here whenever a
    /// new <see cref="SpellAreaType"/> is implemented.
    /// </summary>
    public static class SpellAreaUtils
    {
        // ------------------------------------------------------------------ //
        // Dispatch                                                            //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns all tiles that should be affected by <paramref name="spell"/>, given the
        /// caster's tile and the tile the player aimed at.
        /// </summary>
        public static List<MapTile> GetAreaTiles(Spell spell, MapTile casterTile,
                                                  MapTile targetTile,
                                                  MapTile[,] allTiles, int gridSize)
        {
            switch (spell.AreaType)
            {
                case SpellAreaType.HorizontalLine:
                    return GetHorizontalLineTiles(casterTile, targetTile, spell.Range,
                                                  spell.Area, allTiles, gridSize);
                default:
                    // Unimplemented area types fall back to single-tile targeting.
                    return new List<MapTile> { targetTile };
            }
        }

        // ------------------------------------------------------------------ //
        // HorizontalLine                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the tiles in a line that is perpendicular to the attack direction,
        /// centred on <paramref name="targetTile"/> and extending <paramref name="area"/>
        /// tiles on each side (total width = 2 × area + 1).
        ///
        /// "Horizontal" is defined relative to the attacker: when attacking north or south
        /// the line runs east–west; when attacking east or west the line runs north–south.
        ///
        /// Examples (area = 3, range = 1):
        /// <code>
        ///   Caster at (5,5), target (5,6) — attacking north:
        ///     → (4,6)  (5,6)  (6,6)     [row at z = 6]
        ///
        ///   Caster at (5,5), target (6,5) — attacking east:
        ///     → (6,4)  (6,5)  (6,6)     [column at x = 6]
        /// </code>
        /// </summary>
        public static List<MapTile> GetHorizontalLineTiles(
            MapTile casterTile, 
            MapTile targetTile,
            int range,                                              
            int area,
            MapTile[,] allTiles, int gridSize)
        {
            var result = new List<MapTile>();

            int dx = targetTile.GridX - casterTile.GridX;
            int dz = targetTile.GridZ - casterTile.GridZ;

            // Normalise to a unit cardinal direction.
            int ndx = Math.Sign(dx);
            int ndz = Math.Sign(dz);

            // Rotate 90° to get the perpendicular (spread) direction.
            // Attacking along Z → spread along X, and vice-versa.
            int perpX = -ndz;
            int perpZ =  ndx;

            int tx = targetTile.GridX;
            int tz = targetTile.GridZ;

            for (int k = -(area-1)/2; k <= (area-1)/2; k++)
            {
                int x = tx + k * perpX;
                int z = tz + k * perpZ;
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    result.Add(allTiles[x, z]);
            }

            return result;
        }

        public static List<MapTile> GetCircleAroundUnitTiles(MapTile casterTile, int radius, MapTile[,] allTiles, int gridSize)
        {
            var result = new List<MapTile>();

            int cx = casterTile.GridX;
            int cz = casterTile.GridZ;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = cx + dx;
                    int z = cz + dz;

                    if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    {
                        // Check if the tile is within the circle radius
                        if (dx * dx + dz * dz <= radius * radius)
                        {
                            result.Add(allTiles[x, z]);
                        }
                    }
                }
            }

            return result;
        }
    }
}
