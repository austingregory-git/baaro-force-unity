using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Items;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Statuses;
using BaaroForce.Map;
using BaaroForce.UI;
using BaaroForce.Utils;

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
        private readonly Dictionary<EquipmentSlotType, Equipment> _equippedSlots = new Dictionary<EquipmentSlotType, Equipment>();

        /// <summary>Canonical slot order, shared with any UI that needs to lay out all
        /// equipment slots (e.g. CharacterInspectUI) rather than hardcoding its own order.</summary>
        internal static readonly EquipmentSlotType[] AllEquipmentSlots =
        {
            EquipmentSlotType.Helmet, EquipmentSlotType.Chest, EquipmentSlotType.Legs,
            EquipmentSlotType.MainHand, EquipmentSlotType.OffHand, EquipmentSlotType.Trinket
        };

        /// <summary>Currently equipped items, one per slot, in a fixed Helmet/Chest/Legs/
        /// MainHand/OffHand/Trinket order. Read-only view over <see cref="_equippedSlots"/> —
        /// use <see cref="Equip"/>/<see cref="Unequip"/> to change what's equipped.</summary>
        public IEnumerable<Equipment> CharacterEquipment
        {
            get
            {
                foreach (EquipmentSlotType slot in AllEquipmentSlots)
                    if (_equippedSlots.TryGetValue(slot, out Equipment equipped))
                        yield return equipped;
            }
        }
        /// <summary>Resources-relative path to this character's profile picture sprite,
        /// used by CharacterSelectionManager to render its card portrait.</summary>
        public string CharacterProfilePicPath { get; set; }

        /// <summary>This character's isometric directional sprite set, used by
        /// SpriteCharacterView when its on-map unit is placed. Null falls back to
        /// MapTile's shared DefaultSpriteKit for characters without their own art yet.</summary>
        public SpriteKit CharacterSpriteKit { get; set; }

        /// Should a character have their current tile stored here?  Or should the map manager handle that?
        public MapTile CharacterCurrentTile { get; set; }

        /// <summary>Current level; used for spell and ability power scaling. Defaults to 1.</summary>
        public int Level { get; set; } = 1;

        /// <summary>Experience banked toward the next level-up. See <see cref="GrantExperience"/>.</summary>
        public int Experience { get; private set; }

        /// <summary>True once this character has reached Level 5 and is awaiting a class
        /// promotion choice (see <see cref="Promote"/>) — surfaced by ActMapController.</summary>
        public bool HasPendingPromotion { get; private set; }

        /// <summary>Level-ups reached since the last time <c>LevelUpUI</c> showed and cleared
        /// them — stat gains are already applied to <see cref="CharacterStats"/> by the time
        /// an event lands here; this is purely a replay log for the reveal screen.</summary>
        public List<LevelUpEvent> PendingLevelUpEvents { get; } = new List<LevelUpEvent>();

        private const int ExperiencePerLevel = 10;

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

        /// <summary>True while a Root effect prevents this character from moving.</summary>
        public bool IsRooted => ActiveEffects.Exists(e => e.Name == "Root");

        /// <summary>Bonus range (in tiles) added to this character's basic attacks and
        /// spells by its passive abilities — see <see cref="PassiveAbility.RangeBonus"/>
        /// (e.g. Long Bow).</summary>
        public int RangeBonus
        {
            get
            {
                int bonus = 0;
                foreach (PassiveAbility passive in CharacterPassiveAbilities)
                    bonus += passive.RangeBonus;
                return bonus;
            }
        }

        /// <summary>
        /// Applies a status effect to this character, calling its OnApply hook immediately.
        /// If an effect of the same name is already active, the two are merged via
        /// <see cref="StatusEffect.Stack"/> instead — see that method for how each status
        /// folds a re-application into its existing instance (stacked magnitude, refreshed
        /// duration, an extra banked charge, ...).
        /// </summary>
        public void ApplyStatus(StatusEffect effect)
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i].Name == effect.Name)
                {
                    StatusEffect existing = ActiveEffects[i];
                    existing.Stack(effect, CharacterStats);
                    FloatingCombatTextSystem.Instance?.ShowStatus(this, effect.Name, effect.EffectType);
                    Debug.Log($"[{GetType().Name}] '{CharacterName}' stacks {effect.Name} (now {existing.Stacks} stack(s), {existing.RemainingTurns} turn(s)).");
                    SyncInvisibilityVisual();
                    return;
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
        /// Falls through to <see cref="TakeDamage"/> for the actual (Bubble-Shield-aware)
        /// damage application.
        /// </summary>
        public int TakePhysicalDamage(int amount)
        {
            if (TryConsumeDodge())
            {
                FloatingCombatTextSystem.Instance?.ShowStatus(this, "Dodge", StatusEffect.StatusEffectType.Buff);
                Debug.Log($"[{GetType().Name}] '{CharacterName}' dodged an attack.");
                return 0;
            }
            return TakeDamage(amount);
        }

        /// <summary>
        /// Applies damage of any kind — physical or magical — first checking for an active
        /// Bubble Shield status, which absorbs the hit entirely (Bubble Shield is consumed
        /// and no damage lands) regardless of damage type. Every damage source (basic
        /// attacks via TakePhysicalDamage, and spells/passives directly) should route
        /// through this instead of calling CharacterStats.TakeDamage directly, so Bubble
        /// Shield is respected everywhere.
        /// </summary>
        public int TakeDamage(int amount)
        {
            if (TryConsumeBubbleShield())
            {
                FloatingCombatTextSystem.Instance?.ShowStatus(this, "Bubble Shield", StatusEffect.StatusEffectType.Buff);
                Debug.Log($"[{GetType().Name}] '{CharacterName}'s bubble shield absorbed a hit.");
                return 0;
            }
            int dealt = CharacterStats.TakeDamage(amount);
            BreakInvisibility();
            return dealt;
        }

        /// <summary>
        /// Removes a single banked charge from a stacked charge-based effect at
        /// <paramref name="index"/> in <see cref="ActiveEffects"/> — if more than one stack
        /// remains only the charge counter is decremented (see StatusEffect.ConsumeStack),
        /// otherwise the effect is fully removed via OnRemove, same as expiry. Shared by
        /// every "consume the next X" status: Dodge, Bubble Shield, Aim, Empower.
        /// </summary>
        private void ConsumeOneCharge(int index)
        {
            StatusEffect effect = ActiveEffects[index];
            if (effect.Stacks > 1)
            {
                effect.ConsumeStack();
            }
            else
            {
                effect.OnRemove(CharacterStats);
                ActiveEffects.RemoveAt(index);
            }
        }

        /// <summary>Removes this character's active Dodge status (if any) and returns true if one was consumed.</summary>
        public bool TryConsumeDodge()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i].Name == "Dodge")
                {
                    ConsumeOneCharge(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Removes this character's active Aim status (if any) and returns its damage
        /// multiplier — 1 (no change) if none was active. Consumed by TurnManager.ResolveBasicAttack.</summary>
        public int TryConsumeAim()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i] is AimStatus aim)
                {
                    ConsumeOneCharge(i);
                    return aim.Multiplier;
                }
            }
            return 1;
        }

        /// <summary>Removes this character's active Empower status (if any) and returns it —
        /// null if none was active. Consumed by TurnManager.ResolveBasicAttack.</summary>
        public EmpowerStatus TryConsumeEmpower()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i] is EmpowerStatus empower)
                {
                    ConsumeOneCharge(i);
                    return empower;
                }
            }
            return null;
        }

        /// <summary>Non-destructive read of this character's active Aim multiplier (1 if
        /// none) — for basic-attack damage previews, where TryConsumeAim would wrongly burn
        /// the status just from hovering a target. See CharacterHudController.RefreshAllPreviews.</summary>
        public int PeekAimMultiplier()
        {
            foreach (StatusEffect effect in ActiveEffects)
                if (effect is AimStatus aim) return aim.Multiplier;
            return 1;
        }

        /// <summary>Non-destructive read of this character's active Empower multiplier (1 if
        /// none) — same reasoning as <see cref="PeekAimMultiplier"/>.</summary>
        public int PeekEmpowerMultiplier()
        {
            foreach (StatusEffect effect in ActiveEffects)
                if (effect is EmpowerStatus empower) return empower.Multiplier;
            return 1;
        }

        /// <summary>Removes this character's active Bubble Shield status (if any) and returns true if one was consumed.</summary>
        public bool TryConsumeBubbleShield()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveEffects[i].Name == "Bubble Shield")
                {
                    ConsumeOneCharge(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears every temporary combat effect and restores HP/Mana/Shield to their
        /// pre-combat baseline — called once a fight is won, before the level-up reveal
        /// screen builds its cards, so a character who took damage or was buffed/debuffed
        /// mid-fight doesn't show a half-empty bar or a stale bonus for reasons that have
        /// nothing to do with leveling up. Equipment-derived bonuses (CharacterStats.AttackBonus
        /// etc. from AddEquipment) are untouched — those are permanent, not "temporary."
        /// </summary>
        public void ResetPostCombatState()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
                ActiveEffects[i].OnRemove(CharacterStats);
            ActiveEffects.Clear();

            CharacterStats.ShieldPoints = 0;
            CharacterStats.HealthPoints = CharacterStats.MaxHealthPoints;
            CharacterStats.Mana         = CharacterStats.MaxMana;

            SyncInvisibilityVisual();
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
                        string characterProfilePicPath,
                        SpriteKit characterSpriteKit = null)
        {
            this.CharacterClass = characterClass;
            this.CharacterName = characterName;
            this.CharacterStats = characterStats;
            this.CharacterRealms             = characterRealms             ?? new List<Realm>();
            this.CharacterPassiveAbilities   = characterPassiveAbilities   ?? new List<PassiveAbility>();
            this.CharacterSpells             = characterSpells             ?? new List<Spell>();
            this.CharacterProfilePicPath     = characterProfilePicPath;
            this.CharacterSpriteKit          = characterSpriteKit;

            // Append one randomly selected class spell from this character's class.
            ClassSpell classSpell = SpellRegistry.GetRandomClassSpell(characterClass?.ClassID);
            if (classSpell != null)
                this.CharacterSpells.Add(classSpell);
        }

        // ------------------------------------------------------------------ //
        // Equipment                                                            //
        // ------------------------------------------------------------------ //

        /// <summary>Returns whatever is currently equipped in <paramref name="slot"/>, or null
        /// if that slot is empty.</summary>
        public Equipment GetEquipped(EquipmentSlotType slot) =>
            _equippedSlots.TryGetValue(slot, out Equipment equipped) ? equipped : null;

        /// <summary>
        /// True if this character is allowed to equip <paramref name="equipment"/>. Only
        /// classified weapons (<see cref="Equipment.IsWeapon"/> + a non-null <see
        /// cref="Equipment.WeaponClassification"/>) are gated — armor and unclassified weapons
        /// are always equippable. A classified weapon requires the character's <see
        /// cref="CharacterClass.Specialty"/> to match (Melee/Ranged/Magic).
        /// </summary>
        public bool CanEquip(Equipment equipment)
        {
            if (equipment == null) return false;
            if (!equipment.IsWeapon || equipment.WeaponClassification == null) return true;
            return CharacterClass != null && CharacterClass.Specialty == equipment.WeaponClassification.Value;
        }

        /// <summary>
        /// Equips <paramref name="equipment"/> into its slot, auto-swapping out whatever was
        /// already there — the previous occupant's bonuses are removed first, the new item's
        /// bonuses are applied, and the displaced item (or null if the slot was empty) is
        /// returned so the caller can return it to the party's shared inventory bag.
        /// </summary>
        public Equipment Equip(Equipment equipment)
        {
            if (equipment == null) return null;

            _equippedSlots.TryGetValue(equipment.SlotType, out Equipment previous);
            if (previous != null) RemoveEquipmentBonuses(previous);

            _equippedSlots[equipment.SlotType] = equipment;
            ApplyEquipmentBonuses(equipment);
            return previous;
        }

        /// <summary>Removes and returns whatever is equipped in <paramref name="slot"/>, or
        /// null if that slot was already empty.</summary>
        public Equipment Unequip(EquipmentSlotType slot)
        {
            if (!_equippedSlots.TryGetValue(slot, out Equipment current)) return null;

            RemoveEquipmentBonuses(current);
            _equippedSlots.Remove(slot);
            return current;
        }

        /// <summary>
        /// Upgrades an already-equipped piece of equipment to its "+" variant (see
        /// <see cref="Equipment.Upgrade"/>) in place — removes the original's stat bonuses,
        /// replaces it with the upgraded copy in its slot, and applies the upgraded bonuses.
        /// Returns the upgraded item, or null if this character doesn't currently have
        /// <paramref name="equipment"/> equipped.
        /// </summary>
        public Equipment UpgradeEquipment(Equipment equipment)
        {
            if (equipment == null) return null;
            if (!_equippedSlots.TryGetValue(equipment.SlotType, out Equipment current) || current != equipment)
                return null;

            RemoveEquipmentBonuses(equipment);
            Equipment upgraded = equipment.Upgrade();
            _equippedSlots[equipment.SlotType] = upgraded;
            ApplyEquipmentBonuses(upgraded);
            return upgraded;
        }

        private void ApplyEquipmentBonuses(Equipment equipment)
        {
            CharacterStats.MaxHealthPoints += equipment.HealthBonus;
            CharacterStats.HealthPoints    += equipment.HealthBonus;
            CharacterStats.AttackBonus     += equipment.AttackBonus;
            CharacterStats.SpellPowerBonus += equipment.SpellPowerBonus;
            CharacterStats.MaxMana         += equipment.ManaBonus;
            CharacterStats.Mana            += equipment.ManaBonus;
            CharacterStats.Movement        += equipment.MovementBonus;
        }

        private void RemoveEquipmentBonuses(Equipment equipment)
        {
            CharacterStats.MaxHealthPoints -= equipment.HealthBonus;
            CharacterStats.HealthPoints    = Mathf.Min(CharacterStats.HealthPoints, CharacterStats.MaxHealthPoints);
            CharacterStats.AttackBonus     -= equipment.AttackBonus;
            CharacterStats.SpellPowerBonus -= equipment.SpellPowerBonus;
            CharacterStats.MaxMana         -= equipment.ManaBonus;
            CharacterStats.Mana            = Mathf.Min(CharacterStats.Mana, CharacterStats.MaxMana);
            CharacterStats.Movement        -= equipment.MovementBonus;
        }

        // ------------------------------------------------------------------ //
        // Experience, leveling, and class promotion                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Banks experience and applies as many level-ups as it covers (a single grant can
        /// cross more than one level, e.g. a 20xp boss-fight reward from Level 1). Each
        /// level-up rolls a class-growth-weighted stat increase (see <see cref="LevelUtils"/>),
        /// grants a random class talent at Level 3, and flags <see cref="HasPendingPromotion"/>
        /// at Level 5 for the Act Map to resolve.
        /// </summary>
        public void GrantExperience(int amount)
        {
            if (amount <= 0) return;
            Experience += amount;
            while (Experience >= ExperiencePerLevel)
            {
                Experience -= ExperiencePerLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            List<StatPointGain> gains = CharacterClass != null
                ? LevelUtils.RollAndApply(CharacterStats, CharacterClass.ClassGrowthWeights)
                : new List<StatPointGain>();
            var levelEvent = new LevelUpEvent(Level, gains);
            Debug.Log($"[{GetType().Name}] '{CharacterName}' reached level {Level}.");

            if (Level == 3)
            {
                PassiveAbility talent = PassiveAbilityRegistry.GetRandomClassPassive(CharacterClass?.ClassID);
                if (talent != null)
                {
                    CharacterPassiveAbilities.Add(talent);
                    levelEvent.TalentGained = talent.Name;
                    Debug.Log($"[{GetType().Name}] '{CharacterName}' learned the talent '{talent.Name}'.");
                }
            }
            else if (Level == 5)
            {
                HasPendingPromotion = true;
            }

            PendingLevelUpEvents.Add(levelEvent);
        }

        /// <summary>
        /// Resolves a pending Level-5 promotion into <paramref name="targetClassID"/> (one of
        /// <see cref="ClassTree.GetPromotions"/> for this character's current class). Falls
        /// back to a <see cref="PlaceholderCharacterClass"/> when the target has no concrete
        /// <see cref="CharacterClass"/> registered yet — most promotion targets don't, today —
        /// and grants a random class spell for the new class if one is registered.
        /// </summary>
        public void Promote(string targetClassID)
        {
            CharacterClass target = ClassRegistry.Get(targetClassID) ?? new PlaceholderCharacterClass(
                targetClassID,
                CharacterClass?.ClassTier ?? CharacterClass.Tier.TierTwo,
                CharacterClass?.Specialty ?? CharacterClass.ClassSpecialty.Melee);

            CharacterClass = target;
            HasPendingPromotion = false;

            ClassSpell spell = SpellRegistry.GetRandomClassSpell(targetClassID);
            if (spell != null)
                CharacterSpells.Add(spell);

            Debug.Log($"[{GetType().Name}] '{CharacterName}' promoted to '{targetClassID}'.");
        }

        /// <summary>
        /// Returns a random Realm from this character's CharacterRealms list, or Realm.Light if the list is empty.
        /// </summary>
        public Realm GetRandomRealmType()
        {
            if (CharacterRealms == null || CharacterRealms.Count == 0)
                return Realm.Light;
            int index = UnityEngine.Random.Range(0, CharacterRealms.Count);
            return CharacterRealms[index];
        }

        /// <summary>
        /// Returns a random SpellType matching one of this character's realms — for
        /// single-realm characters this is deterministic; for multi-realm characters
        /// it picks uniformly at random. Used by realm-typed spells like Magic Dart.
        /// </summary>
        public SpellType GetRandomSpellType() => RealmToSpellType(GetRandomRealmType());

        /// <summary>Converts a Realm to its matching elemental SpellType.</summary>
        public static SpellType RealmToSpellType(Realm realm)
        {
            switch (realm)
            {
                case Realm.Fire:  return SpellType.Fire;
                case Realm.Water: return SpellType.Water;
                case Realm.Earth: return SpellType.Earth;
                case Realm.Wind:  return SpellType.Wind;
                case Realm.Dark:  return SpellType.Dark;
                case Realm.Light: return SpellType.Light;
                default:          return SpellType.Physical;
            }
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