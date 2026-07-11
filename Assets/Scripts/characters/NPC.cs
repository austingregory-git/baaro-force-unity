using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{   
    public class NPC
    {
        public string characterName { get; set; }
        public CharacterStats characterStats { get; set; }
        public List<Realm> characterRealms { get; set; }
        public List<PassiveAbility> characterPassiveAbilities { get; set; }
        public List<Spell> characterSpells { get; set; }
        //public List<Equipment> characterEquipment { get; set; }
        public string characterModelPath { get; set; }

        /// <summary>The NPC's current level; defaults to 1.</summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Base perceived difficulty of this NPC type (independent of level).
        /// Override in each concrete NPC subclass.
        /// </summary>
        public virtual int BaseStrengthIndex => 1;

        /// <summary>Effective strength used when building an enemy pack: BaseStrengthIndex × Level.</summary>
        public int StrengthIndex => BaseStrengthIndex * Level;

        public NPC(
                        string characterName, 
                        CharacterStats characterStats, 
                        List<Realm> characterRealms, 
                        List<PassiveAbility> characterPassiveAbilities, 
                        List<Spell> characterSpells,
                        string characterModelPath)
        {
            this.characterName = characterName;
            this.characterStats = characterStats;
            this.characterRealms             = characterRealms             ?? new List<Realm>();
            this.characterPassiveAbilities   = characterPassiveAbilities   ?? new List<PassiveAbility>();
            this.characterSpells             = characterSpells             ?? new List<Spell>();
            this.characterModelPath          = characterModelPath;

            // Append one randomly selected class spell from the NPC pool.
            // Get a random class spell from the NPC pool.
            // ClassSpell classSpell = SpellRegistry.GetRandomClassSpell();
            // if (classSpell != null)
            //     this.characterSpells.Add(classSpell);
            //this.characterEquipment = characterEquipment ?? new List<Equipment>();
        }
    }
}