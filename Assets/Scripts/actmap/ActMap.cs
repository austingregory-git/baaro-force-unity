using System.Collections.Generic;
using UnityEngine;
using BaaroForce.ActMap.Encounters;

namespace BaaroForce.ActMap
{
    /// <summary>
    /// The generated Act 1 path: 13 slots covering the 16 fixed node indices from the design
    /// doc (character picks, Royal Decree, three normal fights, three "split node set" forks,
    /// an elite fight, a village, and the boss). Structure and node ordering are fixed; only
    /// the three fork slots' content types are randomized at generation time.
    /// </summary>
    public class ActMap
    {
        public List<ActMapSlot> Slots { get; } = new List<ActMapSlot>();

        /// <summary>Content types a fork slot's two options are drawn from, per the design
        /// doc's "Other Nodes" section (Village is deliberately excluded — it's rarer and
        /// only appears at its own fixed node 14).</summary>
        private static readonly (ActNodeType type, float weight)[] ForkPool =
        {
            (ActNodeType.Event, 0.30f),
            (ActNodeType.SideQuest, 0.30f),
            (ActNodeType.Treasure, 0.25f),
            (ActNodeType.Anvil, 0.15f),
        };

        public static ActMap GenerateAct1()
        {
            var map = new ActMap();

            map.Slots.Add(new ActMapSlot(new ActMapNode(0, ActNodeType.CharacterSelect)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(1, ActNodeType.RoyalDecree)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(2, ActNodeType.CharacterSelect)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(3, ActNodeType.Fight, EncounterPoolTier.Normal1, enemyLevel: 1)));

            map.Slots.Add(BuildForkSlot(4, 5));

            map.Slots.Add(new ActMapSlot(new ActMapNode(6, ActNodeType.Fight, EncounterPoolTier.Normal2, enemyLevel: 2)));

            map.Slots.Add(BuildForkSlot(7, 8));

            map.Slots.Add(new ActMapSlot(new ActMapNode(9, ActNodeType.Fight, EncounterPoolTier.Normal2, enemyLevel: 3)));

            map.Slots.Add(BuildForkSlot(10, 11));

            map.Slots.Add(new ActMapSlot(new ActMapNode(12, ActNodeType.CharacterSelect)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(13, ActNodeType.Elite, EncounterPoolTier.Elite1, enemyLevel: 4)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(14, ActNodeType.Village)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(15, ActNodeType.Boss, EncounterPoolTier.Boss1, enemyLevel: 5)));

            return map;
        }

        /// <summary>Builds one fork slot: two nodes at <paramref name="indexA"/>/<paramref name="indexB"/>,
        /// weighted-random content types, guaranteed not to match each other (the design
        /// doc's "cannot be the same node type back-to-back" constraint for these pairs).</summary>
        private static ActMapSlot BuildForkSlot(int indexA, int indexB)
        {
            ActNodeType typeA = PickWeightedForkType();
            ActNodeType typeB = PickWeightedForkType();
            while (typeB == typeA)
                typeB = PickWeightedForkType();

            return new ActMapSlot(
                new ActMapNode(indexA, typeA),
                new ActMapNode(indexB, typeB));
        }

        private static ActNodeType PickWeightedForkType()
        {
            float total = 0f;
            foreach (var (_, weight) in ForkPool) total += weight;

            float roll = Random.value * total;
            float cumulative = 0f;
            foreach (var (type, weight) in ForkPool)
            {
                cumulative += weight;
                if (roll <= cumulative) return type;
            }
            return ForkPool[ForkPool.Length - 1].type;
        }
    }
}
