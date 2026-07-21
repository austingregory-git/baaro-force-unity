using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Arcane Beam — deal 2 + SpellPowerBonus damage to all enemies in a line in front of the caster.  The line is 4 tiles long and 1 tile wide.
    /// </summary>
    public class ArcaneBeam : ClassSpell
    {
        public ArcaneBeam() : base(
            characterClass: ClassRegistry.Get("Mage"),
            name:        "Arcane Beam",
            description: "Deal {0} [Magical] damage to enemies in a line.",
            manaCost:        2,
            actionPointCost: 1,
            range:       1,
            area:        4,
            cooldown:    0,
            targetType:  SpellTargetType.Area,
            areaType:    SpellAreaType.VerticalLine,
            oncePerFight: false,
            type:        SpellType.Magical)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Damage")
                    .Add("Base", 2)
                    .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus})", caster.CharacterStats.SpellPowerBonus)
            };

        /// <summary>Previews the single currently-hovered unit's fate — Cleave's line
        /// can hit two others too, but the HUD only ever shows one "target" at a time.</summary>
        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            // Attack the 4 squares in front of the caster (front means the direction of the hovered tile).
            // Example layout:
            // . . . . .
            // . C E E E
            // . . . . .
            // hits every enemy in the line; if none are present the spell still executes but deals no damage.

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

                DealDamage(target, tile, damage, SpellType.Magical, "Arcane Beam");
                hits++;

                Debug.Log($"[Arcane Beam] '{context.Caster.CharacterName}' hits '{target.CharacterName}' " +
                          $"for {damage} magical damage.  " +
                          $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                          $"/{target.CharacterStats.MaxHealthPoints}");
            }

            Debug.Log($"[Arcane Beam] '{context.Caster.CharacterName}' fires a beam, hitting {hits} target(s).");
            return true;
        }
    }
}