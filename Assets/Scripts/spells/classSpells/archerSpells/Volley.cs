using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Volley - Deals 2 + (1.5 * BonusAttack) + (0.5 * Level) damage to enemies in an area.
    /// </summary>
    public class Volley : ClassSpell
    {
        public Volley() : base(
            characterClass: ClassRegistry.Get("Archer"),
            name:        "Volley",
            description: "Deal {0} damage to enemies in an area.",
            manaCost:        0,
            actionPointCost: 1,
            range:       3,
            area:        1,
            cooldown:    2,
            targetType:  SpellTargetType.Area,
            areaType:    SpellAreaType.Cross,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage");
            damage.Add("Base", 2);
            damage.Add("AttackBonus", Mathf.FloorToInt(caster.CharacterStats.AttackBonus * 1.5f));
            damage.Add("Level", Mathf.FloorToInt(caster.Level * 0.5f));
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            // Get the tiles in a cross shape centered on the target tile, with the specified range and area
            // The cross shape includes the target tile and extends in the four cardinal directions
            // The range determines how far the spell can reach from the caster to the target tile
            // example below with range 2 and area 1:
            // . . . . X .
            // . C . X X X
            // . . . . X .
            List<MapTile> targetTiles = SpellAreaUtils.GetCrossTiles(
                casterTile: context.CasterTile,
                targetTile: context.TargetTile,
                range: Range,
                area: Area,
                allTiles: context.AllTiles,
                gridSize: context.GridSize);

            int damage       = ComputeValues(context.Caster)[0].Total;
            bool casterIsNpc = context.Caster is Npc;
            int hits         = 0;

            foreach (MapTile tile in targetTiles)
            {
                // From an Npc's perspective enemies are player Characters; from a player
                // Character's perspective enemies are Npcs.
                Character target = casterIsNpc ? tile.OccupyingCharacter : (Character)tile.OccupyingNpc;
                if (target == null) continue;

                DealDamage(target, tile, damage, SpellType.Physical, "Volley");
                hits++;

                Debug.Log($"[Volley] '{context.Caster.CharacterName}' hits '{target.CharacterName}' " +
                          $"for {damage} physical damage.  " +
                          $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                          $"/{target.CharacterStats.MaxHealthPoints}");
            }


            Debug.Log($"[Volley] '{context.Caster.CharacterName}' dealt {damage} physical damage to {hits} target(s).");

            return true;
        }
    }
}