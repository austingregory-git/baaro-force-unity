using System;
using System.Collections;
using System.Collections.Generic;
using BaaroForce.Map;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    /// <summary>
    /// All information an <see cref="NpcAI"/> strategy needs to resolve one turn.
    /// TurnManager builds this before calling <see cref="NpcAI.ExecuteTurn"/> and
    /// populates the delegate fields so the AI can trigger animations and actions
    /// without taking a direct dependency on TurnManager.
    /// </summary>
    public sealed class NpcTurnContext
    {
        // ------------------------------------------------------------------ //
        // State                                                               //
        // ------------------------------------------------------------------ //

        public readonly Npc       Npc;
        /// <summary>Updated by <see cref="AnimateNpcMove"/> after each movement step.</summary>
        public          MapTile   CurrentTile;
        public readonly MapTile[,] AllTiles;
        public readonly int       GridSize;
        public          int       RemainingMovement;
        public          int       RemainingActions;

        // ------------------------------------------------------------------ //
        // Grid-query delegates (set by TurnManager)                          //
        // ------------------------------------------------------------------ //

        /// <summary>Returns all tiles reachable within <c>range</c> cardinal steps from <c>origin</c>,
        /// excluding occupied tiles.</summary>
        public Func<MapTile, int, HashSet<MapTile>> BfsReachable;

        /// <summary>Returns the shortest walkable path from <c>origin</c> to <c>dest</c>
        /// as an ordered list starting at origin.</summary>
        public Func<MapTile, MapTile, List<MapTile>> FindPath;

        /// <summary>Total Zone-of-Control-aware movement point cost of an already-found path.</summary>
        public Func<List<MapTile>, int> PathCost;

        /// <summary>Returns the longest prefix of a path (starting at its first tile) affordable
        /// within a given movement point budget, accounting for Zone-of-Control step costs.</summary>
        public Func<List<MapTile>, int, List<MapTile>> TrimPathToMovement;

        // ------------------------------------------------------------------ //
        // Execution delegates (set by TurnManager)                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Animates the Npc walking along <c>path</c> (which must start at CurrentTile).
        /// Also updates <see cref="CurrentTile"/> to the final tile in the path.
        /// Yield this in the AI coroutine to wait for the animation to finish.
        /// </summary>
        public Func<List<MapTile>, IEnumerator> AnimateNpcMove;

        /// <summary>Executes a basic melee/ranged attack on the character occupying
        /// <c>targetTile</c>.  Handles damage and death removal.</summary>
        public Action<MapTile> ExecuteAttack;

        /// <summary>Casts <c>spell</c> at <c>targetTile</c>.
        /// Deducts mana on success.  Returns true if the spell resolved.</summary>
        public Func<Spell, MapTile, bool> ExecuteSpell;

        // ------------------------------------------------------------------ //
        // Constructor                                                         //
        // ------------------------------------------------------------------ //

        public NpcTurnContext(Npc npc, MapTile currentTile, MapTile[,] allTiles, int gridSize,
                              int remainingMovement, int remainingActions)
        {
            Npc               = npc;
            CurrentTile       = currentTile;
            AllTiles          = allTiles;
            GridSize          = gridSize;
            RemainingMovement = remainingMovement;
            RemainingActions  = remainingActions;
        }
    }
}
