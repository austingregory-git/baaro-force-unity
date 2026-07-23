using System.Collections.Generic;
using UnityEngine;
using BaaroForce.ActMap.Encounters;
using BaaroForce.GameController;
using BaaroForce.Items;

namespace BaaroForce.Loot
{
    /// <summary>
    /// Builds the loot and XP awarded for winning a fight, scaled by how deep into the run
    /// the party is (<see cref="PartyManager.Depth"/>) and which encounter pool tier the fight
    /// was pulled from.
    ///
    /// Equipment/potion rarity is built around a per-act "floor" (<see cref="FloorRarity"/>:
    /// Common in Act 1, stepping up one tier per act) rather than a fixed rarity list, so the
    /// same drop logic keeps working as later acts are added:
    /// - Normal fights always drop 1 equipment + 1 potion at the floor rarity, each with an
    ///   independent pity chance (see <see cref="RollPityRarity"/>) to come in one tier higher
    ///   instead — 10% base, +20% per Normal fight that misses, reset to 10% on a hit.
    /// - Elite fights guarantee 1 equipment + 1 potion at the floor rarity, plus 1 more of each
    ///   one tier higher.
    /// - Boss fights guarantee 1 equipment + 1 potion one tier above the floor, plus 1 more of
    ///   each two tiers above the floor (clamped to Legendary, the top of the Rarity range).
    /// </summary>
    public static class FightRewardGenerator
    {
        private const float BasePityChance     = 0.10f;
        private const float PityIncreasePerMiss = 0.20f;

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

            Rarity floor = FloorRarity(PartyManager.Instance.CurrentAct);

            switch (tier)
            {
                case EncounterPoolTier.Boss1:
                    loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(RaiseRarity(floor, 1))));
                    loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(RaiseRarity(floor, 2))));
                    loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(RaiseRarity(floor, 1))));
                    loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(RaiseRarity(floor, 2))));
                    break;

                case EncounterPoolTier.Elite1:
                    loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(floor)));
                    loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(RaiseRarity(floor, 1))));
                    loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(floor)));
                    loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(RaiseRarity(floor, 1))));
                    break;

                default: // Normal1 / Normal2
                    loot.Add(LootEntry.ForEquipment(EquipmentRegistry.GetRandom(RollPityRarity(floor, isPotion: false))));
                    loot.Add(LootEntry.ForPotion(PotionRegistry.GetRandom(RollPityRarity(floor, isPotion: true))));
                    break;
            }

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

        /// <summary>The base rarity fight rewards are built from this act — Common in Act 1,
        /// stepping up one <see cref="Rarity"/> tier per act thereafter (capped at Epic, so
        /// Act 4's "chance at Legendary" pity roll stays the top of the range).</summary>
        private static Rarity FloorRarity(int act)
        {
            switch (Mathf.Max(1, act))
            {
                case 1:  return Rarity.Common;
                case 2:  return Rarity.Uncommon;
                case 3:  return Rarity.Rare;
                default: return Rarity.Epic;
            }
        }

        /// <summary><paramref name="baseRarity"/> shifted up <paramref name="steps"/> tiers,
        /// clamped at Legendary (the top of the <see cref="Rarity"/> range).</summary>
        private static Rarity RaiseRarity(Rarity baseRarity, int steps)
        {
            int maxIndex = System.Enum.GetValues(typeof(Rarity)).Length - 1;
            return (Rarity)Mathf.Min((int)baseRarity + steps, maxIndex);
        }

        /// <summary>Rolls a Normal-fight drop's rarity against the equipment/potion pity
        /// streak: <c>floor</c> on a miss (streak +1), or <c>floor</c> raised one tier on a hit
        /// (streak reset to 0). Chance starts at <see cref="BasePityChance"/> and climbs by
        /// <see cref="PityIncreasePerMiss"/> per consecutive miss.</summary>
        private static Rarity RollPityRarity(Rarity floor, bool isPotion)
        {
            PartyManager party = PartyManager.Instance;
            int streak = isPotion ? party.PotionPityStreak : party.EquipmentPityStreak;
            float chance = Mathf.Min(1f, BasePityChance + PityIncreasePerMiss * streak);
            bool hit = Random.value < chance;

            if (isPotion)
            {
                if (hit) party.ResetPotionPityStreak(); else party.IncrementPotionPityStreak();
            }
            else
            {
                if (hit) party.ResetEquipmentPityStreak(); else party.IncrementEquipmentPityStreak();
            }

            return hit ? RaiseRarity(floor, 1) : floor;
        }
    }
}
