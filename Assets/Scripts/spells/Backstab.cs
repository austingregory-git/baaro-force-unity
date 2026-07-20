using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.UI;
using UnityEngine;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Backstab — strike an enemy from behind for {0} damage.
    /// Deals 2x TotalDamage if caster is behind the target (i.e. the caster's tile is directly opposite the target's facing direction).
    /// </summary>
    public class Backstab : ClassSpell
    {
        public Backstab() : base(
            characterClass: ClassRegistry.Get("Rogue"),
            name:        "Backstab",
            description: "Strike an enemy from behind for {0} damage.",
            manaCost:        0,
            actionPointCost: 1,
            range:       1,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy)
        { }

        public override ScalingValue[] ComputeValues(Character caster) =>
            new[] { new ScalingValue("Damage").AddTotalAttack(caster.CharacterStats) };


        public override ActionPreview GetPreview(Character caster, Character target) =>
            new ActionPreview { RawDamage = ComputeValues(caster)[0].Total };

        public override bool Execute(SpellContext context)
        {


            return true;
        }
    }
}