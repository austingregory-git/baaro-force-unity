using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
namespace BaaroForce.Characters
{
    public class Beepo : Character
    {
        static readonly CharacterStats BeepoBaseStats = new CharacterStats(maxHealthPoints: 8, baseAttack: 2, maxMana: 4, movement: 3);
        static readonly string BeepoModelPath = "Characters/frog_wizard_test";
        public Beepo()
            : base(
                characterClass: ClassRegistry.Get("Warrior"),
                characterName: "Beepo",
                characterStats: BeepoBaseStats, 
                characterRealms: new List<Realm> { Realm.FIRE }, 
                characterPassiveAbilities: new List<PassiveAbility>()
                {
                    new BurningShield()
                },
                characterSpells: new List<Spell>(), 
                characterModelPath: BeepoModelPath)
        {
        }
    }
}