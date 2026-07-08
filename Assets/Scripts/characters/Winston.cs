using System.Collections.Generic;
using BaaroForce.Classes;

namespace BaaroForce.Characters
{
    public class Winston : Character
    {
        static readonly CharacterStats WINSTON_BASE_STATS = new CharacterStats(healthPoints: 8, baseAttack: 3, mana: 5, movement: 4);
        public Winston()
            : base(new Mage(), "Winston", WINSTON_BASE_STATS, Realm.DARK, new List<PassiveAbility>(), new List<Spell>())
        {
        }
    }
}