using System;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Classes;
using BaaroForce.Characters;

namespace BaaroForce.Utils 
{
    public class CharacterUtils : MonoBehaviour
    {

        private readonly System.Random _random = new System.Random();
        private readonly NameGenerator _nameGenerator = new NameGenerator();

        // Start is called before the first frame update
        void Start()
        {
            //testing... call this 100 times
            for(int i=0; i < 100; i++) {

                int level = _random.Next(0, 30);
                CharacterStats characterStats = GenerateCharacterStats(level);

                Debug.Log("Level: " + level);
                Debug.Log("HealthPoints: " + characterStats.HealthPoints);
                Debug.Log("BaseAttack: " + characterStats.BaseAttack);
                Debug.Log("Mana: " + characterStats.Mana);
                Debug.Log("Movement: " + characterStats.Movement);
            }
        }

        public CharacterStats GenerateCharacterStats(int level)
        {
            int lowerBound = (level/2)+4;
            int upperBound = (level/2)+12;

            int healthPoints = _random.Next(lowerBound, upperBound);
            int baseAttack = _random.Next(lowerBound, upperBound);
            int mana = _random.Next(lowerBound, upperBound);
            int movement = 5;
            CharacterStats characterStats = new CharacterStats(healthPoints, baseAttack, mana, movement);
            return characterStats;
        }

        /// <summary>Characters shown in a pool since the last time every eligible
        /// character had appeared at least once. Persists across calls (and across
        /// scene loads, since it's static) so a character that just showed up in one
        /// selection screen won't show up again until everyone else has had a turn.
        /// Reset per run via <see cref="ResetShownHistory"/>.</summary>
        private static readonly HashSet<string> _shownNames = new HashSet<string>();

        /// <summary>Clears the shown-character cycle. Call at the start of a new run
        /// so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetShownHistory() => _shownNames.Clear();

        /// <summary>
        /// Returns <paramref name="numCharacters"/> randomly selected characters,
        /// weighted towards <paramref name="realm"/>.
        /// Characters whose realm matches get weight 0.5; all others get 0.1.
        /// Characters in <paramref name="exclude"/> (e.g. current party members) are
        /// never returned. Among the rest, characters already shown in a previous
        /// pool are skipped until every other eligible character has been shown too —
        /// once the cycle completes it starts over. Selection is without repeats
        /// within a single call where possible; repeats are only allowed when more
        /// characters are requested than remain eligible.
        /// </summary>
        public static List<Character> GetRandomCharacters(int numCharacters, Realm realm,
            IEnumerable<Character> exclude = null)
        {
            var excludedNames = new HashSet<string>();
            if (exclude != null)
                foreach (Character character in exclude)
                    excludedNames.Add(character.CharacterName);

            List<(Func<Character> factory, float weight, string name)> entries = BuildWeightedEntries(realm);

            List<(Func<Character> factory, float weight, string name)> picked = WeightedCyclePicker.PickMany(
                entries,
                identity: e => e.name,
                weight: e => e.weight,
                count: numCharacters,
                shownHistory: _shownNames,
                exclude: excludedNames);

            var result = new List<Character>(picked.Count);
            foreach (var entry in picked) result.Add(entry.factory());
            return result;
        }

        private static List<(Func<Character> factory, float weight, string name)> BuildWeightedEntries(Realm realm)
        {
            IReadOnlyList<Func<Character>> factories = CharacterRegistry.GetAll();
            var entries = new List<(Func<Character> factory, float weight, string name)>(factories.Count);
            foreach (Func<Character> factory in factories)
            {
                Character sample = factory();
                float weight = sample.CharacterRealms.Contains(realm) ? 0.5f : 0.1f;
                entries.Add((factory, weight, sample.CharacterName));
            }
            return entries;
        }

        // public List<Character> GenerateCharacters(int numCharacters)
        // {
        //     List<Character> characters = new List<Character>();

        //     for(int i=0; i < numCharacters; i++) {
        //         int level = _random.Next(0, 30);
        //         CharacterStats characterStats = GenerateCharacterStats(level);
        //         CharacterClass characterClass = new Mage();
        //         string characterName = _nameGenerator.GetNameByRealm(_nameGenerator.GetRandomRealm());
        //         Character character = new Character(characterClass, characterName, characterStats);
        //         characters.Add(character);
        //     }

        //     return characters;
        // }
    }

}

