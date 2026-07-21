using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Double shot - Shoots the target twice, dealing damage each time.  Damage is calculated as:
    /// TotalAttack for each shot
    /// </summary>
    public class DoubleShot : ClassSpell
    {
        public DoubleShot() : base(
            characterClass: ClassRegistry.Get("Archer"),
            name:        "Double Shot",
            description: "Shoot the target twice, dealing {0} damage each time.",
            manaCost:        0,
            actionPointCost: 1,
            range:       3,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Physical)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage");
            damage.AddTotalAttack(caster.CharacterStats);
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[Double Shot] No enemy on the target tile.");
                return false;
            }

            int damage = ComputeValues(context.Caster)[0].Total;
            DealDamage(target, context.TargetTile, damage, SpellType.Physical, "Double Shot");
            DealDamage(target, context.TargetTile, damage, SpellType.Physical, "Double Shot");

            Debug.Log($"[Double Shot] '{context.Caster.CharacterName}' dealt {damage} damage to '{target.CharacterName}'. " +
                        $"HP: {target.CharacterStats.HealthPoints}/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}