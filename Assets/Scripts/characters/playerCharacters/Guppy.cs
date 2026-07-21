using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Guppy : Character
    {
        static readonly CharacterStats GuppyBaseStats = new CharacterStats(maxHealthPoints: 5, baseAttack: 2, maxMana: 8, movement: 3);
        static readonly string GuppyProfilePicPath = "winston_profile_pic_128x128";
        public Guppy()
            : base(
                characterClass: ClassRegistry.Get("Mage"),
                characterName: "Guppy", 
                characterStats: GuppyBaseStats, 
                characterRealms: new List<Realm> { Realm.Water },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new BubbleShield()
                },
                characterSpells: new List<Spell>
                {
                    new BubbleBlast(),
                },
                characterProfilePicPath: GuppyProfilePicPath)
        {
        }
    }
}