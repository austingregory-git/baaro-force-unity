using System.Collections;
using System.Collections.Generic;
using BaaroForce.Map;
using BaaroForce.Spells;
using UnityEngine;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Aggressive Npc personality.
    ///
    /// Decision priority each action point:
    ///   1. Cast the first usable spell that has a valid target in range.
    ///   2. If no spell is available, attack the nearest enemy in range.
    ///   3. If no enemy is in range, advance toward the nearest enemy and retry.
    ///
    /// After all action points are spent any remaining movement is used to
    /// advance toward the nearest enemy (aggressive pursuit).
    /// </summary>
    public class AggressiveNpcAI : NpcAI
    {
        public override NpcPersonality Personality => NpcPersonality.Aggressive;

        // ------------------------------------------------------------------ //
        // Turn entry-point                                                    //
        // ------------------------------------------------------------------ //

        public override IEnumerator ExecuteTurn(NpcTurnContext context)
        {
            Npc npc = context.Npc;
            Debug.Log($"[AggressiveNpcAI] '{npc.CharacterName}' begins turn.  " +
                      $"MP:{context.RemainingMovement}  AP:{context.RemainingActions}");

            while (context.RemainingActions > 0)
            {
                // 1. Attempt to cast a spell.
                if (TrySpell(context)) continue;

                // 2. Attack a target already within range.
                MapTile attackTarget = FindBestAttackTarget(context);
                if (attackTarget != null)
                {
                    context.ExecuteAttack(attackTarget);
                    context.RemainingActions--;
                    continue;
                }

                // 3. No valid target — advance toward the nearest enemy and retry.
                if (context.RemainingMovement <= 0) break;

                MapTile nearest = FindNearestEnemyTile(context);
                if (nearest == null) break;

                List<MapTile> approachPath = BuildApproachPath(context, nearest);
                if (approachPath == null || approachPath.Count <= 1) break;

                int cost = context.PathCost(approachPath);
                yield return context.AnimateNpcMove(approachPath);
                context.RemainingMovement -= cost;
                // Loop back — check spells/attacks from the new position.
            }

            // Aggressive: spend any leftover movement to close the gap further.
            if (context.RemainingMovement > 0)
            {
                MapTile nearest = FindNearestEnemyTile(context);
                if (nearest != null)
                {
                    List<MapTile> advancePath = BuildApproachPath(context, nearest);
                    if (advancePath != null && advancePath.Count > 1)
                    {
                        int cost = context.PathCost(advancePath);
                        yield return context.AnimateNpcMove(advancePath);
                        context.RemainingMovement -= cost;
                    }
                }
            }

            Debug.Log($"[AggressiveNpcAI] '{npc.CharacterName}' ends turn.");
        }

        // ------------------------------------------------------------------ //
        // Spell logic                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Tries each spell in order; casts the first one with a valid target.
        /// Returns true and decrements AP if a spell was cast.</summary>
        private bool TrySpell(NpcTurnContext context)
        {
            Npc npc = context.Npc;
            if (npc.CharacterSpells == null || npc.CharacterSpells.Count == 0) return false;

            foreach (Spell spell in npc.CharacterSpells)
            {
                if (spell.ManaCost > npc.CharacterStats.Mana) continue;

                MapTile target = FindSpellTarget(context, spell);
                if (target == null) continue;

                bool success = context.ExecuteSpell(spell, target);
                if (success)
                {
                    context.RemainingActions--;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Returns the nearest tile that is a valid target for <paramref name="spell"/>,
        /// or null if none exist within range.</summary>
        private static MapTile FindSpellTarget(NpcTurnContext context, Spell spell)
        {
            // Self-targeting spells need no search.
            if (spell.TargetType == SpellTargetType.Self) return context.CurrentTile;

            int     ox       = context.CurrentTile.GridX;
            int     oz       = context.CurrentTile.GridZ;
            MapTile best     = null;
            int     bestDist = int.MaxValue;

            for (int x = 0; x < context.GridSize; x++)
            {
                for (int z = 0; z < context.GridSize; z++)
                {
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist == 0 || dist > spell.Range) continue;

                    MapTile tile = context.AllTiles[x, z];
                    if (!IsValidSpellTarget(context, spell.TargetType, tile)) continue;

                    if (dist < bestDist) { bestDist = dist; best = tile; }
                }
            }
            return best;
        }

        /// <summary>From the Npc's point of view, "Enemy" targets are player Characters
        /// and "Ally" targets are other Npcs. An Invisible player Character is not a valid
        /// Enemy target unless this Npc ignores invisibility (see Npc.IgnoresInvisibility).</summary>
        private static bool IsValidSpellTarget(NpcTurnContext context, SpellTargetType targetType, MapTile tile)
        {
            switch (targetType)
            {
                case SpellTargetType.Enemy: return CanTarget(context, tile.OccupyingCharacter);
                case SpellTargetType.Ally:  return tile.OccupyingNpc != null;
                case SpellTargetType.Both:  return tile.IsOccupied;
                default:                   return false;
            }
        }

        /// <summary>True if this Npc is allowed to perceive/target <paramref name="character"/> —
        /// false for a null occupant, or for an Invisible one unless this Npc ignores
        /// invisibility. Shared by spell targeting, basic-attack targeting, and movement
        /// pathing so an invisible player Character is treated as unseen everywhere.</summary>
        private static bool CanTarget(NpcTurnContext context, Character character) =>
            character != null && (context.Npc.IgnoresInvisibility || !character.IsInvisible);

        // ------------------------------------------------------------------ //
        // Attack logic                                                        //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the nearest player Character tile within basic-attack range,
        /// or null if no valid target exists.</summary>
        private static MapTile FindBestAttackTarget(NpcTurnContext context)
        {
            int     range    = GetAttackRange(context.Npc);
            int     ox       = context.CurrentTile.GridX;
            int     oz       = context.CurrentTile.GridZ;
            MapTile best     = null;
            int     bestDist = int.MaxValue;

            for (int x = 0; x < context.GridSize; x++)
            {
                for (int z = 0; z < context.GridSize; z++)
                {
                    if (!CanTarget(context, context.AllTiles[x, z].OccupyingCharacter)) continue;
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist > range) continue;
                    if (dist < bestDist) { bestDist = dist; best = context.AllTiles[x, z]; }
                }
            }
            return best;
        }

        // ------------------------------------------------------------------ //
        // Movement logic                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the tile occupied by the nearest living, targetable player
        /// Character, or null if there are none (an Invisible one counts as unseen —
        /// see <see cref="CanTarget"/> — so the Npc doesn't reveal awareness of it by
        /// pathing straight toward it).</summary>
        private static MapTile FindNearestEnemyTile(NpcTurnContext context)
        {
            int     ox       = context.CurrentTile.GridX;
            int     oz       = context.CurrentTile.GridZ;
            MapTile best     = null;
            int     bestDist = int.MaxValue;

            for (int x = 0; x < context.GridSize; x++)
            {
                for (int z = 0; z < context.GridSize; z++)
                {
                    if (!CanTarget(context, context.AllTiles[x, z].OccupyingCharacter)) continue;
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist < bestDist) { bestDist = dist; best = context.AllTiles[x, z]; }
                }
            }
            return best;
        }

        /// <summary>
        /// Builds the longest walkable path toward <paramref name="target"/> that fits
        /// within <see cref="NpcTurnContext.RemainingMovement"/> movement points (accounting
        /// for Zone-of-Control step costs) and stops one tile short of the (occupied) target tile.
        /// Returns null when the Npc is already adjacent or cannot move at all.
        /// </summary>
        private static List<MapTile> BuildApproachPath(NpcTurnContext context, MapTile target)
        {
            List<MapTile> fullPath = context.FindPath(context.CurrentTile, target);
            // fullPath[0] = currentTile, fullPath[last] = target (occupied).
            // We never step onto the target tile itself.
            if (fullPath == null || fullPath.Count < 2) return null;

            int stepsToAdjacentTile = fullPath.Count - 2;   // steps to reach the tile before target
            if (stepsToAdjacentTile < 1) return null;       // already adjacent

            List<MapTile> pathToAdjacentTile = fullPath.GetRange(0, stepsToAdjacentTile + 1);
            return context.TrimPathToMovement(pathToAdjacentTile, context.RemainingMovement);
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>Basic attack range for Npcs.  Defaults to melee (1) until a class
        /// system is added to Npcs.</summary>
        private static int GetAttackRange(Npc npc) => 1;
    }
}
