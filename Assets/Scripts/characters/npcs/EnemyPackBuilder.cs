using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Builds an enemy pack by randomly selecting Npcs from <see cref="NpcRegistry"/>
    /// until the cumulative <see cref="Npc.StrengthIndex"/> meets the requested target.
    ///
    /// Every Npc is set to the requested level (default 1) before its StrengthIndex is
    /// read — StrengthIndex is BaseStrengthIndex * Level, so higher-level packs need a
    /// larger targetStrength budget to reach the same enemy count. No single Npc will
    /// push the total over the target; if no remaining candidate fits the leftover budget
    /// the method stops early so the result may be slightly under the target rather than over it.
    ///
    /// Example: targetStrength=2, level=1, only Wolf (BaseStrengthIndex=1) available
    ///   → two Wolf instances at Level 1 (total strength = 2).
    /// </summary>
    public static class EnemyPackBuilder
    {
        /// <summary>
        /// Returns a list of Npcs whose combined <see cref="Npc.StrengthIndex"/> equals
        /// <paramref name="targetStrength"/> as closely as possible without exceeding it, all
        /// set to <paramref name="level"/> (default 1) with a light per-level stat bump applied
        /// (+2 max HP, +1 attack per level above 1) — no general Npc stat-scaling formula
        /// exists yet, so this is a minimal foundation for the Act Map's level-pacing table.
        /// </summary>
        /// <param name="targetStrength">Desired total enemy strength for the encounter.</param>
        /// <param name="level">Level to set on every spawned Npc.</param>
        public static List<Npc> Build(int targetStrength, int level = 1)
        {
            var pack      = new List<Npc>();
            var factories = NpcRegistry.GetAll();

            if (factories.Count == 0 || targetStrength <= 0)
                return pack;

            int accumulated = 0;

            // Limit iterations to avoid an infinite loop if no Npc ever fits the remaining budget.
            int maxAttempts = targetStrength * 20;

            for (int attempt = 0; attempt < maxAttempts && accumulated < targetStrength; attempt++)
            {
                Npc candidate = factories[Random.Range(0, factories.Count)]();

                // Level must be applied before reading StrengthIndex — Npc.StrengthIndex is
                // BaseStrengthIndex * Level, so the budget check below needs the post-level
                // value (a level 4 Npc "costs" 4x its level-1 budget).
                ApplyLevel(candidate, level);

                // Skip degenerate cases where an Npc has zero or negative strength.
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

        private static void ApplyLevel(Npc npc, int level)
        {
            npc.Level = Mathf.Max(1, level);
            int bonusLevels = npc.Level - 1;
            if (bonusLevels <= 0) return;

            npc.CharacterStats.MaxHealthPoints += bonusLevels * 2;
            npc.CharacterStats.HealthPoints    += bonusLevels * 2;
            npc.CharacterStats.BaseAttack      += bonusLevels;
        }
    }
}
