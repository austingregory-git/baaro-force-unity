using System;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

namespace BaaroForce.Utils 
{
    public class CharacterUtils : MonoBehaviour
    {

        private readonly System.Random random = new System.Random();
        private readonly NameGenerator nameGenerator = new NameGenerator();

        // Start is called before the first frame update
        void Start()
        {
            //testing... call this 100 times
            for(int i=0; i < 100; i++) {

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
            int lowerBound = (level/2)+4;
            int upperBound = (level/2)+12;

            int healthPoints = random.Next(lowerBound, upperBound);
            int baseAttack = random.Next(lowerBound, upperBound);
            int mana = random.Next(lowerBound, upperBound);
            int movement = 5;
            CharacterStats characterStats = new CharacterStats(healthPoints, baseAttack, mana, movement);
            return characterStats;
        }

        // public List<Character> GenerateCharacters(int numCharacters)
        // {
        //     List<Character> characters = new List<Character>();

        //     for(int i=0; i < numCharacters; i++) {
        //         int level = random.Next(0, 30);
        //         CharacterStats characterStats = GenerateCharacterStats(level);
        //         CharacterClass characterClass = new Mage();
        //         string characterName = nameGenerator.GetNameByRealm(nameGenerator.GetRandomRealm());
        //         Character character = new Character(characterClass, characterName, characterStats);
        //         characters.Add(character);
        //     }

        //     return characters;
        // }
    }

}

