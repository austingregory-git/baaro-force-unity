using System;
using System.Collections.Generic;

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
                        () => new Equipment("Worn Sword", "A basic blade, better than fists.",
                            Rarity.Common, EquipmentSlotType.Weapon, attackBonus: 2),
                        () => new Equipment("Hunting Bow", "A simple bow of green wood.",
                            Rarity.Common, EquipmentSlotType.Weapon, attackBonus: 2),
                        () => new Equipment("Apprentice Wand", "Hums faintly with unspent magic.",
                            Rarity.Common, EquipmentSlotType.Weapon, spellPowerBonus: 2),
                        () => new Equipment("Leather Armor", "Boiled hide, stitched tight.",
                            Rarity.Common, EquipmentSlotType.Armor, healthBonus: 4),
                        () => new Equipment("Traveler's Boots", "Well-worn but sturdy.",
                            Rarity.Common, EquipmentSlotType.Accessory, movementBonus: 1),
                    } },
                { Rarity.Uncommon, new List<Func<Equipment>>
                    {
                        () => new Equipment("Tempered Blade", "Folded steel, keeps an edge.",
                            Rarity.Uncommon, EquipmentSlotType.Weapon, attackBonus: 4),
                        () => new Equipment("Chain Mail", "Interlocking rings turn a glancing blow.",
                            Rarity.Uncommon, EquipmentSlotType.Armor, healthBonus: 7),
                        () => new Equipment("Runed Focus", "Carved with a minor binding sigil.",
                            Rarity.Uncommon, EquipmentSlotType.Weapon, spellPowerBonus: 4, manaBonus: 2),
                    } },
                { Rarity.Rare, new List<Func<Equipment>>
                    {
                        () => new Equipment("Kingsteel Blade", "Forged for the royal guard.",
                            Rarity.Rare, EquipmentSlotType.Weapon, attackBonus: 7, healthBonus: 2),
                        () => new Equipment("Dragonscale Plate", "Scavenged from a fallen wyrm.",
                            Rarity.Rare, EquipmentSlotType.Armor, healthBonus: 12, attackBonus: 1),
                    } },
            };

        /// <summary>Returns a random equipment factory result of exactly <paramref name="rarity"/>.</summary>
        public static Equipment GetRandom(Rarity rarity)
        {
            List<Func<Equipment>> pool = _byRarity[rarity];
            return pool[UnityEngine.Random.Range(0, pool.Count)]();
        }

        /// <summary>Returns a random equipment factory result of <paramref name="rarity"/> whose
        /// slot matches <paramref name="slot"/> — e.g. for a Royal Decree "choose a weapon" pick.
        /// Falls back to any item of that rarity if none matches the requested slot.</summary>
        public static Equipment GetRandomOfSlot(Rarity rarity, EquipmentSlotType slot)
        {
            List<Func<Equipment>> pool = _byRarity[rarity];
            List<Equipment> matches = new List<Equipment>();
            foreach (Func<Equipment> factory in pool)
            {
                Equipment candidate = factory();
                if (candidate.SlotType == slot) matches.Add(candidate);
            }
            if (matches.Count > 0)
                return matches[UnityEngine.Random.Range(0, matches.Count)];
            return GetRandom(rarity);
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
