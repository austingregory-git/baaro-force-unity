using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Winston : Character
    {
        static readonly CharacterStats WINSTON_BASE_STATS = new CharacterStats(maxHealthPoints: 8, baseAttack: 3, maxMana: 5, movement: 4);
        static readonly string WINSTON_MODEL_PATH = "Characters/frog_wizard_test";
        public Winston()
            : base(
                ClassRegistry.Get("Warrior"), 
                "Winston", WINSTON_BASE_STATS, new List<Realm> { Realm.DARK },
                new List<PassiveAbility>
                {
                    new AutumnalGrowth()
                },
                new List<Spell>
                {
                    new DeathStare(),
                },
                WINSTON_MODEL_PATH)
        {
        }
    }
}