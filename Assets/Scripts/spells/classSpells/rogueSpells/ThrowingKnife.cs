using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Throw a knife at an enemy for 2 + 0.5 × Level.
    /// This does NOT use an action point.
    /// </summary>
    public class ThrowingKnife : ClassSpell
    {
        public ThrowingKnife() : base(
            characterClass: ClassRegistry.Get("Rogue"),
            name:        "Throwing Knife",
            description: "Throw a knife at an enemy for {0} damage. This does not use an action point.",
            manaCost:        1,
            actionPointCost: 0,
            range:       3,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Damage")
                    .Add("Base", 2)
                    .Add($"Level ({caster.Level} × 0.5, floored)", Mathf.FloorToInt(caster.Level * 0.5f))
            };

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[ThrowingKnife] No enemy on the target tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Physical, "ThrowingKnife", physical: true);

            Debug.Log($"[ThrowingKnife] '{context.Caster.CharacterName}' throws a knife at '{target.CharacterName}' " +
                      $"for {damage} damage.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}