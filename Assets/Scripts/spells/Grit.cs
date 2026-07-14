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
            cost:        2,
            range:       0,
            area:        0,
            cooldown:    3,
            targetType:  SpellTargetType.Self)
        { }

        public override bool Execute(SpellContext context)
        {
            int bonus = Mathf.FloorToInt(3f + 0.5f * context.CasterLevel);
            context.Caster.characterStats.maxHealthPoints += bonus;
            context.Caster.characterStats.healthPoints    += bonus;

            Debug.Log($"[Grit] '{context.Caster.characterName}' gained {bonus} max HP.  " +
                      $"HP: {context.Caster.characterStats.healthPoints}" +
                      $"/{context.Caster.characterStats.maxHealthPoints}");
            return true;
        }
    }
}