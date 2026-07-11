using System;
using System.Collections.Generic;
using BaaroForce.Utils;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

public class LevelUtils : MonoBehaviour
{

    private readonly System.Random random = new System.Random();

    CharacterStats LevelUp(CharacterStats characterStats, ClassGrowthWeights weights, int newLevel)
    {
        for (int i = 0; i < newLevel; i++)
        {
            double roll = random.NextDouble();
            if (roll < weights.healthPointsGrowthWeight)
                characterStats.healthPoints++;
            else if (roll < weights.healthPointsGrowthWeight + weights.baseAttackGrowthWeight)
                characterStats.baseAttack++;
            else
                characterStats.mana++;
        }
        return characterStats;
    }
}
