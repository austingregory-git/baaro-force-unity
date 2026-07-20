using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
namespace BaaroForce.Characters
{
    public class Beepo : Character
    {
        static readonly CharacterStats BeepoBaseStats = new CharacterStats(maxHealthPoints: 8, baseAttack: 2, maxMana: 4, movement: 3);
        static readonly string BeepoProfilePicPath = "winston_profile_pic_128x128";
        public Beepo()
            : base(
                characterClass: ClassRegistry.Get("Warrior"),
                characterName: "Beepo",
                characterStats: BeepoBaseStats, 
                characterRealms: new List<Realm> { Realm.Fire }, 
                characterPassiveAbilities: new List<PassiveAbility>()
                {
                    new BurningShield()
                },
                characterSpells: new List<Spell>()
                {
                    new BallForm()
                }, 
                characterProfilePicPath: BeepoProfilePicPath)
        {
        }
    }
}