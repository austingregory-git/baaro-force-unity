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

        /// <summary>
        /// Returns <paramref name="numCharacters"/> randomly selected characters,
        /// weighted towards <paramref name="realm"/>.
        /// Characters whose realm matches get weight 0.5; all others get 0.1.
        /// Selection is without replacement where possible; repeats are only
        /// allowed when more characters are requested than the registry holds.
        /// </summary>
        public static List<Character> GetRandomCharacters(int numCharacters, Realm realm)
        {
            var entries = BuildWeightedEntries(realm);
            var result  = new List<Character>(numCharacters);
            bool[] used = new bool[entries.Count];

            for (int pick = 0; pick < numCharacters; pick++)
            {
                ResetUsedIfExhausted(used, entries);
                int chosen = PickWeightedIndex(used, entries);
                used[chosen] = true;
                result.Add(entries[chosen].factory());
            }

            return result;
        }

        private static List<(Func<Character> factory, float weight)> BuildWeightedEntries(Realm realm)
        {
            var entries = new List<(Func<Character> factory, float weight)>(CharacterRegistry.GetAll().Count);
            foreach (Func<Character> factory in CharacterRegistry.GetAll())
            {
                float weight = factory().characterRealms.Contains(realm) ? 0.5f : 0.1f;
                entries.Add((factory, weight));
            }
            return entries;
        }

        private static void ResetUsedIfExhausted(bool[] used,
            List<(Func<Character> factory, float weight)> entries)
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                if (!used[i]) total += entries[i].weight;

            if (total <= 0f)
                for (int i = 0; i < used.Length; i++) used[i] = false;
        }

        private static int PickWeightedIndex(bool[] used,
            List<(Func<Character> factory, float weight)> entries)
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                if (!used[i]) total += entries[i].weight;

            float roll       = UnityEngine.Random.value * total;
            float cumulative = 0f;
            int   chosen     = entries.Count - 1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (used[i]) continue;
                cumulative += entries[i].weight;
                if (roll <= cumulative) { chosen = i; break; }
            }
            return chosen;
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

