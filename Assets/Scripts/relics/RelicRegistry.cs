using System;
using System.Collections.Generic;
using BaaroForce.Items;

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

        public static Relic GetRandom(Rarity rarity)
        {
            List<Func<Relic>> pool = _byRarity[rarity];
            return pool[UnityEngine.Random.Range(0, pool.Count)]();
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
