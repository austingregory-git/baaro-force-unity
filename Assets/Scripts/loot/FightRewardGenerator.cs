using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Loot
{
    /// <summary>Builds the loot awarded for winning a fight, scaled by how deep into the
    /// run the party is (see PartyManager.Depth).</summary>
    public static class FightRewardGenerator
    {
        public static List<LootEntry> Generate(int depth)
        {
            int d = Mathf.Max(1, depth);
            int minGold = 15 + (d - 1) * 10;
            int maxGold = 30 + (d - 1) * 15;
            int gold = Random.Range(minGold, maxGold + 1);

            return new List<LootEntry> { LootEntry.ForGold(gold) };
        }
    }
}
