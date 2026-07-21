using System.Collections.Generic;
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
    /// Bind — deal 2 + 0.75 × SpellPower magical damage and Root an enemy for 2 turns.
    /// </summary>
    public class Bind : ClassSpell
    {
        private const int RootDurationTurns = 2;

        public Bind() : base(
            characterClass: ClassRegistry.Get("Mage"),
            name:        "Bind",
            description: "Deal {0} [Magical] damage and [Root] an enemy for 2 turns.",
            manaCost:        2,
            actionPointCost: 1,
            range:       3,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Area,
            type:        SpellType.Magical)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Damage")
                    .Add("Base", 2)
                    .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 0.75, floored)", Mathf.FloorToInt(0.75f * caster.CharacterStats.SpellPowerBonus))
            };

        /// <summary>Previews the single currently-hovered unit's fate — Bind's line
        /// can hit others too, but the HUD only ever shows one "target" at a time.</summary>
        public override ActionPreview GetPreview(Character caster, Character target)
        {
            int damage = ComputeValues(caster)[0].Total;
            return new ActionPreview
            {
                RawDamage        = damage,
                StatusEffectName = "Root",
                StatusEffectKind = StatusEffect.StatusEffectType.Debuff,
            };
        }

        public override bool Execute(SpellContext context)
        {

            Npc target = context.TargetTile?.OccupyingNpc;
            if (target == null)
            {
                Debug.LogWarning("[Bind] No enemy on the target tile.");
                return false;
            }

            var root = new RootStatus(durationTurns: RootDurationTurns);
            target.ApplyStatus(root);

            int damage       = ComputeValues(context.Caster)[0].Total;
            int dealt = target.TakeDamage(damage);
            FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, SpellType.Magical);

            Debug.Log($"[Bind] '{context.Caster.CharacterName}' hits '{target.CharacterName}' " +
                        $"for {damage} magical damage and roots them for {RootDurationTurns} turn(s).  " +
                        $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                        $"/{target.CharacterStats.MaxHealthPoints}");

            if (target.CharacterStats.HealthPoints <= 0)
            {
                Debug.Log($"[Bind] '{target.CharacterName}' has been defeated!");
                context.TargetTile.RemoveUnit();
            }

            return true;
        }
    }
}
