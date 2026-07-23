using System.Collections.Generic;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Hans : Character
    {
        static readonly string HansProfilePicPath = "hans_profile_pic_128x128";

        static readonly SpriteKit HansSpriteKit = new SpriteKit(
            backLeftSpritePath: "hans_back_left_128x128",
            backRightSpritePath: "hans_back_right_128x128",
            frontLeftSpritePath: "hans_front_left_128x128",
            frontRightSpritePath: "hans_front_right_128x128",
            idleSpritePaths: null,
            walkSpritePaths: null,
            attackSpritePaths: null,
            deathSpritePaths: null);

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
                characterProfilePicPath: HansProfilePicPath,
                characterSpriteKit: HansSpriteKit)
        {
        }
    }
}