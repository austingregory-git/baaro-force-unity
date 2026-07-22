using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

namespace BaaroForce.Utils
{
    /// <summary>Applies class-growth-weighted stat gains on level-up (see
    /// <see cref="BaaroForce.Characters.Character.GrantExperience"/>).</summary>
    public static class LevelUtils
    {
        /// <summary>
        /// Rolls one stat increase (health, attack, or mana) weighted by <paramref name="weights"/>
        /// for each of <paramref name="levelsGained"/> new levels, mutating and returning
        /// <paramref name="characterStats"/> in place.
        /// </summary>
        public static CharacterStats LevelUp(CharacterStats characterStats, ClassGrowthWeights weights, int levelsGained)
        {
            for (int i = 0; i < levelsGained; i++)
            {
                double roll = Random.value;
                if (roll < weights.HealthPointsGrowthWeight)
                {
                    characterStats.MaxHealthPoints++;
                    characterStats.HealthPoints++;
                }
                else if (roll < weights.HealthPointsGrowthWeight + weights.BaseAttackGrowthWeight)
                    characterStats.BaseAttack++;
                else
                {
                    characterStats.MaxMana++;
                    characterStats.Mana++;
                }
            }
            return characterStats;
        }
    }
}
