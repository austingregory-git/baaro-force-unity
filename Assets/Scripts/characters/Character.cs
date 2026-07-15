using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Statuses;
using BaaroForce.Map;

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
        /// <summary>Current level; used for spell and ability power scaling. Defaults to 1.</summary>
        /// Should a character have their current tile stored here?  Or should the map manager handle that?
        public MapTile characterCurrentTile { get; set; }

        public int Level { get; set; } = 1;

        /// <summary>Status effects currently active on this character.</summary>
        public List<StatusEffect> ActiveEffects { get; } = new List<StatusEffect>();

        /// <summary>
        /// Applies a status effect to this character, calling its OnApply hook immediately.
        /// If an effect of the same type is already active it is removed first.
        /// </summary>
        public void ApplyStatus(StatusEffect effect)
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i].Name == effect.Name)
                {
                    ActiveEffects[i].OnRemove(characterStats);
                    ActiveEffects.RemoveAt(i);
                }
            }
            effect.OnApply(characterStats);
            ActiveEffects.Add(effect);
            Debug.Log($"[Character] '{characterName}' afflicted with {effect.Name} ({effect.RemainingTurns} turn(s)).");
        }

        /// <summary>
        /// Ticks all active effects at the start of this character's turn.
        /// Expired effects are removed and their OnRemove hooks are called.
        /// </summary>
        public void TickStatusEffects()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect fx = ActiveEffects[i];
                fx.OnTurnStart(characterStats);
                if (fx.Tick())
                {
                    fx.OnRemove(characterStats);
                    ActiveEffects.RemoveAt(i);
                    Debug.Log($"[Character] '{characterName}': {fx.Name} has expired.");
                }
            }
        }

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