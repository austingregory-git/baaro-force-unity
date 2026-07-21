using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Acorn Spray — Shopu's signature nature-magic spell.
    ///
    /// Sprays acorns in a cone in front of the caster, dealing earth damage to all enemies in the area.
    ///
    /// Damage: 0.5 × TotalAttack (floored)
    ///
    /// new Cone Area of Effect (AoE) type, with a range of 1 and an area of 3 (shown below)
    ///           x
    ///         x x
    /// Shopu x x x
    ///         x x
    ///           x
    /// </summary>
    public class AcornSpray : CharacterSpell
    {
        public AcornSpray()
            : base(
                name:        "Acorn Spray",
                description: "Spray acorns in a cone, dealing {0} [Earth] damage to all enemies in the area.",
                manaCost:        2,
                actionPointCost: 1,
                range:       1,
                area:        3,
                cooldown:    1,
                targetType:  SpellTargetType.Area,
                areaType:    SpellAreaType.Cone)
        {
        }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            int totalAttack = caster.CharacterStats.TotalAttack;
            var damage = new ScalingValue("Damage")
                .Add($"Total Attack ({totalAttack} × 0.5, floored)", Mathf.FloorToInt(totalAttack * 0.5f));
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            List<MapTile> targetTiles = SpellAreaUtils.GetAreaTiles(
                this, context.CasterTile, context.TargetTile, context.AllTiles, context.GridSize);

            int damage       = ComputeValues(context.Caster)[0].Total;
            bool casterIsNpc = context.Caster is Npc;
            int hits         = 0;

            foreach (MapTile tile in targetTiles)
            {
                // From an Npc's perspective enemies are player Characters; from a player
                // Character's perspective enemies are Npcs.
                Character target = casterIsNpc ? tile.OccupyingCharacter : (Character)tile.OccupyingNpc;
                if (target == null) continue;

                int dealt = target.CharacterStats.TakeDamage(damage);
                FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, SpellType.Earth);
                hits++;

                Debug.Log($"[AcornSpray] '{context.Caster.CharacterName}' hits '{target.CharacterName}' " +
                          $"for {damage} earth damage.  " +
                          $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                          $"/{target.CharacterStats.MaxHealthPoints}");

                if (target.CharacterStats.HealthPoints <= 0)
                {
                    Debug.Log($"[AcornSpray] '{target.CharacterName}' has been defeated!");
                    tile.RemoveUnit();
                }
            }

            Debug.Log($"[AcornSpray] '{context.Caster.CharacterName}' sprays acorns, hitting {hits} target(s).");
            return true;
        }
    }
}
