using BaaroForce.Classes;
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
            description: "Gain 3 + 0.5 × [Level] maximum health for the fight.",
            manaCost:        2,
            actionPointCost: 1,
            range:       0,
            area:        0,
            cooldown:    3,
            targetType:  SpellTargetType.Self)
        { }

        public override bool Execute(SpellContext context)
        {
            int bonus = Mathf.FloorToInt(3f + 0.5f * context.CasterLevel);
            context.Caster.CharacterStats.MaxHealthPoints += bonus;
            context.Caster.CharacterStats.HealthPoints    += bonus;

            Debug.Log($"[Grit] '{context.Caster.CharacterName}' gained {bonus} max HP.  " +
                      $"HP: {context.Caster.CharacterStats.HealthPoints}" +
                      $"/{context.Caster.CharacterStats.MaxHealthPoints}");
            return true;
        }
    }
}