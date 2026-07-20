using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using UnityEngine;
using System.Collections.Generic;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Cleave — strike in front of you with your weapon, dealing damage equal to your basic attack.
    /// </summary>
    public class Slam : ClassSpell
    {
        public Slam() : base(
            characterClass: ClassRegistry.Get("Warrior"),
            name:        "Slam",
            description: "Slam into the target, dealing {0} damage.",
            manaCost:        0,
            actionPointCost: 1,
            range:       1,
            area:        0,
            cooldown:    2,
            targetType:  SpellTargetType.Enemy)
        { }

        public override ScalingValue[] ComputeValues(Character caster)
        {
            var damage = new ScalingValue("Damage")
                .Add("Base", 3)
                .Add($"Level ({caster.Level} × 0.5, floored)", Mathf.FloorToInt(caster.Level * 0.5f));
            damage.AddTotalAttack(caster.CharacterStats);
            return new[] { damage };
        }

        public override bool Execute(SpellContext context)
        {
            if (context.TargetTile.IsOccupied && context.TargetTile.OccupyingNpc != null)
            {
                Npc target = context.TargetTile.OccupyingNpc;
                int damage = ComputeValues(context.Caster)[0].Total;
                target.CharacterStats.TakeDamage(damage);
                Debug.Log($"[Slam] '{context.Caster.CharacterName}' dealt {damage} damage to '{target.CharacterName}'. " +
                          $"HP: {target.CharacterStats.HealthPoints}/{target.CharacterStats.MaxHealthPoints}");
                if (target.CharacterStats.HealthPoints <= 0)
                {
                    Debug.Log($"[Slam] '{target.CharacterName}' has been defeated!");
                    context.TargetTile.RemoveUnit();
                }
            }
            else
            {
                Debug.LogWarning("[Slam] No target found on the selected tile.");
                return false;
            }
            return true;
        }
    }
}