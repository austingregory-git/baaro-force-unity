using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Grit — dig deep and permanently expand maximum health for this battle.
    /// Level scaling: bonus = floor(3 + 0.5 × level)   (3 HP at level 1)
    /// </summary>
    public class Grit : ClassSpell
    {
        public Grit() : base(
            characterClass: ClassRegistry.Get("Warrior"),
            name:        "Grit",
            description: "Gain {0} maximum health for the fight.",
            manaCost:        2,
            actionPointCost: 1,
            range:       0,
            area:        0,
            cooldown:    3,
            targetType:  SpellTargetType.Self)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[]
            {
                new ScalingValue("Max Health")
                    .Add("Base", 3)
                    .Add($"Level ({caster.Level} × 0.5, floored)", Mathf.FloorToInt(caster.Level * 0.5f))
            };

        public override bool Execute(SpellContext context)
        {
            int bonus = ComputeValues(context.Caster)[0].Total;
            context.Caster.CharacterStats.MaxHealthPoints += bonus;
            context.Caster.CharacterStats.HealthPoints    += bonus;

            Debug.Log($"[Grit] '{context.Caster.CharacterName}' gained {bonus} max HP.  " +
                      $"HP: {context.Caster.CharacterStats.HealthPoints}" +
                      $"/{context.Caster.CharacterStats.MaxHealthPoints}");
            return true;
        }
    }
}