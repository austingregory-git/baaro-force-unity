using System;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

namespace BaaroForce.Utils
{
    public class LevelUtils : MonoBehaviour
    {

        private readonly System.Random _random = new System.Random();

        CharacterStats LevelUp(CharacterStats characterStats, ClassGrowthWeights weights, int newLevel)
        {
            for (int i = 0; i < newLevel; i++)
            {
                double roll = _random.NextDouble();
                if (roll < weights.HealthPointsGrowthWeight)
                    characterStats.HealthPoints++;
                else if (roll < weights.HealthPointsGrowthWeight + weights.BaseAttackGrowthWeight)
                    characterStats.BaseAttack++;
                else
                    characterStats.Mana++;
            }
            return characterStats;
        }
    }
}
