using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;

namespace BaaroForce.Characters
{
    public class Beepo : Character
    {
        static readonly CharacterStats BEEPO_BASE_STATS = new CharacterStats(healthPoints: 8, baseAttack: 2, mana: 4, movement: 3);
        static readonly string BEEPO_MODEL_PATH = "Characters/frog_wizard_test";
        public Beepo()
            : base(
                ClassRegistry.Get("Warrior"), 
                "Beepo", BEEPO_BASE_STATS, 
                new List<Realm> { Realm.FIRE }, 
                new List<PassiveAbility>(), 
                new List<Spell>(), 
                BEEPO_MODEL_PATH)
        {
        }

        public PassiveAbility GetBeepoPassiveAbility()
        {
            return new PassiveAbility("Beepo's Passive Ability", "This is Beepo's passive ability description.");
        }
    }
}