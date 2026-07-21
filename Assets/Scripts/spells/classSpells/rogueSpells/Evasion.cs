using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.Statuses;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Dodge the next physical attack against you.
    /// This does not use an action point.
    /// </summary>
    public class Evasion : ClassSpell
    {
        public Evasion() : base(
            characterClass: ClassRegistry.Get("Rogue"),
            name:        "Evasion",
            description: "[Dodge] the next physical attack against you. This does not use an action point.",
            manaCost:        2,
            actionPointCost: 0,
            range:       0,
            area:        0,
            cooldown:    3,
            targetType:  SpellTargetType.Self)
        { }

        /// <summary>Self-only — <paramref name="target"/> is always the caster.</summary>
        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview
            {
                StatusEffectName = "Dodge",
                StatusEffectKind = StatusEffect.StatusEffectType.Buff,
            };

        public override bool Execute(SpellContext context)
        {
            context.Caster.ApplyStatus(new DodgeStatus());
            Debug.Log($"[Evasion] '{context.Caster.CharacterName}' braces to dodge the next physical attack.");
            return true;
        }
    }
}
