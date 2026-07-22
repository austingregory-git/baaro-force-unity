using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

namespace BaaroForce.Utils
{
    /// <summary>Which stat a single level-up point was allocated to.</summary>
    public enum LevelUpStat { Health, Mana, Attack }

    /// <summary>One allocated level-up point and the flat amount it grants — Health/Mana
    /// points are worth +2 to that stat, Attack points are worth +1.</summary>
    public readonly struct StatPointGain
    {
        public readonly LevelUpStat Stat;
        public readonly int Amount;

        public StatPointGain(LevelUpStat stat, int amount)
        {
            Stat = stat;
            Amount = amount;
        }
    }

    /// <summary>Applies class-growth-weighted stat gains on level-up (see
    /// <see cref="BaaroForce.Characters.Character.GrantExperience"/>).</summary>
    public static class LevelUtils
    {
        /// <summary>Level-up points granted per level — each rolled independently against
        /// the character's class growth weights.</summary>
        public const int PointsPerLevel = 2;

        /// <summary>
        /// Rolls <see cref="PointsPerLevel"/> points against <paramref name="weights"/>,
        /// applying each immediately to <paramref name="characterStats"/> (Health/Mana +2,
        /// Attack +1), and returns the ordered list of gains so a reveal UI can replay them.
        /// </summary>
        public static List<StatPointGain> RollAndApply(CharacterStats characterStats, ClassGrowthWeights weights)
        {
            var gains = new List<StatPointGain>(PointsPerLevel);
            for (int i = 0; i < PointsPerLevel; i++)
            {
                double roll = Random.value;
                StatPointGain gain;
                if (roll < weights.HealthPointsGrowthWeight)
                    gain = new StatPointGain(LevelUpStat.Health, 2);
                else if (roll < weights.HealthPointsGrowthWeight + weights.BaseAttackGrowthWeight)
                    gain = new StatPointGain(LevelUpStat.Attack, 1);
                else
                    gain = new StatPointGain(LevelUpStat.Mana, 2);

                Apply(characterStats, gain);
                gains.Add(gain);
            }
            return gains;
        }

        private static void Apply(CharacterStats characterStats, StatPointGain gain)
        {
            switch (gain.Stat)
            {
                case LevelUpStat.Health:
                    characterStats.MaxHealthPoints += gain.Amount;
                    characterStats.HealthPoints    += gain.Amount;
                    break;
                case LevelUpStat.Mana:
                    characterStats.MaxMana += gain.Amount;
                    characterStats.Mana    += gain.Amount;
                    break;
                case LevelUpStat.Attack:
                    characterStats.BaseAttack += gain.Amount;
                    break;
            }
        }
    }
}
