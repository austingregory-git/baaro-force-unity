using System.Collections.Generic;
using UnityEngine;
using BaaroForce.ActMap.Encounters;

namespace BaaroForce.ActMap
{
    /// <summary>
    /// The generated Act 1 path: 16 slots covering the 22 fixed node indices from the design
    /// doc (character picks, Royal Decree, three normal fights, three "split node set" forks,
    /// an elite fight, a village, and the boss). Each fork is two branches of two sequential
    /// nodes — represented as a pair of consecutive slots, see <see cref="ActMapSlot"/> — so a
    /// fork spans 4 node indices, not 2. Structure and node ordering are fixed; only the fork
    /// branches' content types are randomized at generation time.
    /// </summary>
    public class ActMap
    {
        public List<ActMapSlot> Slots { get; } = new List<ActMapSlot>();

        /// <summary>Content types a fork branch's nodes are drawn from, per the design doc's
        /// "Other Nodes" section (Village is deliberately excluded — it's rarer and only
        /// appears at its own fixed node near the end of the act).</summary>
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

            AddFork(map, 4, 5, 6, 7);

            map.Slots.Add(new ActMapSlot(new ActMapNode(8, ActNodeType.Fight, EncounterPoolTier.Normal2, enemyLevel: 2)));

            AddFork(map, 9, 10, 11, 12);

            map.Slots.Add(new ActMapSlot(new ActMapNode(13, ActNodeType.Fight, EncounterPoolTier.Normal2, enemyLevel: 3)));

            AddFork(map, 14, 15, 16, 17);

            map.Slots.Add(new ActMapSlot(new ActMapNode(18, ActNodeType.CharacterSelect)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(19, ActNodeType.Elite, EncounterPoolTier.Elite1, enemyLevel: 4)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(20, ActNodeType.Village)));
            map.Slots.Add(new ActMapSlot(new ActMapNode(21, ActNodeType.Boss, EncounterPoolTier.Boss1, enemyLevel: 5)));

            return map;
        }

        /// <summary>
        /// Appends one "split node set" to the map — two branches (A/B) of two sequential
        /// nodes each. <paramref name="indexA1"/>/<paramref name="indexB1"/> are the first
        /// slot's two options (the real choice); <paramref name="indexA2"/>/<paramref name="indexB2"/>
        /// are the second slot's options, which auto-continue whichever branch was picked
        /// (see <see cref="ActMapSlot.IsForkContinuation"/> and
        /// <c>ActRunState.CompleteCurrentNode</c>). Content types are weighted-random,
        /// guaranteed not to repeat back-to-back within a branch (A1→A2, B1→B2) or between
        /// the two initial options (A1/B1), so the choice and each walked branch both stay varied.
        /// </summary>
        private static void AddFork(ActMap map, int indexA1, int indexB1, int indexA2, int indexB2)
        {
            ActNodeType typeA1 = PickWeightedForkType();
            ActNodeType typeB1 = PickDifferentForkType(typeA1);
            ActNodeType typeA2 = PickDifferentForkType(typeA1);
            ActNodeType typeB2 = PickDifferentForkType(typeB1);

            map.Slots.Add(new ActMapSlot(
                new ActMapNode(indexA1, typeA1),
                new ActMapNode(indexB1, typeB1)));

            map.Slots.Add(new ActMapSlot(isForkContinuation: true,
                new ActMapNode(indexA2, typeA2),
                new ActMapNode(indexB2, typeB2)));
        }

        private static ActNodeType PickDifferentForkType(ActNodeType other)
        {
            ActNodeType candidate = PickWeightedForkType();
            while (candidate == other)
                candidate = PickWeightedForkType();
            return candidate;
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
