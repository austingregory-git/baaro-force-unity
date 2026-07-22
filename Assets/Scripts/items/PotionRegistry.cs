using System;
using System.Collections.Generic;

namespace BaaroForce.Items
{
    /// <summary>Central registry of potions, grouped by <see cref="Rarity"/>. Same
    /// factory-per-rarity convention as <see cref="EquipmentRegistry"/>.</summary>
    public static class PotionRegistry
    {
        private static readonly Dictionary<Rarity, List<Func<Potion>>> _byRarity =
            new Dictionary<Rarity, List<Func<Potion>>>
            {
                { Rarity.Common, new List<Func<Potion>>
                    {
                        () => new Potion("Minor Healing Draught", "Tastes of moss. Mends light wounds.",
                            Rarity.Common, healAmount: 5),
                    } },
                { Rarity.Uncommon, new List<Func<Potion>>
                    {
                        () => new Potion("Healing Draught", "A standard-issue field remedy.",
                            Rarity.Uncommon, healAmount: 10),
                    } },
                { Rarity.Rare, new List<Func<Potion>>
                    {
                        () => new Potion("Greater Healing Draught", "Brewed by the royal physicians.",
                            Rarity.Rare, healAmount: 20),
                    } },
            };

        public static Potion GetRandom(Rarity rarity)
        {
            List<Func<Potion>> pool = _byRarity[rarity];
            return pool[UnityEngine.Random.Range(0, pool.Count)]();
        }

        public static void Register(Rarity rarity, Func<Potion> factory)
        {
            if (factory == null) return;
            if (!_byRarity.ContainsKey(rarity))
                _byRarity[rarity] = new List<Func<Potion>>();
            _byRarity[rarity].Add(factory);
        }
    }
}
