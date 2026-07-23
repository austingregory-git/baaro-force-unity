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
                case SpellAreaType.CircleAround:
                    // Always centred on the caster — no tile is aimed.
                    return GetCircleAroundTiles(casterTile, spell.Area, allTiles, gridSize, spell.IncludeOriginTile);
                case SpellAreaType.Cone:
                    return GetConeTiles(casterTile, targetTile, spell.Area, allTiles, gridSize);
                case SpellAreaType.VerticalLine:
                    return GetVerticalLineTiles(casterTile, targetTile, spell.Range, spell.Area, allTiles, gridSize);
                case SpellAreaType.Circle:
                    // Always centred on the caster — no tile is aimed.
                    return GetTrueCircleAreaTiles(casterTile, spell.Range, spell.Area, allTiles, gridSize, spell.IncludeOriginTile);
                case SpellAreaType.Cross:
                    return GetCrossTiles(casterTile, targetTile, spell.Range, spell.Area, allTiles, gridSize);
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

        // ------------------------------------------------------------------ //
        // VerticalLine                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the tiles in a straight, 1-tile-wide line extending forward from
        /// <paramref name="casterTile"/> toward <paramref name="targetTile"/>, <paramref name="area"/>
        /// tiles long — e.g. Arcane Beam's 4-tile beam. "Forward" is restricted to the four
        /// cardinal directions, same as <see cref="GetConeTiles"/> — this grid has no diagonal
        /// movement. <paramref name="range"/> is unused (kept for signature parity with the
        /// other Get*Tiles helpers, which are all called uniformly from <see cref="GetAreaTiles"/>).
        ///
        /// Example (area = 4):
        /// <code>
        ///   Caster at (5,5), target (5,6) — beaming north:
        ///     → (5,6) (5,7) (5,8) (5,9)
        /// </code>
        /// </summary>
        public static List<MapTile> GetVerticalLineTiles(
            MapTile casterTile,
            MapTile targetTile,
            int range,
            int area,
            MapTile[,] allTiles, int gridSize)
        {
            var result = new List<MapTile>();
            if (casterTile == null || targetTile == null) return result;

            int dx = targetTile.GridX - casterTile.GridX;
            int dz = targetTile.GridZ - casterTile.GridZ;

            // Normalise to a unit cardinal direction; default to +Z if aimed at the caster's own tile.
            int ndx = Math.Sign(dx);
            int ndz = Math.Sign(dz);
            if (ndx == 0 && ndz == 0) ndz = 1;

            int tx = targetTile.GridX;
            int tz = targetTile.GridZ;

            for (int d = 0; d < area; d++)
            {
                int x = tx + d * ndx;
                int z = tz + d * ndz;
                if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                    result.Add(allTiles[x, z]);
            }

            return result;
        }

        // ------------------------------------------------------------------ //
        // CircleAround                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns every tile adjacent to and diagonal to <paramref name="casterTile"/> within
        /// <paramref name="radius"/> squares (Chebyshev distance). Radius 1 = 8 tiles (the 3×3
        /// block around the caster minus its centre), radius 2 = 24 tiles (5×5 minus centre),
        /// and so on. The caster's own tile is excluded unless <paramref name="includeCenter"/>
        /// is set — e.g. a self-centred buff like Rally wants to affect its own tile too, while
        /// most enemy-targeted AoE spells don't.
        /// </summary>
        public static List<MapTile> GetCircleAroundTiles(MapTile casterTile, int radius, MapTile[,] allTiles, int gridSize, bool includeCenter = false)
        {
            var result = new List<MapTile>();

            int cx = casterTile.GridX;
            int cz = casterTile.GridZ;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0 && !includeCenter) continue;

                    int x = cx + dx;
                    int z = cz + dz;

                    if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                        result.Add(allTiles[x, z]);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ //
        // Circle (true circle — no diagonals)                                 //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns every tile within Manhattan distance <paramref name="area"/> of
        /// <paramref name="casterTile"/> — a diamond shape that follows this grid's
        /// 4-directional movement, unlike <see cref="GetCircleAroundTiles"/>'s Chebyshev
        /// (diagonal-inclusive) square. Area 1 = 4 tiles, area 2 = 12 tiles, and so on
        /// (2 × area × (area + 1) tiles total). The caster's own tile is excluded unless
        /// <paramref name="includeCenter"/> is set. <paramref name="range"/> is unused
        /// (kept for signature parity with the other Get*Tiles helpers).
        /// </summary>
        public static List<MapTile> GetTrueCircleAreaTiles(
            MapTile casterTile,
            int range,
            int area,
            MapTile[,] allTiles, int gridSize,
            bool includeCenter = false)
        {
            var result = new List<MapTile>();
            if (casterTile == null) return result;

            int cx = casterTile.GridX;
            int cz = casterTile.GridZ;

            for (int dx = -area; dx <= area; dx++)
            {
                int spread = area - Math.Abs(dx);
                for (int dz = -spread; dz <= spread; dz++)
                {
                    if (dx == 0 && dz == 0 && !includeCenter) continue;

                    int x = cx + dx;
                    int z = cz + dz;

                    if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                        result.Add(allTiles[x, z]);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ //
        // Cone                                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns a widening triangular cone of tiles fanning out from
        /// <paramref name="casterTile"/> toward <paramref name="targetTile"/>.
        /// At forward distance <c>d</c> (1..area) the cone is <c>2d-1</c> tiles wide,
        /// centred on the straight line to the target — so an area of N covers
        /// 1 + 3 + 5 + ... + (2N-1) = N² tiles total (area 2 → 4 tiles, area 3 → 9 tiles).
        /// "Forward" is restricted to the four cardinal directions, same as
        /// <see cref="GetHorizontalLineTiles"/> — this grid has no diagonal movement.
        /// </summary>
        public static List<MapTile> GetConeTiles(
            MapTile casterTile,
            MapTile targetTile,
            int area,
            MapTile[,] allTiles, int gridSize)
        {
            var result = new List<MapTile>();
            if (casterTile == null || targetTile == null) return result;

            int dx = targetTile.GridX - casterTile.GridX;
            int dz = targetTile.GridZ - casterTile.GridZ;

            // Normalise to a unit cardinal direction; default to +Z if aimed at the caster's own tile.
            int ndx = Math.Sign(dx);
            int ndz = Math.Sign(dz);
            if (ndx == 0 && ndz == 0) ndz = 1;

            // Rotate 90° to get the perpendicular (spread) direction.
            int perpX = -ndz;
            int perpZ =  ndx;

            int cx = casterTile.GridX;
            int cz = casterTile.GridZ;

            for (int d = 1; d <= area; d++)
            {
                int halfWidth = d - 1;
                for (int k = -halfWidth; k <= halfWidth; k++)
                {
                    int x = cx + d * ndx + k * perpX;
                    int z = cz + d * ndz + k * perpZ;
                    if (x >= 0 && x < gridSize && z >= 0 && z < gridSize)
                        result.Add(allTiles[x, z]);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ //
        // Cross                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns a plus/cross-shaped area centred on <paramref name="targetTile"/>: the
        /// target's own row extended <paramref name="area"/> tiles east/west, plus its own
        /// column extended <paramref name="area"/> tiles north/south. Unlike
        /// <see cref="GetConeTiles"/>/<see cref="GetVerticalLineTiles"/> the shape doesn't
        /// depend on the caster's direction to the target — only where the target tile is.
        /// <paramref name="casterTile"/>/<paramref name="range"/> are unused (kept for
        /// signature parity with the other Get*Tiles helpers, which are all called
        /// uniformly from <see cref="GetAreaTiles"/>); the caster-to-target range is
        /// enforced separately during targeting.
        ///
        /// Example (area = 1) — target at (4,1), caster at (1,1):
        /// <code>
        ///   . . . . X .
        ///   . C . X X X
        ///   . . . . X .
        /// </code>
        /// </summary>
        public static List<MapTile> GetCrossTiles(
            MapTile casterTile,
            MapTile targetTile,
            int range,
            int area,
            MapTile[,] allTiles, int gridSize)
        {
            var result = new List<MapTile>();
            if (targetTile == null) return result;

            int tx = targetTile.GridX;
            int tz = targetTile.GridZ;

            // Horizontal arm — target's row, including the target tile itself.
            for (int dx = -area; dx <= area; dx++)
            {
                int x = tx + dx;
                if (x >= 0 && x < gridSize && tz >= 0 && tz < gridSize)
                    result.Add(allTiles[x, tz]);
            }

            // Vertical arm — target's column, excluding dz == 0 (already added above).
            for (int dz = -area; dz <= area; dz++)
            {
                if (dz == 0) continue;
                int z = tz + dz;
                if (tx >= 0 && tx < gridSize && z >= 0 && z < gridSize)
                    result.Add(allTiles[tx, z]);
            }

            return result;
        }
    }
}
