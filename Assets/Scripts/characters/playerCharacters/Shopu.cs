using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Shopu : Character
    {
        static readonly CharacterStats ShopuBaseStats = new CharacterStats(maxHealthPoints: 8, baseAttack: 3, maxMana: 5, movement: 4);
        static readonly string ShopuModelPath = "Characters/frog_wizard_test";
        public Shopu()
            : base(
                characterClass: ClassRegistry.Get("Rogue"),
                characterName: "Shopu", 
                characterStats: ShopuBaseStats, 
                characterRealms: new List<Realm> { Realm.Dark },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new AutumnalGrowth()
                },
                characterSpells: new List<Spell>
                {
                    new DeathStare(),
                },
                characterModelPath: ShopuModelPath)
        {
        }
    }
}