using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Rally — the caster and nearby allied units gain increased attack for 3 turns.
    /// Level scaling: bonus = floor(1 + 0.25 × level)   (1 attack at level 1, 2 attack at level 4)
    ///
    /// No tile is aimed — the caster rallies immediately and the effect always centres on
    /// their own tile, buffing every allied unit in the surrounding CircleAround area
    /// (adjacent and diagonal tiles) plus the caster's own tile.
    /// </summary>
    public class Rally : ClassSpell
    {
        /// <summary>How many turns the attack boost lasts on each rallied ally.</summary>
        private const int RallyDurationTurns = 3;

        public Rally() : base(
            characterClass: ClassRegistry.Get("Warrior"),
            name:        "Rally",
            description: "Increase attack of yourself and nearby allies by {0} for 3 turns.",
            manaCost:        2,
            actionPointCost: 1,
            range:       0,
            area:        1,
            cooldown:    3,
            targetType:  SpellTargetType.Self,
            areaType:    SpellAreaType.CircleAround,
            includeOriginTile: true,
            type:        SpellType.Buff)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Attack")
                    .Add("Base", 1)
                    .Add($"Level ({caster.Level} × 0.25, floored)", Mathf.FloorToInt(caster.Level * 0.25f))
            };

        /// <summary>Previewed on the caster's own panel only (see class doc) —
        /// <paramref name="target"/> is always the caster in that case.</summary>
        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { AttackBonusDelta = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            int bonus = ComputeValues(context.Caster)[0].Total;
            bool casterIsNpc = context.Caster is Npc;

            List<MapTile> nearbyTiles = SpellAreaUtils.GetCircleAroundTiles(
                context.CasterTile, Area, context.AllTiles, context.GridSize, IncludeOriginTile);

            int alliesRallied = 0;
            foreach (MapTile tile in nearbyTiles)
            {
                // From an Npc's perspective allies are other Npcs; from a player Character's
                // perspective allies are other player Characters (never the enemy occupant type).
                Character ally = casterIsNpc ? tile.OccupyingNpc : tile.OccupyingCharacter;
                if (ally == null) continue;

                ally.ApplyStatus(new RallyStatus(durationTurns: RallyDurationTurns, attackBoost: bonus));
                alliesRallied++;
            }

            Debug.Log($"[Rally] '{context.Caster.CharacterName}' rallied {alliesRallied} nearby " +
                      $"{(alliesRallied == 1 ? "ally" : "allies")}, increasing attack by {bonus} " +
                      $"for {RallyDurationTurns} turns.");

            return true;
        }
    }
}
