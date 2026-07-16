using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Map;
using UnityEngine;
using System.Collections.Generic;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Cleave — strike in front of you with your weapon, dealing damage equal to your basic attack.
    /// </summary>
    public class Cleave : ClassSpell
    {
        public Cleave() : base(
            characterClass: ClassRegistry.Get("Warrior"),
            name:        "Cleave",
            description: "Strike in front of you with your weapon, dealing damage equal to your basic attack.",
            manaCost:        0,
            actionPointCost: 1,
            range:       1,
            area:        3,
            cooldown:    3,
            targetType:  SpellTargetType.Area)
        { }

        public override bool Execute(SpellContext context)
        {
            //Attack the 3 squares in front of the caster (front means the direction of the hovered tile)
            // example
            // C = Caster
            // E = Enemy
            // Example layout:
            // . . .
            // E E E
            // . C .
            // can hit all three enemies in the row in front of the caster
            // if no enemies are present, the spell still executes but deals no damage.

            List<MapTile> targetTiles = SpellAreaUtils.GetHorizontalLineTiles(
                casterTile: context.CasterTile, 
                targetTile: context.TargetTile, 
                range: range,
                area: area, 
                allTiles: context.AllTiles, 
                gridSize: context.GridSize);
            foreach (MapTile tile in targetTiles)
            {
                if (tile.IsOccupied && tile.OccupyingNpc != null)
                {
                    // Apply damage to the occupant
                    NPC target = tile.OccupyingNpc;
                    int damage = context.Caster.characterStats.TotalAttack;
                    target.characterStats.healthPoints -= damage;
                    Debug.Log($"[Cleave] '{context.Caster.characterName}' dealt {damage} damage to '{target.characterName}'. " +
                              $"HP: {target.characterStats.healthPoints}/{target.characterStats.maxHealthPoints}");
                }
            }
            return true;
        }
    }
}