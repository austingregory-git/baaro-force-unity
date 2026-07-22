using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Poison — Poison an enemy 1 + (0.25 * SpellPower).
    /// Poison is a damage-over-time effect that deals 1 + (0.25 * SpellPower) damage at the start of the target's turn. Lasts the entire battle or until the target is cured of the effect.
    /// </summary>
    public class Poison : ClassSpell
    {
        public Poison() : base(
            characterClass: ClassRegistry.Get("Mystic"),
            name:        "Poison",
            description: "[Poison] an enemy for {0}",
            manaCost:        2,
            actionPointCost: 1,
            range:       2,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy,
            type:        SpellType.Earth)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var poison = new ScalingValue("Poison")
                .Add("Base", 1)
                .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 0.25, floored)", Mathf.FloorToInt(caster.CharacterStats.SpellPowerBonus * 0.25f));
            return new[] { poison };
        }


        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

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
                Debug.LogWarning("[Poison] No target found on the selected tile.");
                return false;
            }

            int poisonAmount = ComputeValues(context.Caster)[0].Total;
            ApplyPoison(target, context.TargetTile, poisonAmount, "Poison");

            Debug.Log($"[Poison] '{context.Caster.CharacterName}' applied {poisonAmount} poison damage to '{target.CharacterName}'. " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
