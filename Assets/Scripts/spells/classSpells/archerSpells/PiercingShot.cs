using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Piercing Shot - Deals 2 + TotalAttack damage to targets in a 2 tile line.
    /// </summary>
    public class PiercingShot : ClassSpell
    {
        public PiercingShot() : base(
            characterClass: ClassRegistry.Get("Archer"),
            name:        "Piercing Shot",
            description: "Deal {0} damage to targets in a line.",
            manaCost:        0,
            actionPointCost: 1,
            range:       3,
            area:        2,
            cooldown:    2,
            targetType:  SpellTargetType.Area,
            areaType:    SpellAreaType.VerticalLine,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage");
            damage.Add("Base", 2);
            damage.AddTotalAttack(caster.CharacterStats);
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            List<MapTile> targetTiles = SpellAreaUtils.GetVerticalLineTiles(
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

                DealDamage(target, tile, damage, SpellType.Physical, "Piercing Shot");
                hits++;

                Debug.Log($"[Piercing Shot] '{context.Caster.CharacterName}' hits '{target.CharacterName}' " +
                          $"for {damage} physical damage.  " +
                          $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                          $"/{target.CharacterStats.MaxHealthPoints}");
            }


            Debug.Log($"[Piercing Shot] '{context.Caster.CharacterName}' dealt {damage} physical damage to {hits} target(s).");

            return true;
        }
    }
}