namespace BaaroForce.Items
{
    /// <summary>
    /// A single piece of equipment (weapon, armor, or accessory) that can be granted to a
    /// character via <see cref="BaaroForce.Characters.Character.AddEquipment"/>. Plain C#
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

        public int HealthBonus { get; set; }
        public int AttackBonus { get; set; }
        public int SpellPowerBonus { get; set; }
        public int ManaBonus { get; set; }
        public int MovementBonus { get; set; }

        public Equipment(string name, string description, Rarity rarity, EquipmentSlotType slotType,
            int healthBonus = 0, int attackBonus = 0, int spellPowerBonus = 0,
            int manaBonus = 0, int movementBonus = 0)
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
                movementBonus: Bump(MovementBonus))
            { IsUpgraded = true };
        }

        private static int Bump(int value) => value <= 0 ? 0 : value + System.Math.Max(1, value / 2);
    }
}
