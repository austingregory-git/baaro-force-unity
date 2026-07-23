using System;
using System.Collections.Generic;

namespace BaaroForce.Utils
{
    /// <summary>
    /// Shared "offer N options from a fixed named pool" picking algorithm, used by every
    /// registry that hands the player a random draw from authored content (characters,
    /// equipment, potions, relics, spells, events, side quests, encounters, decrees).
    /// Rules: entries in <c>exclude</c> are never returned. Among the rest, entries already
    /// present in <c>shownHistory</c> are skipped until every other eligible entry has been
    /// shown too — once that cycle completes, <c>shownHistory</c> is cleared and a fresh
    /// cycle starts. Only when the eligible pool itself is smaller than the number requested
    /// are repeats allowed within a single draw.
    /// </summary>
    public static class WeightedCyclePicker
    {
        public static List<T> PickMany<T>(
            IReadOnlyList<T> pool,
            Func<T, string> identity,
            Func<T, float> weight,
            int count,
            HashSet<string> shownHistory,
            HashSet<string> exclude = null)
        {
            var result = new List<T>(count);
            if (pool.Count == 0) return result;

            bool[] excluded = new bool[pool.Count];
            bool[] used     = new bool[pool.Count];
            bool[] shown    = new bool[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                string name = identity(pool[i]);
                excluded[i] = exclude != null && exclude.Contains(name);
                shown[i]    = shownHistory.Contains(name);
            }

            for (int pick = 0; pick < count; pick++)
            {
                int chosen = PickNext(pool, weight, excluded, used, shown, shownHistory, identity);
                if (chosen < 0) break; // nothing eligible at all (pool fully excluded)

                used[chosen]  = true;
                shown[chosen] = true;
                shownHistory.Add(identity(pool[chosen]));
                result.Add(pool[chosen]);
            }

            return result;
        }

        /// <summary>Convenience wrapper for the common "pick exactly one" case. Returns
        /// <c>default</c> when nothing is eligible.</summary>
        public static T PickOne<T>(
            IReadOnlyList<T> pool,
            Func<T, string> identity,
            Func<T, float> weight,
            HashSet<string> shownHistory,
            HashSet<string> exclude = null)
        {
            List<T> picked = PickMany(pool, identity, weight, 1, shownHistory, exclude);
            return picked.Count > 0 ? picked[0] : default;
        }

        private static int PickNext<T>(IReadOnlyList<T> pool, Func<T, float> weight,
            bool[] excluded, bool[] used, bool[] shown,
            HashSet<string> shownHistory, Func<T, string> identity)
        {
            int chosen = PickWeightedIndex(pool, weight, i => !excluded[i] && !used[i] && !shown[i]);
            if (chosen >= 0) return chosen;

            chosen = PickWeightedIndex(pool, weight, i => !excluded[i] && !used[i]);
            if (chosen >= 0)
            {
                // Every remaining eligible entry has been shown before — the cycle
                // completed, so start a fresh one.
                shownHistory.Clear();
                for (int i = 0; i < shown.Length; i++) shown[i] = false;
                return chosen;
            }

            // More entries requested than remain eligible — allow repeats within this
            // draw (but never among permanently excluded entries).
            for (int i = 0; i < used.Length; i++)
                if (!excluded[i]) used[i] = false;
            return PickWeightedIndex(pool, weight, i => !excluded[i]);
        }

        private static int PickWeightedIndex<T>(IReadOnlyList<T> pool, Func<T, float> weight,
            Func<int, bool> eligible)
        {
            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
                if (eligible(i)) total += weight(pool[i]);

            if (total <= 0f) return -1;

            float roll       = UnityEngine.Random.value * total;
            float cumulative = 0f;
            int   chosen     = -1;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!eligible(i)) continue;
                cumulative += weight(pool[i]);
                chosen = i;
                if (roll <= cumulative) break;
            }
            return chosen;
        }
    }
}
