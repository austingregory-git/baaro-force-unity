using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
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
            description: "Strike in front of you with your weapon, dealing {0} damage to each enemy hit.",
            manaCost:        0,
            actionPointCost: 1,
            range:       1,
            area:        3,
            cooldown:    3,
            targetType:  SpellTargetType.Area)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[] { new ScalingValue("Damage").AddTotalAttack(caster.CharacterStats) };

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
                range: Range,
                area: Area,
                allTiles: context.AllTiles,
                gridSize: context.GridSize);
            int damage = ComputeValues(context.Caster)[0].Total;
            foreach (MapTile tile in targetTiles)
            {
                if (tile.IsOccupied && tile.OccupyingNpc != null)
                {
                    // Apply damage to the occupant
                    Npc target = tile.OccupyingNpc;
                    target.CharacterStats.TakeDamage(damage);
                    Debug.Log($"[Cleave] '{context.Caster.CharacterName}' dealt {damage} damage to '{target.CharacterName}'. " +
                              $"HP: {target.CharacterStats.HealthPoints}/{target.CharacterStats.MaxHealthPoints}");
                    if (target.CharacterStats.HealthPoints <= 0)
                    {
                        Debug.Log($"[Cleave] '{target.CharacterName}' has been defeated!");
                        tile.RemoveUnit();
                    }
                              
                }
            }
            return true;
        }
    }
}