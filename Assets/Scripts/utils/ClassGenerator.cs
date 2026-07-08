using System;
using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Characters;

using UnityEngine;

namespace BaaroForce.Utils
{
    public class ClassGenerator : MonoBehaviour
    {

        private readonly System.Random random = new System.Random();

        // Start is called before the first frame update
        void Start()
        {
            //testing... call this 100 times
            for (int i = 0; i < 100; i++)
            {

                int level = random.Next(0, 30);
                CharacterStats characterStats = GenerateCharacterStats(level);

                Debug.Log("Level: " + level);
                Debug.Log("HealthPoints: " + characterStats.healthPoints);
                Debug.Log("BaseAttack: " + characterStats.baseAttack);
                Debug.Log("Mana: " + characterStats.mana);
                Debug.Log("Movement: " + characterStats.movement);
            }
        }

        public CharacterStats GenerateCharacterStats(int level)
        {
            int lowerBound = (level / 2) + 4;
            int upperBound = (level / 2) + 12;

            int healthPoints = random.Next(lowerBound, upperBound);
            int baseAttack = random.Next(lowerBound, upperBound);
            int mana = random.Next(lowerBound, upperBound);
            int movement = 5;

            CharacterStats characterStats = new CharacterStats(healthPoints, baseAttack, mana, movement);
            return characterStats;
        }
    }
}


