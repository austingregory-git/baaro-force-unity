using System;
using System.Collections.Generic;
using BaaroForce.Utils;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

public class LevelUtils : MonoBehaviour
{

    private readonly System.Random random = new System.Random();
    private readonly ClassGenerator classGenerator = new ClassGenerator();

    // Start is called before the first frame update
    void Start()
    {

        // for(int i=0; i < 100; i++) {

        //     int level = random.Next(0, 30);
        //     ClassGrowthWeights weights = new ClassGrowthWeights(0.3, 0.1, 0.6);
        //     //CharacterStats characterStats = classGenerator.GenerateCharacterStats(level);
        //     Debug.Log("Before Level --- Level: " + level + " HP: " + characterStats.healthPoints + " ATK: " + characterStats.baseAttack + " Mana: " + characterStats.mana);
        //     characterStats = LevelUp(characterStats, weights, level + 1);
        //     level++;
        //     Debug.Log("After Level  --- Level: " + level + " HP: " + characterStats.healthPoints + " ATK: " + characterStats.baseAttack + " Mana: " + characterStats.mana);
        // }
    }

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
