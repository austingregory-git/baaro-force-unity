using System.Collections.Generic;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Shopu : Character
    {
        static readonly string ShopuProfilePicPath = "shopu_profile_pic_128x128";
        static readonly SpriteKit ShopuSpriteKit = new SpriteKit(
            backLeftSpritePath: "shopu_back_left_128x128",
            backRightSpritePath: "shopu_back_right_128x128",
            frontLeftSpritePath: "shopu_front_left_128x128",
            frontRightSpritePath: "shopu_front_right_128x128",
            idleSpritePaths: null,
            walkSpritePaths: null,
            attackSpritePaths: null,
            deathSpritePaths: null);
        public Shopu()
            : base(
                characterClass: ClassRegistry.Get("Rogue"),
                characterName: "Shopu",
                characterStats: new CharacterStats(maxHealthPoints: 3, baseAttack: 5, maxMana: 6, movement: 5),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new InTheTrees()
                },
                characterSpells: new List<Spell>
                {
                    new AcornSpray(),
                },
                characterProfilePicPath: ShopuProfilePicPath,
                characterSpriteKit: ShopuSpriteKit)
        {
        }
    }
}