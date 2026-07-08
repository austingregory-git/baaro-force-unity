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
        public List<Realm> characterRealms { get; set; }
        public List<PassiveAbility> characterPassiveAbilities { get; set; }
        public List<Spell> characterSpells { get; set; }
        //public List<Equipment> characterEquipment { get; set; }
        public string characterImagePath { get; set; }

        public Character(
                        CharacterClass characterClass, 
                        string characterName, 
                        CharacterStats characterStats, 
                        List<Realm> characterRealms, 
                        List<PassiveAbility> characterPassiveAbilities, 
                        List<Spell> characterSpells,
                        string characterImagePath)
        {
            this.characterClass = characterClass;
            this.characterName = characterName;
            this.characterStats = characterStats;
            this.characterRealms = characterRealms ?? new List<Realm>();
            this.characterPassiveAbilities = characterPassiveAbilities ?? new List<PassiveAbility>();
            this.characterSpells = characterSpells ?? new List<Spell>();
            this.characterImagePath = characterImagePath;
            //this.characterEquipment = characterEquipment ?? new List<Equipment>();
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