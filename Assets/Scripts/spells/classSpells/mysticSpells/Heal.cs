using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Heal — restore 3 + 1.5*SpellPower + (0.5 * Level) health to a single target within 3 tiles.
    /// </summary>
    public class Heal : ClassSpell
    {
        public Heal() : base(
            characterClass: ClassRegistry.Get("Mystic"),
            name:        "Heal",
            description: "Restore {0} health to a single target.",
            manaCost:        3,
            actionPointCost: 1,
            range:       3,
            area:        0,
            cooldown:    0,
            targetType:  SpellTargetType.Ally,
            type:        SpellType.Earth)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var healing = new ScalingValue("Healing")
                .Add("Base", 3)
                .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 1.5, floored)", Mathf.FloorToInt(caster.CharacterStats.SpellPowerBonus * 1.5f))
                .Add("Level", Mathf.FloorToInt(caster.Level * 0.5f));
            return new[] { healing };
        }


        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawHeal = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {
            bool casterIsNpc = context.Caster is Npc;

            // Heal targets an ally, so it resolves against the caster's own side:
            // an Npc's allies are other Npcs; a player Character's allies (including itself)
            // are other player Characters.
            Character target = casterIsNpc
                ? context.TargetTile?.OccupyingNpc
                : context.TargetTile?.OccupyingCharacter;

            if (target == null)
            {
                Debug.LogWarning("[Heal] No target found on the selected tile.");
                return false;
            }

            int healing = ComputeValues(context.Caster)[0].Total;
            ApplyHealing(target, context.TargetTile, healing, "Heal");

            Debug.Log($"[Heal] '{context.Caster.CharacterName}' restored {healing} health to '{target.CharacterName}'. " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
