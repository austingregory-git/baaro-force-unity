using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;

namespace BaaroForce.Characters
{   
    public class Character
    {
        public CharacterClass characterClass { get; set; }
        public string characterName { get; set; }
        public CharacterStats characterStats { get; set; }
        public Realm characterRealm { get; set; }
        public List<PassiveAbility> characterPassiveAbilities { get; set; }
        public List<Spell> characterSpells { get; set; }

        public Character(
                        CharacterClass characterClass, 
                        string characterName, 
                        CharacterStats characterStats, 
                        Realm characterRealm, 
                        List<PassiveAbility> characterPassiveAbilities, 
                        List<Spell> characterSpells)
        {
            this.characterClass = characterClass;
            this.characterName = characterName;
            this.characterStats = characterStats;
            this.characterRealm = characterRealm;
            this.characterPassiveAbilities = characterPassiveAbilities ?? new List<PassiveAbility>();
            this.characterSpells = characterSpells ?? new List<Spell>();   
        }
    }

    public enum Realm
    {
        FIRE,
        WATER,
        EARTH,
        WIND,
        DARK,
        LIGHT
    }
}