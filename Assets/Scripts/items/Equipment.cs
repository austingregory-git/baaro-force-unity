using System.Collections.Generic;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Items
{
    /// <summary>
    /// A single piece of equipment (helmet, chest, legs, main-hand, or off-hand) that can be
    /// granted to a character via <see cref="BaaroForce.Characters.Character.Equip"/>. Plain C#
    /// class, matching the rest of the data model (Character, Spell, CharacterClass) rather
    /// than a MonoBehaviour/ScriptableObject.
    /// </summary>
    public class Equipment
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Rarity Rarity { get; set; }
        public EquipmentSlotType SlotType { get; set; }

        /// <summary>True once this item has been upgraded at an Anvil/Smith (its "+" variant).</summary>
        public bool IsUpgraded { get; set; }

        /// <summary>True for weapons — always true for MainHand items in practice, sometimes
        /// true for OffHand items (a second blade), false for OffHand shields/tomes and every
        /// other slot.</summary>
        public bool IsWeapon { get; set; }

        public int HealthBonus { get; set; }
        public int AttackBonus { get; set; }
        public int SpellPowerBonus { get; set; }
        public int ManaBonus { get; set; }
        public int MovementBonus { get; set; }

        public List<Spell> GrantedSpells { get; set; } = new List<Spell>();
        public List<PassiveAbility> GrantedPassives { get; set; } = new List<PassiveAbility>();

        public Equipment(string name, string description, Rarity rarity, EquipmentSlotType slotType,
            int healthBonus = 0, int attackBonus = 0, int spellPowerBonus = 0,
            int manaBonus = 0, int movementBonus = 0, bool isWeapon = false)
        {
            Name = name;
            Description = description;
            Rarity = rarity;
            SlotType = slotType;
            HealthBonus = healthBonus;
            AttackBonus = attackBonus;
            SpellPowerBonus = spellPowerBonus;
            ManaBonus = manaBonus;
            MovementBonus = movementBonus;
            IsWeapon = isWeapon;
        }

        /// <summary>
        /// Returns this item's "+" variant (e.g. "Leather Armor" -> "Leather Armor +") with
        /// its bonuses bumped by roughly half their original value (minimum +1 on whichever
        /// stat this item actually grants). Does not mutate this instance — callers should
        /// replace their reference with the returned upgraded copy.
        /// </summary>
        public Equipment Upgrade()
        {
            return new Equipment(
                IsUpgraded ? Name : Name + " +",
                Description,
                Rarity,
                SlotType,
                healthBonus: Bump(HealthBonus),
                attackBonus: Bump(AttackBonus),
                spellPowerBonus: Bump(SpellPowerBonus),
                manaBonus: Bump(ManaBonus),
                movementBonus: Bump(MovementBonus),
                isWeapon: IsWeapon)
            { IsUpgraded = true };
        }

        private static int Bump(int value) => value <= 0 ? 0 : value + System.Math.Max(1, value / 2);
    }
}
