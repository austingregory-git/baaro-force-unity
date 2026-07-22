using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Statuses;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Rejuvenate — apply [Regen] to a single target within 3 tiles.
    /// The amount of regen is 1 + (0.25 * SpellPower)
    /// </summary>
    public class Rejuvenate : ClassSpell
    {
        private const int RegenDurationTurns = 3;

        public Rejuvenate() : base(
            characterClass: ClassRegistry.Get("Mystic"),
            name:        "Rejuvenate",
            description: "Apply {0} [Regen] to an ally for 3 turns.",
            manaCost:        3,
            actionPointCost: 1,
            range:       2,
            area:        0,
            cooldown:    1,
            targetType:  SpellTargetType.Ally,
            type:        SpellType.Buff)
        { }


        public override ScalingValue[] ComputeValues(Character caster)
        {
            var healing = new ScalingValue("Healing")
                .Add("Base", 1)
                .Add($"Spell Power Bonus ({caster.CharacterStats.SpellPowerBonus} × 0.25, floored)", Mathf.FloorToInt(caster.CharacterStats.SpellPowerBonus * 0.25f));
            return new[] { healing };
        }


        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview
            {
                StatusEffectName = "Regen",
                StatusEffectKind = StatusEffect.StatusEffectType.Buff,
            };

        public override bool Execute(SpellContext context)
        {
            bool casterIsNpc = context.Caster is Npc;

            // Rejuvenate targets an ally, so it resolves against the caster's own side:
            // an Npc's allies are other Npcs; a player Character's allies (including itself)
            // are other player Characters.
            Character target = casterIsNpc
                ? context.TargetTile?.OccupyingNpc
                : context.TargetTile?.OccupyingCharacter;

            if (target == null)
            {
                Debug.LogWarning("[Rejuvenate] No target found on the selected tile.");
                return false;
            }

            int healing = ComputeValues(context.Caster)[0].Total;
            target.ApplyStatus(new RegenStatus(RegenDurationTurns, healing));

            Debug.Log($"[Rejuvenate] '{context.Caster.CharacterName}' applied {healing} [Regen] to '{target.CharacterName}' for {RegenDurationTurns} turns. " +
                      $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}/{target.CharacterStats.MaxHealthPoints}");

            return true;
        }
    }
}
