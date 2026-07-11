using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Builds an enemy pack by randomly selecting NPCs from <see cref="NPCRegistry"/>
    /// until the cumulative <see cref="NPC.StrengthIndex"/> meets the requested target.
    ///
    /// All NPCs are created at Level 1 (default).  No single NPC will push the total
    /// over the target; if no remaining candidate fits the leftover budget the method
    /// stops early so the result may be slightly under the target rather than over it.
    ///
    /// Example: targetStrength=2, only Wolf (BaseStrengthIndex=1) available
    ///   → two Wolf instances at Level 1 (total strength = 2).
    /// </summary>
    public static class EnemyPackBuilder
    {
        /// <summary>
        /// Returns a list of NPCs whose combined <see cref="NPC.StrengthIndex"/> equals
        /// <paramref name="targetStrength"/> as closely as possible without exceeding it.
        /// </summary>
        /// <param name="targetStrength">Desired total enemy strength for the encounter.</param>
        public static List<NPC> Build(int targetStrength)
        {
            var pack      = new List<NPC>();
            var factories = NPCRegistry.GetAll();

            if (factories.Count == 0 || targetStrength <= 0)
                return pack;

            int accumulated = 0;

            // Limit iterations to avoid an infinite loop if no NPC ever fits the remaining budget.
            int maxAttempts = targetStrength * 20;

            for (int attempt = 0; attempt < maxAttempts && accumulated < targetStrength; attempt++)
            {
                NPC candidate = factories[Random.Range(0, factories.Count)]();

                // Skip degenerate cases where an NPC has zero or negative strength.
                if (candidate.StrengthIndex <= 0) continue;

                // Only add the candidate if it fits within the remaining budget.
                if (accumulated + candidate.StrengthIndex <= targetStrength)
                {
                    pack.Add(candidate);
                    accumulated += candidate.StrengthIndex;
                }
                // If the candidate overshoots, skip it and try again.
            }

            return pack;
        }
    }
}
