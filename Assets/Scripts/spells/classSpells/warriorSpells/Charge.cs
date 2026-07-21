using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Charge — close the distance to an enemy up to 4 tiles away, land on the
    /// adjacent tile closest to the caster, and strike for TotalAttack damage.
    /// </summary>
    public class Charge : ClassSpell
    {
        public Charge() : base(
            characterClass: ClassRegistry.Get("Warrior"),
            name:        "Charge",
            description: "Charge up to 3 squares at an enemy, dealing {0} damage.",
            manaCost:        0,
            actionPointCost: 1,
            range:       3,
            area:        0,
            cooldown:    3,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[] { new ScalingValue("Damage").AddTotalAttack(caster.CharacterStats) };

        /// <summary>
        /// Finds the unoccupied tile adjacent to the target that is closest to
        /// the caster.  Returns null if the caster is already standing on that
        /// tile (i.e. already adjacent — no movement required).
        /// </summary>
        public override MapTile GetCasterLandingTile(SpellContext context)
        {
            if (context.CasterTile == null || context.TargetTile == null) return null;

            int tx = context.TargetTile.GridX, tz = context.TargetTile.GridZ;
            int cx = context.CasterTile.GridX, cz = context.CasterTile.GridZ;

            int[]   dx       = { -1,  1,  0,  0 };
            int[]   dz       = {  0,  0, -1,  1 };
            MapTile best     = null;
            int     bestDist = int.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                int nx = tx + dx[i], nz = tz + dz[i];
                if (nx < 0 || nx >= context.GridSize || nz < 0 || nz >= context.GridSize)
                    continue;

                MapTile candidate = context.AllTiles[nx, nz];

                // Only land on unoccupied tiles (caster's own tile counts as free).
                if (candidate.IsOccupied && candidate != context.CasterTile)
                    continue;

                int dist = Mathf.Abs(nx - cx) + Mathf.Abs(nz - cz);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = candidate;
                }
            }

            // Already on the best tile — no repositioning needed.
            if (best == context.CasterTile) return null;

            return best;
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[Charge] No enemy on the target tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Physical, "Charge");

            Debug.Log($"[Charge] '{context.Caster.CharacterName}' charges '{target.CharacterName}' " +
                      $"for {damage} damage.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}