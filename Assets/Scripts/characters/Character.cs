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
    public abstract class Character
    {
        public CharacterClass CharacterClass { get; set; }
        public string CharacterName { get; set; }
        public CharacterStats CharacterStats { get; set; }
        public List<Realm> CharacterRealms { get; set; }
        public List<PassiveAbility> CharacterPassiveAbilities { get; set; }
        public List<Spell> CharacterSpells { get; set; }
        //public List<Equipment> characterEquipment { get; set; }
        /// <summary>Resources-relative path to this character's profile picture sprite,
        /// used by CharacterSelectionManager to render its card portrait.</summary>
        public string CharacterProfilePicPath { get; set; }

        /// Should a character have their current tile stored here?  Or should the map manager handle that?
        public MapTile CharacterCurrentTile { get; set; }

        /// <summary>Current level; used for spell and ability power scaling. Defaults to 1.</summary>
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
                    ActiveEffects[i].OnRemove(CharacterStats);
                    ActiveEffects.RemoveAt(i);
                }
            }
            effect.OnApply(CharacterStats);
            ActiveEffects.Add(effect);
            Debug.Log($"[{GetType().Name}] '{CharacterName}' afflicted with {effect.Name} ({effect.RemainingTurns} turn(s)).");
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
                fx.OnTurnStart(CharacterStats);
                if (fx.Tick())
                {
                    fx.OnRemove(CharacterStats);
                    ActiveEffects.RemoveAt(i);
                    Debug.Log($"[{GetType().Name}] '{CharacterName}': {fx.Name} has expired.");
                }
            }
        }

        protected Character(
                        CharacterClass characterClass,
                        string characterName,
                        CharacterStats characterStats,
                        List<Realm> characterRealms,
                        List<PassiveAbility> characterPassiveAbilities,
                        List<Spell> characterSpells,
                        string characterProfilePicPath)
        {
            this.CharacterClass = characterClass;
            this.CharacterName = characterName;
            this.CharacterStats = characterStats;
            this.CharacterRealms             = characterRealms             ?? new List<Realm>();
            this.CharacterPassiveAbilities   = characterPassiveAbilities   ?? new List<PassiveAbility>();
            this.CharacterSpells             = characterSpells             ?? new List<Spell>();
            this.CharacterProfilePicPath     = characterProfilePicPath;

            // Append one randomly selected class spell from this character's class.
            ClassSpell classSpell = SpellRegistry.GetRandomClassSpell(characterClass?.ClassID);
            if (classSpell != null)
                this.CharacterSpells.Add(classSpell);
            //this.characterEquipment = characterEquipment ?? new List<Equipment>();
        }
    }

    public enum Realm
    {
        Fire,
        Water,
        Earth,
        Wind,
        Dark,
        Light
    }
}