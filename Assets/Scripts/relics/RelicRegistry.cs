using System;
using System.Collections.Generic;
using BaaroForce.Items;
using BaaroForce.Utils;

namespace BaaroForce.Relics
{
    /// <summary>Central registry of relics, grouped by <see cref="Rarity"/>. Same
    /// factory-per-rarity convention as EquipmentRegistry/PotionRegistry.</summary>
    public static class RelicRegistry
    {
        private static readonly Dictionary<Rarity, List<Func<Relic>>> _byRarity =
            new Dictionary<Rarity, List<Func<Relic>>>
            {
                { Rarity.Common, new List<Func<Relic>>
                    {
                        () => new Relic("Lucky Coin", "A worn coin from some forgotten kingdom.", Rarity.Common),
                        () => new Relic("Cracked Whetstone", "Still sharpens a blade well enough.", Rarity.Common),
                        () => new Relic("Pressed Wildflower", "Kept for luck, or just sentiment.", Rarity.Common),
                    } },
                { Rarity.Uncommon, new List<Func<Relic>>
                    {
                        () => new Relic("Traveler's Compass", "Always points toward opportunity.", Rarity.Uncommon),
                    } },
                { Rarity.Rare, new List<Func<Relic>>
                    {
                        () => new Relic("Shard of the First King", "Hums with old, royal authority.", Rarity.Rare),
                    } },
            };

        // A relic drawn here won't be offered again until every other relic of that
        // rarity has had a turn.
        private static readonly Dictionary<Rarity, HashSet<string>> _shownByRarity =
            new Dictionary<Rarity, HashSet<string>>();

        private static HashSet<string> ShownSet(Rarity rarity)
        {
            if (!_shownByRarity.TryGetValue(rarity, out HashSet<string> set))
                _shownByRarity[rarity] = set = new HashSet<string>();
            return set;
        }

        /// <summary>Clears the shown-relic cycle for every rarity. Call at the start of a
        /// new run so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetShownHistory() => _shownByRarity.Clear();

        public static Relic GetRandom(Rarity rarity)
        {
            List<Func<Relic>> picked = WeightedCyclePicker.PickMany(
                _byRarity[rarity], identity: f => f().Name, weight: _ => 1f,
                count: 1, shownHistory: ShownSet(rarity));
            return picked.Count > 0 ? picked[0]() : null;
        }

        public static void Register(Rarity rarity, Func<Relic> factory)
        {
            if (factory == null) return;
            if (!_byRarity.ContainsKey(rarity))
                _byRarity[rarity] = new List<Func<Relic>>();
            _byRarity[rarity].Add(factory);
        }
    }
}
