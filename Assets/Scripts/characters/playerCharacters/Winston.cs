using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Winston : Character
    {
        static readonly CharacterStats WinstonBaseStats = new CharacterStats(maxHealthPoints: 8, baseAttack: 3, maxMana: 5, movement: 4);
        static readonly string WinstonModelPath = "Characters/frog_wizard_test";
        public Winston()
            : base(
                characterClass: ClassRegistry.Get("Warrior"),
                characterName: "Winston", 
                characterStats: WinstonBaseStats, 
                characterRealms: new List<Realm> { Realm.Dark },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new AutumnalGrowth()
                },
                characterSpells: new List<Spell>
                {
                    new DeathStare(),
                },
                characterModelPath: WinstonModelPath)
        {
        }
    }
}