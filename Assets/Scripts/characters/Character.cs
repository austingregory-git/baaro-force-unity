using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

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
        public string characterModelPath { get; set; }

        public Character(
                        CharacterClass characterClass, 
                        string characterName, 
                        CharacterStats characterStats, 
                        List<Realm> characterRealms, 
                        List<PassiveAbility> characterPassiveAbilities, 
                        List<Spell> characterSpells,
                        string characterModelPath)
        {
            this.characterClass = characterClass;
            this.characterName = characterName;
            this.characterStats = characterStats;
            this.characterRealms             = characterRealms             ?? new List<Realm>();
            this.characterPassiveAbilities   = characterPassiveAbilities   ?? new List<PassiveAbility>();
            this.characterSpells             = characterSpells             ?? new List<Spell>();
            this.characterModelPath          = characterModelPath;

            // Append one randomly selected class spell from this character's class.
            ClassSpell classSpell = SpellRegistry.GetRandomClassSpell(characterClass?.classID);
            if (classSpell != null)
                this.characterSpells.Add(classSpell);
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