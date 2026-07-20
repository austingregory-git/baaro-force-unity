using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Shopu : Character
    {
        static readonly CharacterStats ShopuBaseStats = new CharacterStats(maxHealthPoints: 3, baseAttack: 5, maxMana: 6, movement: 5);
        static readonly string ShopuProfilePicPath = "winston_profile_pic_128x128";
        public Shopu()
            : base(
                characterClass: ClassRegistry.Get("Rogue"),
                characterName: "Shopu", 
                characterStats: ShopuBaseStats, 
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new InTheTrees()
                },
                characterSpells: new List<Spell>
                {
                    new AcornSpray(),
                },
                characterProfilePicPath: ShopuProfilePicPath)
        {
        }
    }
}