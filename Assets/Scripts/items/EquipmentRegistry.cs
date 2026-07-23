using System;
using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Utils;

namespace BaaroForce.Items
{
    /// <summary>
    /// Central registry of equipment, grouped by <see cref="Rarity"/>. Each entry is a
    /// factory function so every draw returns a fresh instance — same convention as
    /// CharacterRegistry/NpcRegistry/SpellRegistry. This is a small seed pool (foundation
    /// for the treasure/shop/decree/anvil systems); expand freely as real item content
    /// is authored.
    /// </summary>
    public static class EquipmentRegistry
    {
        private static readonly Dictionary<Rarity, List<Func<Equipment>>> _byRarity =
            new Dictionary<Rarity, List<Func<Equipment>>>
            {
                { Rarity.Common, new List<Func<Equipment>>
                    {
                        () => new Equipment("Copper Sword", "A basic blade, better than fists.",
                            Rarity.Common, EquipmentSlotType.MainHand, attackBonus: 1, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Melee),
                        () => new Equipment("Hunting Bow", "A simple bow of green wood.",
                            Rarity.Common, EquipmentSlotType.MainHand, attackBonus: 1, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Ranged),
                        () => new Equipment("Apprentice Wand", "Hums faintly with unspent magic.",
                            Rarity.Common, EquipmentSlotType.MainHand, spellPowerBonus: 1, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Magic),
                        () => new Equipment("Leather Armor", "Boiled hide, stitched tight.",
                            Rarity.Common, EquipmentSlotType.Chest, healthBonus: 1),
                        () => new Equipment("Leather Pants", "Well-worn but sturdy.",
                            Rarity.Common, EquipmentSlotType.Legs, healthBonus: 1),
                        () => new Equipment("Cloth Hood", "Simple protection, better than nothing.",
                            Rarity.Common, EquipmentSlotType.Helmet, healthBonus: 1),
                        () => new Equipment("Wooden Buckler", "A small round shield, easy to swing.",
                            Rarity.Common, EquipmentSlotType.OffHand, healthBonus: 1),
                    } },
                { Rarity.Uncommon, new List<Func<Equipment>>
                    {
                        () => new Equipment("Steel Blade", "Folded steel, keeps an edge.",
                            Rarity.Uncommon, EquipmentSlotType.MainHand, attackBonus: 2, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Melee),
                        () => new Equipment("Mozeem Bow", "A bow of crafted by the Mozeem, strung with sinew.",
                            Rarity.Uncommon, EquipmentSlotType.MainHand, attackBonus: 2, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Ranged),
                        () => new Equipment("Steel Wand", "A wand of polished steel, etched with runes.",
                            Rarity.Uncommon, EquipmentSlotType.MainHand, spellPowerBonus: 2, manaBonus: 1, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Magic),
                        () => new Equipment("Chain Mail", "Interlocking rings turn a glancing blow.",
                            Rarity.Uncommon, EquipmentSlotType.Chest, healthBonus: 3),
                        () => new Equipment("Runed Focus", "Carved with a minor binding sigil.",
                            Rarity.Uncommon, EquipmentSlotType.MainHand, spellPowerBonus: 2, manaBonus: 2, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Magic),
                        () => new Equipment("Steel Cap", "Dented, but it's held.",
                            Rarity.Uncommon, EquipmentSlotType.Helmet, healthBonus: 2),
                        () => new Equipment("Steel Shield", "Heavy enough to stop a charge.",
                            Rarity.Uncommon, EquipmentSlotType.OffHand, healthBonus: 2),
                    } },
                { Rarity.Rare, new List<Func<Equipment>>
                    {
                        () => new Equipment("Kingsteel Blade", "Forged for the royal guard.",
                            Rarity.Rare, EquipmentSlotType.MainHand, attackBonus: 7, healthBonus: 2, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Melee),
                        () => new Equipment("Dragonscale Plate", "Scavenged from a fallen wyrm.",
                            Rarity.Rare, EquipmentSlotType.Chest, healthBonus: 12, attackBonus: 1),
                        () => new Equipment("Crown of the Fallen King", "Still bears the royal crest.",
                            Rarity.Rare, EquipmentSlotType.Helmet, healthBonus: 8, attackBonus: 2),
                        () => new Equipment("Aegis of the Wyrm", "Scaled plating fused to an old shield.",
                            Rarity.Rare, EquipmentSlotType.OffHand, healthBonus: 10),
                    } },
                { Rarity.Epic, new List<Func<Equipment>>
                    {
                        () => new Equipment("Voidsteel Greatsword", "Drinks in the light around it.",
                            Rarity.Epic, EquipmentSlotType.MainHand, attackBonus: 12, healthBonus: 3, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Melee),
                        () => new Equipment("Stormcaller Bow", "Every loosed arrow crackles faintly.",
                            Rarity.Epic, EquipmentSlotType.MainHand, attackBonus: 12, movementBonus: 1, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Ranged),
                        () => new Equipment("Archon's Scepter", "A conduit for more power than it looks like it should hold.",
                            Rarity.Epic, EquipmentSlotType.MainHand, spellPowerBonus: 10, manaBonus: 4, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Magic),
                        () => new Equipment("Wyrmplate Aegis", "Void-tempered scale, colder than the vacuum it was forged in.",
                            Rarity.Epic, EquipmentSlotType.Chest, healthBonus: 20, attackBonus: 2),
                    } },
                { Rarity.Legendary, new List<Func<Equipment>>
                    {
                        () => new Equipment("Godslayer", "It has ended things larger than gods before.",
                            Rarity.Legendary, EquipmentSlotType.MainHand, attackBonus: 20, healthBonus: 5, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Melee),
                        () => new Equipment("Skybreaker", "Nocked, it hums with a sound like distant thunder.",
                            Rarity.Legendary, EquipmentSlotType.MainHand, attackBonus: 20, movementBonus: 2, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Ranged),
                        () => new Equipment("Worldheart Codex", "Every page rewrites itself the moment you look away.",
                            Rarity.Legendary, EquipmentSlotType.MainHand, spellPowerBonus: 18, manaBonus: 6, isWeapon: true,
                            weaponClassification: CharacterClass.ClassSpecialty.Magic),
                        () => new Equipment("Crown of the First King", "The kingdom's original crown, before it was ever fallen.",
                            Rarity.Legendary, EquipmentSlotType.Helmet, healthBonus: 15, attackBonus: 4),
                    } },
            };

        // Per-rarity shown history — an item drawn here (whether via GetRandom or a
        // slot-filtered draw) won't be offered again until every other item of that
        // rarity has had a turn. Shared across GetRandom/GetRandomOfSlot so both draw
        // from the same cycle per rarity tier.
        private static readonly Dictionary<Rarity, HashSet<string>> _shownByRarity =
            new Dictionary<Rarity, HashSet<string>>();

        private static HashSet<string> ShownSet(Rarity rarity)
        {
            if (!_shownByRarity.TryGetValue(rarity, out HashSet<string> set))
                _shownByRarity[rarity] = set = new HashSet<string>();
            return set;
        }

        /// <summary>Clears the shown-item cycle for every rarity. Call at the start of a
        /// new run so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetShownHistory() => _shownByRarity.Clear();

        /// <summary>Returns a random equipment factory result of exactly <paramref name="rarity"/>.</summary>
        public static Equipment GetRandom(Rarity rarity)
        {
            List<Func<Equipment>> picked = WeightedCyclePicker.PickMany(
                _byRarity[rarity], identity: f => f().Name, weight: _ => 1f,
                count: 1, shownHistory: ShownSet(rarity));
            return picked.Count > 0 ? picked[0]() : null;
        }

        /// <summary>Returns <paramref name="count"/> random equipment factory results of
        /// <paramref name="rarity"/> whose slot matches <paramref name="slot"/> — e.g. for a
        /// Royal Decree "choose a weapon" pick — with no duplicates within the draw. Falls
        /// back to the full rarity pool if nothing matches the requested slot.</summary>
        public static List<Equipment> GetRandomOfSlot(Rarity rarity, EquipmentSlotType slot, int count = 1)
        {
            List<Func<Equipment>> pool = _byRarity[rarity];
            List<Func<Equipment>> matches = pool.FindAll(f => f().SlotType == slot);
            if (matches.Count == 0) matches = pool;

            List<Func<Equipment>> picked = WeightedCyclePicker.PickMany(
                matches, identity: f => f().Name, weight: _ => 1f,
                count: count, shownHistory: ShownSet(rarity));

            var result = new List<Equipment>(picked.Count);
            foreach (Func<Equipment> factory in picked) result.Add(factory());
            return result;
        }

        /// <summary>Registers an additional equipment factory under the given rarity.</summary>
        public static void Register(Rarity rarity, Func<Equipment> factory)
        {
            if (factory == null) return;
            if (!_byRarity.ContainsKey(rarity))
                _byRarity[rarity] = new List<Func<Equipment>>();
            _byRarity[rarity].Add(factory);
        }
    }
}
