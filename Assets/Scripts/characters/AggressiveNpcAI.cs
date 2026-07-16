using System.Collections;
using System.Collections.Generic;
using BaaroForce.Map;
using BaaroForce.Spells;
using UnityEngine;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Aggressive NPC personality.
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
            NPC npc = context.Npc;
            Debug.Log($"[AggressiveNpcAI] '{npc.characterName}' begins turn.  " +
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

                int steps = approachPath.Count - 1;
                yield return context.AnimateNpcMove(approachPath);
                context.RemainingMovement -= steps;
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
                        int steps = advancePath.Count - 1;
                        yield return context.AnimateNpcMove(advancePath);
                        context.RemainingMovement -= steps;
                    }
                }
            }

            Debug.Log($"[AggressiveNpcAI] '{npc.characterName}' ends turn.");
        }

        // ------------------------------------------------------------------ //
        // Spell logic                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Tries each spell in order; casts the first one with a valid target.
        /// Returns true and decrements AP if a spell was cast.</summary>
        private bool TrySpell(NpcTurnContext context)
        {
            NPC npc = context.Npc;
            if (npc.characterSpells == null || npc.characterSpells.Count == 0) return false;

            foreach (Spell spell in npc.characterSpells)
            {
                if (spell.manaCost > npc.characterStats.mana) continue;

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
            if (spell.targetType == SpellTargetType.Self) return context.CurrentTile;

            int     ox       = context.CurrentTile.GridX;
            int     oz       = context.CurrentTile.GridZ;
            MapTile best     = null;
            int     bestDist = int.MaxValue;

            for (int x = 0; x < context.GridSize; x++)
            {
                for (int z = 0; z < context.GridSize; z++)
                {
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist == 0 || dist > spell.range) continue;

                    MapTile tile = context.AllTiles[x, z];
                    if (!IsValidSpellTarget(spell.targetType, tile)) continue;

                    if (dist < bestDist) { bestDist = dist; best = tile; }
                }
            }
            return best;
        }

        /// <summary>From the NPC's point of view, "Enemy" targets are player Characters
        /// and "Ally" targets are other NPCs.</summary>
        private static bool IsValidSpellTarget(SpellTargetType targetType, MapTile tile)
        {
            switch (targetType)
            {
                case SpellTargetType.Enemy: return tile.OccupyingCharacter != null;
                case SpellTargetType.Ally:  return tile.OccupyingNpc       != null;
                case SpellTargetType.Both:  return tile.IsOccupied;
                default:                   return false;
            }
        }

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
                    if (context.AllTiles[x, z].OccupyingCharacter == null) continue;
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

        /// <summary>Returns the tile occupied by the nearest living player Character,
        /// or null if there are none.</summary>
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
                    if (context.AllTiles[x, z].OccupyingCharacter == null) continue;
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist < bestDist) { bestDist = dist; best = context.AllTiles[x, z]; }
                }
            }
            return best;
        }

        /// <summary>
        /// Builds the longest walkable path toward <paramref name="target"/> that fits
        /// within <see cref="NpcTurnContext.RemainingMovement"/> steps and stops
        /// one tile short of the (occupied) target tile.
        /// Returns null when the NPC is already adjacent or cannot move at all.
        /// </summary>
        private static List<MapTile> BuildApproachPath(NpcTurnContext context, MapTile target)
        {
            List<MapTile> fullPath = context.FindPath(context.CurrentTile, target);
            // fullPath[0] = currentTile, fullPath[last] = target (occupied).
            // We never step onto the target tile itself.
            if (fullPath == null || fullPath.Count < 2) return null;

            int stepsToAdjacentTile = fullPath.Count - 2;   // steps to reach the tile before target
            if (stepsToAdjacentTile < 1) return null;       // already adjacent

            int steps = Mathf.Min(stepsToAdjacentTile, context.RemainingMovement);
            return fullPath.GetRange(0, steps + 1);         // +1 to include the origin tile
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>Basic attack range for NPCs.  Defaults to melee (1) until a class
        /// system is added to NPCs.</summary>
        private static int GetAttackRange(NPC npc) => 1;
    }
}
