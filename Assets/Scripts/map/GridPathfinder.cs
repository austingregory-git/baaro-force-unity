using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Spells;

namespace BaaroForce.Map
{
    /// <summary>
    /// Zone-of-Control-aware Dijkstra pathfinding over a <see cref="MapTile"/> grid: reachable-tile
    /// search (<see cref="BfsReachable"/>), cheapest-path search (<see cref="FindShortestPath"/>),
    /// and the shared step-cost rule both rely on. Extracted from TurnManager since none of this
    /// depends on turn/UI state — only the grid itself and the moving unit.
    /// </summary>
    public class GridPathfinder
    {
        private readonly MapTile[,] _tiles;
        private readonly int _gridSize;

        public GridPathfinder(MapTile[,] tiles, int gridSize)
        {
            _tiles = tiles;
            _gridSize = gridSize;
        }

        /// <summary>
        /// True if <paramref name="other"/> is an enemy of <paramref name="mover"/> for
        /// Zone-of-Control purposes — factions are simply Npc vs. non-Npc Character.
        /// An Invisible <paramref name="other"/> is not considered an enemy unless
        /// <paramref name="mover"/> is an Npc that ignores invisibility, matching the
        /// perception rule <see cref="AggressiveNpcAI"/> already uses for targeting/pathing
        /// (see AggressiveNpcAI.CanTarget) — otherwise ZoC would reveal an invisible unit's
        /// position via a doubled movement cost the mover shouldn't be able to notice.
        /// </summary>
        public static bool IsEnemyOf(Character mover, Character other)
        {
            if ((mover is Npc) == (other is Npc)) return false;
            if (other.IsInvisible && !(mover is Npc n && n.IgnoresInvisibility)) return false;
            return true;
        }

        /// <summary>
        /// Movement cost of a single cardinal step from <paramref name="from"/> to
        /// <paramref name="to"/> for <paramref name="mover"/>. Base cost is <paramref
        /// name="to"/>'s terrain (see TerrainInfoRegistry — 2 for difficult terrain like
        /// Forest/Swamp/Mountain/Snow, 1 otherwise), +1 more if <paramref name="from"/> lies
        /// within an enemy's Zone of Control (the 8 tiles surrounding that enemy) and
        /// <paramref name="to"/> lies outside that same enemy's zone — i.e. the step leaves
        /// the zone. Checking all enemies adjacent to <paramref name="from"/> with an
        /// early-exit means overlapping enemy zones still only add the +1 once, never stack.
        /// </summary>
        public int StepCost(MapTile from, MapTile to, Character mover)
        {
            int cost = TerrainInfoRegistry.Get(to.TerrainType).MovementCost;
            if (LeavesAnEnemyZone(from, to, mover))
                cost += 1;
            return cost;
        }

        private bool LeavesAnEnemyZone(MapTile from, MapTile to, Character mover)
        {
            foreach (MapTile t in SpellAreaUtils.GetCircleAroundTiles(from, 1, _tiles, _gridSize))
            {
                Character occupant = t.OccupyingUnit;
                if (occupant == null || !IsEnemyOf(mover, occupant)) continue;

                int dx = Mathf.Abs(to.GridX - t.GridX);
                int dz = Mathf.Abs(to.GridZ - t.GridZ);
                if (Mathf.Max(dx, dz) > 1) return true;
            }
            return false;
        }

        /// <summary>Total Zone-of-Control-aware movement cost of an already-found
        /// <paramref name="path"/> (as returned by <see cref="FindShortestPath"/>).</summary>
        public int PathCost(List<MapTile> path, Character mover)
        {
            int cost = 0;
            for (int i = 1; i < path.Count; i++)
                cost += StepCost(path[i - 1], path[i], mover);
            return cost;
        }

        /// <summary>Returns the longest affordable prefix of <paramref name="path"/> (which
        /// must start at the mover's current tile) given a movement point <paramref name="budget"/>,
        /// accounting for Zone-of-Control step costs. May return just the origin tile if even
        /// the first step is unaffordable.</summary>
        public List<MapTile> TrimPathToMovement(List<MapTile> path, int budget, Character mover)
        {
            var trimmed = new List<MapTile> { path[0] };
            int spent = 0;
            for (int i = 1; i < path.Count; i++)
            {
                int stepCost = StepCost(path[i - 1], path[i], mover);
                if (spent + stepCost > budget) break;
                spent += stepCost;
                trimmed.Add(path[i]);
            }
            return trimmed;
        }

        /// <summary>
        /// Dijkstra: returns all tiles reachable within <paramref name="range"/> movement
        /// points of <paramref name="mover"/>, excluding occupied tiles (characters may not
        /// pass through or land on them). Step costs vary (see <see cref="StepCost"/>), so a
        /// genuine relax-then-settle loop is used rather than plain BFS.
        /// </summary>
        public HashSet<MapTile> BfsReachable(MapTile origin, int range, Character mover)
        {
            var dist = new Dictionary<MapTile, int> { [origin] = 0 };
            var settled = new HashSet<MapTile>();
            var frontier = new List<MapTile> { origin };

            while (frontier.Count > 0)
            {
                MapTile cur = PopClosest(frontier, dist);
                settled.Add(cur);
                if (dist[cur] > range) break;

                RelaxNeighborsForReachability(cur, dist[cur], range, mover, settled, dist, frontier);
            }

            return new HashSet<MapTile>(settled);
        }

        private void RelaxNeighborsForReachability(MapTile cur, int curDist, int range, Character mover,
            HashSet<MapTile> settled, Dictionary<MapTile, int> dist, List<MapTile> frontier)
        {
            foreach (MapTile nb in Neighbors(cur))
            {
                if (!nb.IsPassable(mover)) continue;
                if (nb.IsOccupied || settled.Contains(nb)) continue;

                int nd = curDist + StepCost(cur, nb, mover);
                if (nd > range) continue;

                if (!dist.TryGetValue(nb, out int old) || nd < old)
                {
                    dist[nb] = nd;
                    if (!frontier.Contains(nb)) frontier.Add(nb);
                }
            }
        }

        /// <summary>
        /// Dijkstra shortest (cheapest) path from <paramref name="origin"/> to
        /// <paramref name="dest"/> for <paramref name="mover"/>, accounting for
        /// Zone-of-Control step costs (see <see cref="StepCost"/>).
        /// Returns an ordered list that starts with origin and ends with dest.
        /// </summary>
        public List<MapTile> FindShortestPath(MapTile origin, MapTile dest, Character mover)
        {
            var dist = new Dictionary<MapTile, int> { [origin] = 0 };
            var prev = new Dictionary<MapTile, MapTile>();
            var settled = new HashSet<MapTile>();
            var frontier = new List<MapTile> { origin };

            while (frontier.Count > 0)
            {
                MapTile cur = PopClosest(frontier, dist);
                settled.Add(cur);
                if (cur == dest) break;

                RelaxNeighborsForPath(cur, dest, mover, settled, dist, prev, frontier);
            }

            return ReconstructPath(dest, prev);
        }

        private void RelaxNeighborsForPath(MapTile cur, MapTile dest, Character mover,
            HashSet<MapTile> settled, Dictionary<MapTile, int> dist, Dictionary<MapTile, MapTile> prev,
            List<MapTile> frontier)
        {
            foreach (MapTile nb in Neighbors(cur))
            {
                if (!nb.IsPassable(mover)) continue;
                if (nb.IsOccupied && nb != dest) continue;
                if (settled.Contains(nb)) continue;

                int nd = dist[cur] + StepCost(cur, nb, mover);
                if (!dist.TryGetValue(nb, out int old) || nd < old)
                {
                    dist[nb] = nd;
                    prev[nb] = cur;
                    if (!frontier.Contains(nb)) frontier.Add(nb);
                }
            }
        }

        /// <summary>Removes and returns the frontier tile with the lowest tentative distance.</summary>
        private static MapTile PopClosest(List<MapTile> frontier, Dictionary<MapTile, int> dist)
        {
            MapTile closest = null;
            int best = int.MaxValue;
            foreach (MapTile t in frontier)
                if (dist[t] < best) { best = dist[t]; closest = t; }

            frontier.Remove(closest);
            return closest;
        }

        /// <summary>Walks the prev-pointer chain back from dest to origin, then reverses it.</summary>
        private static List<MapTile> ReconstructPath(MapTile dest, Dictionary<MapTile, MapTile> prev)
        {
            var path = new List<MapTile>();
            MapTile node = dest;
            while (node != null)
            {
                path.Insert(0, node);
                if (!prev.TryGetValue(node, out MapTile parent)) break;
                node = parent;
            }
            return path;
        }

        private List<MapTile> Neighbors(MapTile tile)
        {
            var list = new List<MapTile>(4);
            int x = tile.GridX;
            int z = tile.GridZ;
            if (x > 0) list.Add(_tiles[x - 1, z]);
            if (x < _gridSize - 1) list.Add(_tiles[x + 1, z]);
            if (z > 0) list.Add(_tiles[x, z - 1]);
            if (z < _gridSize - 1) list.Add(_tiles[x, z + 1]);
            return list;
        }
    }
}
