using System.Collections.Generic;
using BaaroForce.Classes;

namespace BaaroForce.Characters
{
    public class Beepo : Character
    {
        static readonly CharacterStats BEEPO_BASE_STATS = new CharacterStats(healthPoints: 8, baseAttack: 2, mana: 4, movement: 3);
        static readonly string BEEPO_IMAGE_PATH = "Characters/beepo_48x36";
        public Beepo()
            : base(new Warrior(), "Beepo", BEEPO_BASE_STATS, new List<Realm> { Realm.FIRE }, new List<PassiveAbility>(), new List<Spell>(), BEEPO_IMAGE_PATH)
        {
        }
    }
}