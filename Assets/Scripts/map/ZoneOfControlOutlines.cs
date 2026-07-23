using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>
    /// Draws/clears one boundary-outline GameObject per living enemy's Zone of Control (a thin
    /// world-space LineRenderer traced around the enemy's whole 3x3 zone, clipped to the grid
    /// edge) while the player is browsing move range. Owns the outline GameObjects itself rather
    /// than any single MapTile, since each spans more than one tile. Extracted from TurnManager
    /// since this is pure rendering with no turn/UI state of its own.
    /// </summary>
    public class ZoneOfControlOutlines
    {
        /// <summary>Light red — distinct from the gold hover outline (MapTile.HoverHighlightColor)
        /// and darker/more saturated than the attack-range red (MapTile.AttackHighlightColor).</summary>
        private static readonly Color OutlineColor = new Color(1f, 0.4f, 0.4f, 0.85f);

        private readonly Transform _parent;
        private readonly int _gridSize;
        private readonly float _step;
        private readonly float _originX;
        private readonly float _originZ;
        private readonly List<GameObject> _outlines = new List<GameObject>();

        public ZoneOfControlOutlines(Transform parent, int gridSize, float step, float originX, float originZ)
        {
            _parent = parent;
            _gridSize = gridSize;
            _step = step;
            _originX = originX;
            _originZ = originZ;
        }

        /// <summary>Draws one outline per living enemy of <paramref name="mover"/>, regardless of
        /// which of its zone tiles are reachable — the outline is a thin line layered on top of
        /// whatever fill (or no fill) is already on those tiles, so it never needs to avoid or
        /// recolor the move highlight.</summary>
        public void DrawForEnemiesOf(Character mover, MapTile[,] tiles)
        {
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                {
                    MapTile t = tiles[x, z];
                    Character occupant = t.OccupyingUnit;
                    if (occupant == null || !GridPathfinder.IsEnemyOf(mover, occupant)) continue;

                    DrawOutline(t);
                }
        }

        public void Clear()
        {
            foreach (GameObject go in _outlines)
                Object.Destroy(go);
            _outlines.Clear();
        }

        /// <summary>One big rectangular outline around <paramref name="enemyTile"/>'s whole 3x3
        /// Zone of Control (clipped to the grid edge), rather than a separate outline per tile.</summary>
        private void DrawOutline(MapTile enemyTile)
        {
            var (xMin, xMax, zMin, zMax, y) = ComputeBounds(enemyTile);

            var go = new GameObject("ZoneOfControlOutline");
            go.transform.SetParent(_parent, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = 4;
            lr.SetPositions(new[]
            {
                new Vector3(xMin, y, zMin),
                new Vector3(xMax, y, zMin),
                new Vector3(xMax, y, zMax),
                new Vector3(xMin, y, zMax),
            });
            lr.widthMultiplier = 0.06f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = OutlineColor;

            _outlines.Add(go);
        }

        /// <summary>World-space rectangle bounds of <paramref name="enemyTile"/>'s 3x3 zone,
        /// reusing the same origin/step grid-to-world math as TurnManager's tile-under-mouse
        /// raycast.</summary>
        private (float xMin, float xMax, float zMin, float zMax, float y) ComputeBounds(MapTile enemyTile)
        {
            int minX = Mathf.Max(0, enemyTile.GridX - 1);
            int maxX = Mathf.Min(_gridSize - 1, enemyTile.GridX + 1);
            int minZ = Mathf.Max(0, enemyTile.GridZ - 1);
            int maxZ = Mathf.Min(_gridSize - 1, enemyTile.GridZ + 1);

            float halfStep = _step * 0.5f;
            float y = enemyTile.transform.position.y + enemyTile.transform.lossyScale.y * 0.5f + 0.03f;

            return (
                _originX + minX * _step - halfStep,
                _originX + maxX * _step + halfStep,
                _originZ + minZ * _step - halfStep,
                _originZ + maxZ * _step + halfStep,
                y);
        }
    }
}
