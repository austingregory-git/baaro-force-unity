using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Backstab — strike an enemy from behind for {0} damage.
    /// Deals 2x TotalDamage if caster is behind the target (i.e. the caster's tile is directly opposite the target's facing direction).
    /// Can only be used from behind the target
    /// </summary>
    public class Backstab : ClassSpell
    {
        public Backstab() : base(
            characterClass: ClassRegistry.Get("Rogue"),
            name:        "Backstab",
            description: "Strike an enemy from behind for {0} damage.",
            manaCost:        0,
            actionPointCost: 1,
            range:       1,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage");
            damage.AddTotalAttack(caster.CharacterStats);
            damage.Add("Behind Target (×2)", damage.Total);
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[Backstab] No enemy on the target tile.");
                return false;
            }

            if (!IsBehindTarget(context.CasterTile, context.TargetTile, target))
            {
                Debug.LogWarning("[Backstab] Can only be used from behind the target.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            int dealt  = target.TakePhysicalDamage(damage);
            FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, SpellType.Physical);

            Debug.Log($"[Backstab] '{context.Caster.CharacterName}' backstabs '{target.CharacterName}' for {damage} damage.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            if (target.CharacterStats.HealthPoints <= 0)
            {
                Debug.Log($"[Backstab] '{target.CharacterName}' has been defeated!");
                context.TargetTile.RemoveUnit();
            }

            return true;
        }

        /// <summary>True if casterTile sits directly opposite the target's facing direction —
        /// i.e. the target's back is turned to the caster.</summary>
        private static bool IsBehindTarget(MapTile casterTile, MapTile targetTile, Character target)
        {
            if (casterTile == null || targetTile == null) return false;

            Vector2Int facing = target.FacingDirection;
            return casterTile.GridX == targetTile.GridX - facing.x &&
                   casterTile.GridZ == targetTile.GridZ - facing.y;
        }
    }
}