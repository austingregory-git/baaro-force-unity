using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Arcane Explosion — deal 2 + 0.5*SpellPowerBonus damage to all enemies in a radius around the caster.
    /// </summary>
    public class ArcaneExplosion : ClassSpell
    {
        public ArcaneExplosion() : base(
            characterClass: ClassRegistry.Get("Mage"),
            name:        "Arcane Explosion",
            description: "Deal {0} [Magical] damage to all enemies in a radius around the caster.",
            manaCost:        2,
            actionPointCost: 1,
            range:       0,
            area:        2,
            cooldown:    0,
            targetType:  SpellTargetType.Self,
            areaType:    SpellAreaType.Circle,
            oncePerFight: false,
            type:        SpellType.Magical)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Damage")
                    .Add("Base", 2)
                    .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 0.5, floored)", Mathf.FloorToInt(0.5f * caster.CharacterStats.SpellPowerBonus))
            };

        // No GetPreview override — a Self-type spell's preview is rendered on the
        // caster's own panel (see CharacterHudController.ComputeCasterPreview), and
        // Arcane Explosion never damages its own caster, only nearby enemies. The base
        // class's default (ActionPreview.None) is correct here.

        public override bool Execute(SpellContext context)
        {
            // Attack all squares in a radius around the caster.
            // Example layout (radius = 2):
            // . . E . .
            // . E E E .
            // E E C E E
            // . E E E .
            // . . E . .
            // hits every enemy in the radius; if none are present the spell still executes but deals no damage.

            List<MapTile> targetTiles = SpellAreaUtils.GetTrueCircleAreaTiles(
                casterTile: context.CasterTile,
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

                int dealt = target.TakeDamage(damage);
                FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, SpellType.Magical);
                hits++;

                Debug.Log($"[Arcane Explosion] '{context.Caster.CharacterName}' hits '{target.CharacterName}' " +
                          $"for {damage} magical damage.  " +
                          $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                          $"/{target.CharacterStats.MaxHealthPoints}");

                if (target.CharacterStats.HealthPoints <= 0)
                {
                    Debug.Log($"[Arcane Explosion] '{target.CharacterName}' has been defeated!");
                    tile.RemoveUnit();
                }
            }

            Debug.Log($"[Arcane Explosion] '{context.Caster.CharacterName}' explodes arcane energy, hitting {hits} target(s).");
            return true;
        }
    }
}