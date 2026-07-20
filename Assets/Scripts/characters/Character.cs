using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Statuses;
using BaaroForce.Map;
using BaaroForce.UI;

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

        /// <summary>Grid direction (dx,dz) this unit is currently facing, updated whenever it
        /// moves (see TurnManager.MoveUnitAlongPath). Used by positional effects like Backstab.
        /// Defaults to (0,1), matching the initial FrontRight sprite orientation set by
        /// SpriteCharacterView.Initialize.</summary>
        public Vector2Int FacingDirection { get; set; } = new Vector2Int(0, 1);

        /// <summary>Status effects currently active on this character.</summary>
        public List<StatusEffect> ActiveEffects { get; } = new List<StatusEffect>();

        /// <summary>True while a Silence effect prevents this character from casting spells.</summary>
        public bool IsSilenced => ActiveEffects.Exists(e => e.Name == "Silence");

        /// <summary>True while an Invisible effect prevents most Npcs from targeting this
        /// character with attacks or spells (see Npc.IgnoresInvisibility for exceptions).</summary>
        public bool IsInvisible => ActiveEffects.Exists(e => e.Name == "Invisible");

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
            FloatingCombatTextSystem.Instance?.ShowStatus(this, effect.Name, effect.EffectType);
            Debug.Log($"[{GetType().Name}] '{CharacterName}' afflicted with {effect.Name} ({effect.RemainingTurns} turn(s)).");
            SyncInvisibilityVisual();
        }

        /// <summary>
        /// Removes this character's active Invisible status early, if any. Should be called
        /// whenever the character attacks, casts a spell, or is damaged from any other
        /// source — the three ways Invisible can end before its duration runs out.
        /// </summary>
        public void BreakInvisibility()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i].Name == "Invisible")
                {
                    ActiveEffects[i].OnRemove(CharacterStats);
                    ActiveEffects.RemoveAt(i);
                    break;
                }
            }
            SyncInvisibilityVisual();
        }

        /// <summary>Applies the translucent shadow-tint to this character's on-map sprite
        /// while Invisible is active, and clears it otherwise. No-ops if the character
        /// currently has no live model on the map (e.g. still in deployment).</summary>
        private void SyncInvisibilityVisual()
        {
            SpriteCharacterView view = CharacterCurrentTile?.UnitObject?.GetComponent<SpriteCharacterView>();
            view?.SetInvisible(IsInvisible);
        }

        /// <summary>
        /// Applies physical damage, first checking for an active Dodge status — if present,
        /// the attack is avoided entirely (Dodge is consumed and no damage lands). Basic
        /// attacks and physical-typed spells should route damage through this instead of
        /// calling CharacterStats.TakeDamage directly, so Dodge is respected everywhere.
        /// </summary>
        public int TakePhysicalDamage(int amount)
        {
            if (TryConsumeDodge())
            {
                FloatingCombatTextSystem.Instance?.ShowStatus(this, "Dodge", StatusEffect.StatusEffectType.Buff);
                Debug.Log($"[{GetType().Name}] '{CharacterName}' dodged an attack.");
                return 0;
            }
            int dealt = CharacterStats.TakeDamage(amount);
            BreakInvisibility();
            return dealt;
        }

        /// <summary>Removes this character's active Dodge status (if any) and returns true if one was consumed.</summary>
        public bool TryConsumeDodge()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i].Name == "Dodge")
                {
                    ActiveEffects[i].OnRemove(CharacterStats);
                    ActiveEffects.RemoveAt(i);
                    return true;
                }
            }
            return false;
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
            SyncInvisibilityVisual();
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