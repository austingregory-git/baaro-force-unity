using System.Collections.Generic;
using UnityEngine;
using BaaroForce.ActMap.Encounters;
using BaaroForce.Items;

namespace BaaroForce.Loot
{
    /// <summary>Builds the loot and XP awarded for winning a fight, scaled by how deep into
    /// the run the party is (<see cref="PartyManager.Depth"/>) and which encounter pool tier
    /// the fight was pulled from — see the Act 1 design doc's "Fight Pools" section for the
    /// exact reward shape per tier.</summary>
    public static class FightRewardGenerator
    {
        public static List<LootEntry> Generate(int depth, EncounterPoolTier tier)
        {
            int d = Mathf.Max(1, depth);
            int minGold = 15 + (d - 1) * 10;
            int maxGold = 30 + (d - 1) * 15;
            float goldMultiplier = 1f;
            if (tier == EncounterPoolTier.Boss1) goldMultiplier = 3f;
            else if (tier == EncounterPoolTier.Elite1) goldMultiplier = 2f;
            int gold = Mathf.RoundToInt(Random.Range(minGold, maxGold + 1) * goldMultiplier);

            var loot = new List<LootEntry> { LootEntry.ForGold(gold) };

            (int minEquip, int maxEquip, int minPotion, int maxPotion, float uncommonChance,
                bool guaranteedRareEquip, bool guaranteedRarePotion) = RewardShape(tier);

            int equipCount = Random.Range(minEquip, maxEquip + 1);
            for (int i = 0; i < equipCount; i++)
                loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(RollRarity(uncommonChance))));
            if (guaranteedRareEquip)
                loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(Rarity.Rare)));

            int potionCount = Random.Range(minPotion, maxPotion + 1);
            for (int i = 0; i < potionCount; i++)
                loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(RollRarity(uncommonChance))));
            if (guaranteedRarePotion)
                loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(Rarity.Rare)));

            return loot;
        }

        /// <summary>XP granted to every living party member for winning a fight of this tier —
        /// 10/15/20 for Normal/Elite/Boss per the design doc's level-pacing table.</summary>
        public static int GetExperience(EncounterPoolTier tier)
        {
            switch (tier)
            {
                case EncounterPoolTier.Elite1: return 15;
                case EncounterPoolTier.Boss1:  return 20;
                default:                       return 10;
            }
        }

        private static Rarity RollRarity(float uncommonChance) =>
            Random.value < uncommonChance ? Rarity.Uncommon : Rarity.Common;

        private static (int minEquip, int maxEquip, int minPotion, int maxPotion, float uncommonChance,
            bool guaranteedRareEquip, bool guaranteedRarePotion) RewardShape(EncounterPoolTier tier)
        {
            switch (tier)
            {
                case EncounterPoolTier.Elite1:
                    return (1, 3, 1, 2, 0.25f, false, false);
                case EncounterPoolTier.Boss1:
                    return (1, 2, 1, 2, 0.5f, true, true);
                default: // Normal1 / Normal2
                    return (1, 2, 0, 1, 0f, false, false);
            }
        }
    }
}
