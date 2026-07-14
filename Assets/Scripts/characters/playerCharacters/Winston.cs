using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Winston : Character
    {
        static readonly CharacterStats WINSTON_BASE_STATS = new CharacterStats(healthPoints: 8, baseAttack: 3, mana: 5, movement: 4);
        static readonly string WINSTON_MODEL_PATH = "Characters/frog_wizard_test";
        public Winston()
            : base(
                ClassRegistry.Get("Warrior"), 
                "Winston", WINSTON_BASE_STATS, new List<Realm> { Realm.DARK },
                new List<PassiveAbility>
                {
                    new PassiveAbility("Autumnal Growth",
                        "[Regen] 1 + 0.25 x [Level] health points per turn")
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