using System;
using System.Collections.Generic;
using BaaroForce.Utils;

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
                { Rarity.Epic, new List<Func<Potion>>
                    {
                        () => new Potion("Elixir of Vigor", "Warm going down, warmer still once it settles.",
                            Rarity.Epic, healAmount: 35),
                    } },
                { Rarity.Legendary, new List<Func<Potion>>
                    {
                        () => new Potion("Phoenix Tears", "Said to have fallen from something that couldn't stay dead.",
                            Rarity.Legendary, healAmount: 60),
                    } },
            };

        // A potion drawn here won't be offered again until every other potion of that
        // rarity has had a turn.
        private static readonly Dictionary<Rarity, HashSet<string>> _shownByRarity =
            new Dictionary<Rarity, HashSet<string>>();

        private static HashSet<string> ShownSet(Rarity rarity)
        {
            if (!_shownByRarity.TryGetValue(rarity, out HashSet<string> set))
                _shownByRarity[rarity] = set = new HashSet<string>();
            return set;
        }

        /// <summary>Clears the shown-potion cycle for every rarity. Call at the start of a
        /// new run so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetShownHistory() => _shownByRarity.Clear();

        public static Potion GetRandom(Rarity rarity)
        {
            List<Func<Potion>> picked = WeightedCyclePicker.PickMany(
                _byRarity[rarity], identity: f => f().Name, weight: _ => 1f,
                count: 1, shownHistory: ShownSet(rarity));
            return picked.Count > 0 ? picked[0]() : null;
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
