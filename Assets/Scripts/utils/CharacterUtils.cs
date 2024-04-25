using System;
using System.Collections.Generic;
using UnityEngine;

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
                ClassStats classStats= GenerateClassStats(level);

                Debug.Log("Level: " + level);
                Debug.Log("Hp: " + classStats.hp);
                Debug.Log("Str: " + classStats.str);
                Debug.Log("Def: " + classStats.def);
                Debug.Log("Magic: " + classStats.magic);
                Debug.Log("Dex: " + classStats.dex);
                Debug.Log("Mana: " + classStats.mana);
            }
        }

        public ClassStats GenerateClassStats(int level)
        {
            int lowerBound = (level/2)+4;
            int upperBound = (level/2)+12;

            int hp = random.Next(lowerBound, upperBound);
            int str = random.Next(lowerBound, upperBound);
            int def = random.Next(lowerBound, upperBound);
            int magic = random.Next(lowerBound, upperBound);
            int dex = random.Next(lowerBound, upperBound);
            int mana = random.Next(lowerBound, upperBound);
            ClassStats classStats = new ClassStats(hp, str, def, magic, dex, mana);
            return classStats;
        }

        public List<Character> GenerateCharacters(int numCharacters)
        {
            List<Character> characters = new List<Character>();

            for(int i=0; i < numCharacters; i++) {
                int level = random.Next(0, 30);
                ClassStats classStats= GenerateClassStats(level);
                List<string> promotions = new List<string>();
                List<Spell> spells = new List<Spell>();
                CharacterClass characterClass = new Mage(promotions, spells, classStats);
                string characterName = nameGenerator.GetNameByRealm(nameGenerator.GetRandomRealm());
                Character character = new Character(characterClass, characterName);
                characters.Add(character);
            }

            return characters;
        }
    }

}

