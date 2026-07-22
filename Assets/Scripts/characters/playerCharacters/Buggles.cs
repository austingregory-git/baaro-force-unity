using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Buggles : Character
    {
        static readonly string BugglesProfilePicPath = "winston_profile_pic_128x128";
        public Buggles()
            : base(
                characterClass: ClassRegistry.Get("Mystic"),
                characterName: "Buggles", 
                characterStats: new CharacterStats(maxHealthPoints: 5, baseAttack: 2, maxMana: 9, movement: 2),
                characterRealms: new List<Realm> { Realm.Water },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new SpiritualProtector()
                    // Spiritual Protector is a passive ability with the following effect: Once per combat, when a nearby ally is reduced below 50% health, Heal them 4 + Level health.
                    // This should abide by the TrueCircle area type (as seen by ArcaneExplosion.cs) with an area of 2
                },
                characterSpells: new List<Spell>
                {
                    new SongOfTheElders(),
                    // Song of the Elders is a Cone spell (see AcornSpray.cs for reference) that applies haste to all allies in the area for 2 turns and slows all enemies in the area for 2 turns.
                    // The spell costs 4 mana and has a CD of 3 turns. It has the same range and area as Acorn Spray (range 1, area 3). 
                },
                characterProfilePicPath: BugglesProfilePicPath)
        {
        }
    }
}