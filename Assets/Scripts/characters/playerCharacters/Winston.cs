using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;

namespace BaaroForce.Characters
{
    public class Winston : Character
    {
        static readonly CharacterStats WINSTON_BASE_STATS = new CharacterStats(healthPoints: 8, baseAttack: 3, mana: 5, movement: 4);
        static readonly string WINSTON_MODEL_PATH = "Characters/frog_wizard_test";
        public Winston()
            : base(new Warrior(), "Winston", WINSTON_BASE_STATS, new List<Realm> { Realm.DARK },
                   new List<PassiveAbility>
                   {
                       new PassiveAbility("Autumnal Growth",
                           "[Regen] 1 + 0.25 x [Level] health points per turn")
                   },
                   new List<Spell>
                   {
                       new Spell("Death Stare",
                           "[Fear] an enemy for 1 + 0.25 x [Level] turns")
                   },
                   WINSTON_MODEL_PATH)
        {
        }

        public PassiveAbility GetWinstonPassiveAbility()
        {
            return new PassiveAbility("Autumnal Growth",
                "[Regen] 1 + 0.25 x [Level] health points per turn");
        }

        public Spell GetWinstonSpell()
        {
            return new Spell("Death Stare",
                "[Fear] an enemy for 1 + 0.25 x [Level] turns");
        }
    }
}