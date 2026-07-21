using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Meditate — gain mana based on half of your maximum mana.
    /// bonus = floor(Max Mana × 0.5)
    /// </summary>
    public class Meditate : ClassSpell
    {
        public Meditate() : base(
            characterClass: ClassRegistry.Get("Mage"),
            name:        "Meditate",
            description: "Gain {0} mana. Does not cost an action point. Can only be used once per fight.",
            manaCost:        0,
            actionPointCost: 0,
            range:       0,
            area:        0,
            cooldown:    0,
            targetType:  SpellTargetType.Self,
            oncePerFight: true)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Mana")
                    .Add($"{caster.CharacterStats.MaxMana} × 0.5, floored)", Mathf.FloorToInt(caster.CharacterStats.MaxMana * 0.5f))
            };

        /// <summary>Self-only — <paramref name="target"/> is always the caster.</summary>
        public override ActionPreview GetPreview(Character caster, Character target)
        {
            int bonus = ComputeValues(caster)[0].Total;
            return new ActionPreview { ManaDelta = bonus };
        }

        public override bool Execute(SpellContext context)
        {
            int bonus = ComputeValues(context.Caster)[0].Total;
            context.Caster.CharacterStats.Mana += bonus;
            FloatingCombatTextSystem.Instance?.ShowMana(context.Caster, bonus);

            Debug.Log($"[Meditate] '{context.Caster.CharacterName}' gained {bonus} mana.  " +
                      $"Mana: {context.Caster.CharacterStats.Mana}" +
                      $"/{context.Caster.CharacterStats.MaxMana}");
            return true;
        }
    }
}