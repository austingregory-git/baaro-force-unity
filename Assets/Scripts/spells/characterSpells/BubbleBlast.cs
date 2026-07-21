using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Bubble Blast — Guppy's signature water-magic spell.
    ///
    /// Deal Water damage to a single enemy within range 4.  The damage scales with the caster's maximum [Mana].
    /// </summary>
    public class BubbleBlast : CharacterSpell
    {
        public BubbleBlast()
            : base(
                name:        "Bubble Blast",
                description: "Deal {0} [Water] damage. This spell scales with maximum [Mana].",
                manaCost:        2,
                actionPointCost: 1,
                range:       4,
                area:        0,
                cooldown:    3,
                targetType:  SpellTargetType.Enemy,
                type:        SpellType.Water)
        {
        }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage")
                .Add($"Max Mana ({caster.CharacterStats.MaxMana} x 0.5, floored) + {caster.CharacterStats.SpellPowerBonus}", Mathf.FloorToInt(caster.CharacterStats.MaxMana * 0.5f) + caster.CharacterStats.SpellPowerBonus);
            return new[] { damage };
        }

        public override ActionPreview GetPreview(Character caster, Character target)
        {
            ScalingValue[] values = ComputeValues(caster);
            return new ActionPreview
            {
                RawDamage         = values[0].Total,
            };
        }

        public override bool Execute(SpellContext context)
        {
            bool casterIsNpc = context.Caster is Npc;

            // From an Npc's perspective the enemy is a player Character; from a
            // player Character's perspective the enemy is an Npc.
            Character target = casterIsNpc
                ? context.TargetTile?.OccupyingCharacter
                : context.TargetTile?.OccupyingNpc;

            if (target == null)
            {
                Debug.LogWarning("[BubbleBlast] No valid target on the target tile.");
                return false;
            }

            ScalingValue[] values = ComputeValues(context.Caster);
            int damage = values[0].Total;

            // Deal water damage.
            DealDamage(target, context.TargetTile, damage, SpellType.Water, "BubbleBlast");

            Debug.Log($"[BubbleBlast] '{context.Caster.CharacterName}' casts Bubble Blast on " +
                      $"'{target.CharacterName}'.  Damage: {damage}, " +
                      $"Spell Power Bonus: {context.Caster.CharacterStats.SpellPowerBonus}.  " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}" +
                      $"/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
