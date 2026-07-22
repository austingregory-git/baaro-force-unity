using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Map;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Song of the Elders — Buggles' signature spell.
    ///
    /// Sings in a cone in front of the caster (same shape as Acorn Spray: range 1, area 3),
    /// applying [Haste] to every ally caught in the area and [Slow] to every enemy caught
    /// in the area, both for 2 turns.
    /// </summary>
    public class SongOfTheElders : CharacterSpell
    {
        private const int HasteDurationTurns = 2;
        private const int SlowDurationTurns  = 2;

        public SongOfTheElders()
            : base(
                name:        "Song of the Elders",
                description: "Sing an inspiring tune, applying [Haste] to allies and [Slow] to enemies in a cone for 2 turns.",
                manaCost:        4,
                actionPointCost: 1,
                range:       1,
                area:        3,
                cooldown:    3,
                targetType:  SpellTargetType.Area,
                areaType:    SpellAreaType.Cone,
                type:        SpellType.Buff)
        {
        }

        public override bool Execute(SpellContext context)
        {
            List<MapTile> targetTiles = SpellAreaUtils.GetAreaTiles(
                this, context.CasterTile, context.TargetTile, context.AllTiles, context.GridSize);

            bool casterIsNpc = context.Caster is Npc;
            int hastened = 0;
            int slowed   = 0;

            foreach (MapTile tile in targetTiles)
            {
                // From an Npc's perspective allies are other Npcs and enemies are player
                // Characters; from a player Character's perspective it's the reverse.
                Character ally  = casterIsNpc ? (Character)tile.OccupyingNpc : tile.OccupyingCharacter;
                Character enemy = casterIsNpc ? tile.OccupyingCharacter : (Character)tile.OccupyingNpc;

                if (ally != null)
                {
                    ally.ApplyStatus(new HasteStatus(HasteDurationTurns));
                    hastened++;
                }
                else if (enemy != null)
                {
                    enemy.ApplyStatus(new SlowStatus(SlowDurationTurns));
                    slowed++;
                }
            }

            Debug.Log($"[Song of the Elders] '{context.Caster.CharacterName}' sings, " +
                      $"hastening {hastened} ally/allies and slowing {slowed} enemy/enemies.");
            return true;
        }
    }
}
