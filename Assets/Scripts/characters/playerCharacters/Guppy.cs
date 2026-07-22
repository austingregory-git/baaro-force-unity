using System.Collections.Generic;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Guppy : Character
    {
        static readonly string GuppyProfilePicPath = "guppy_profile_pic_128x128";

        static readonly SpriteKit GuppySpriteKit = new SpriteKit(
            backLeftSpritePath: "guppy_back_left_128x128",
            backRightSpritePath: "guppy_back_right_128x128",
            frontLeftSpritePath: "guppy_front_left_128x128",
            frontRightSpritePath: "guppy_front_right_128x128",
            idleSpritePaths: null,
            walkSpritePaths: null,
            attackSpritePaths: null,
            deathSpritePaths: null);

        public Guppy()
            : base(
                characterClass: ClassRegistry.Get("Mage"),
                characterName: "Guppy", 
                characterStats: new CharacterStats(maxHealthPoints: 5, baseAttack: 2, maxMana: 8, movement: 3),
                characterRealms: new List<Realm> { Realm.Water },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new BubbleShield()
                },
                characterSpells: new List<Spell>
                {
                    new BubbleBlast(),
                },
                characterProfilePicPath: GuppyProfilePicPath,
                characterSpriteKit: GuppySpriteKit)
        {
        }
    }
}