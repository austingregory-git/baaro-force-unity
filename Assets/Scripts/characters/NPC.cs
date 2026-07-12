using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Statuses;

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

        /// <summary>The AI strategy that drives this NPC's decisions during the enemy turn.
        /// Set in each concrete subclass constructor.  NPCs with a null AI are skipped
        /// during the enemy turn.</summary>
        public NpcAI AI { get; set; }

        /// <summary>Status effects currently active on this NPC.</summary>
        public List<StatusEffect> ActiveEffects { get; } = new List<StatusEffect>();

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

        /// <summary>
        /// Applies a status effect to this NPC, calling its OnApply hook immediately.
        /// If an effect of the same type is already active it is removed first.
        /// </summary>
        public void ApplyStatus(StatusEffect effect)
        {
            // Remove any existing effect of the same name to avoid stacking.
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
            Debug.Log($"[NPC] '{characterName}' afflicted with {effect.Name} ({effect.RemainingTurns} turn(s)).");
        }

        /// <summary>
        /// Ticks all active effects at the start of this NPC's turn.
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
                    Debug.Log($"[NPC] '{characterName}': {fx.Name} has expired.");
                }
            }
        }
    }
}