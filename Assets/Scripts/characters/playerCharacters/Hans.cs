using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Hans : Character
    {
        static readonly string HansProfilePicPath = "winston_profile_pic_128x128";
        public Hans()
            : base(
                characterClass: ClassRegistry.Get("Archer"),
                characterName: "Hans",
                characterStats: new CharacterStats(maxHealthPoints: 5, baseAttack: 3, maxMana: 7, movement: 4),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new LongBow()
                    // All spells and attacks gain +1 range
                },
                characterSpells: new List<Spell>
                {
                    new StarShot(),
                    // Infinite range - Deals TotalAttack damage to an enemy. Mana cost: 2, Cooldown: 2
                },
                characterProfilePicPath: HansProfilePicPath)
        {
        }
    }
}