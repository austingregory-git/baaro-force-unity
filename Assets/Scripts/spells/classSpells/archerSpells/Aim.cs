using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Aim - Focus your aim, multiplying the damage of your next basic attack by {0}. 2 + (0.25 * level)
    /// </summary>
    public class Aim : ClassSpell
    {
        public Aim() : base(
            characterClass: ClassRegistry.Get("Archer"),
            name:        "Aim",
            description: "Focus your aim, multiplying the damage of your next basic attack by {0}. This does not cost an action point.",
            manaCost:        2,
            actionPointCost: 0,
            range:       0,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Self,
            type:        SpellType.Buff)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage");
            damage.Add("Base", 2);
            damage.Add("Level", Mathf.FloorToInt(caster.Level * 0.25f));
            return new[] { damage };
        }

        /// <summary>Self-only — <paramref name="target"/> is always the caster.</summary>
        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview
            {
                StatusEffectName = "Aim",
                StatusEffectKind = StatusEffect.StatusEffectType.Buff,
            };

        public override bool Execute(SpellContext context)
        {
            int multiplier = ComputeValues(context.Caster)[0].Total;
            context.Caster.ApplyStatus(new AimStatus(multiplier));

            Debug.Log($"[Aim] '{context.Caster.CharacterName}' focuses their aim, " +
                      $"multiplying the damage of their next basic attack by {multiplier}.");

            return true;
        }
    }
}