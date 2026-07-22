using System.Collections.Generic;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
namespace BaaroForce.Characters
{
    public class Beepo : Character
    {
        static readonly string BeepoProfilePicPath = "beepo_profile_pic_128x128";

        static readonly SpriteKit BeepoSpriteKit = new SpriteKit(
            backLeftSpritePath: "beepo_back_left_128x128",
            backRightSpritePath: "beepo_back_right_128x128",
            frontLeftSpritePath: "beepo_front_left_128x128",
            frontRightSpritePath: "beepo_front_right_128x128",
            idleSpritePaths: null,
            walkSpritePaths: null,
            attackSpritePaths: null,
            deathSpritePaths: null);
            
        public Beepo()
            : base(
                characterClass: ClassRegistry.Get("Warrior"),
                characterName: "Beepo",
                characterStats: new CharacterStats(maxHealthPoints: 8, baseAttack: 2, maxMana: 4, movement: 3),
                characterRealms: new List<Realm> { Realm.Fire }, 
                characterPassiveAbilities: new List<PassiveAbility>()
                {
                    new BurningShield()
                },
                characterSpells: new List<Spell>()
                {
                    new BallForm()
                }, 
                characterProfilePicPath: BeepoProfilePicPath,
                characterSpriteKit: BeepoSpriteKit)
        {
        }
    }
}