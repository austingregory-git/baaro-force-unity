using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.ActMap.Content
{
    /// <summary>The 5 possible Royal Decree rewards from the design doc — 3 are shown at
    /// random when the Royal Decree node (index 1) is resolved.</summary>
    public enum RoyalDecreeOptionType
    {
        /// <summary>Choose a common weapon for a party member, from 3 random.</summary>
        ChooseWeapon,
        /// <summary>Gain a random common relic.</summary>
        RandomRelic,
        /// <summary>Gain 100 gold.</summary>
        Gold,
        /// <summary>Gain 2 random pieces of common equipment.</summary>
        TwoCommonEquipment,
        /// <summary>Learn a new tier-1 class spell for a party member, from 3 random.</summary>
        LearnTierOneSpell
    }

    public static class RoyalDecree
    {
        private static readonly RoyalDecreeOptionType[] AllOptions =
            (RoyalDecreeOptionType[])System.Enum.GetValues(typeof(RoyalDecreeOptionType));

        /// <summary>Returns 3 of the 5 options, chosen at random without repeats.</summary>
        public static List<RoyalDecreeOptionType> GetRandomThree()
        {
            var pool = new List<RoyalDecreeOptionType>(AllOptions);
            var result = new List<RoyalDecreeOptionType>(3);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }
            return result;
        }

        public static string GetLabel(RoyalDecreeOptionType type)
        {
            switch (type)
            {
                case RoyalDecreeOptionType.ChooseWeapon:         return "Choose a Weapon";
                case RoyalDecreeOptionType.RandomRelic:          return "A Common Relic";
                case RoyalDecreeOptionType.Gold:                 return "100 Gold";
                case RoyalDecreeOptionType.TwoCommonEquipment:   return "2 Pieces of Equipment";
                case RoyalDecreeOptionType.LearnTierOneSpell:    return "Learn a New Spell";
                default:                                         return type.ToString();
            }
        }

        public static string GetDescription(RoyalDecreeOptionType type)
        {
            switch (type)
            {
                case RoyalDecreeOptionType.ChooseWeapon:
                    return "The king's armory opens to you. Choose one of three common weapons for a party member.";
                case RoyalDecreeOptionType.RandomRelic:
                    return "Receive a common relic from the royal treasury.";
                case RoyalDecreeOptionType.Gold:
                    return "The king grants your party 100 gold to fund the journey ahead.";
                case RoyalDecreeOptionType.TwoCommonEquipment:
                    return "Receive two random pieces of common equipment from the quartermaster.";
                case RoyalDecreeOptionType.LearnTierOneSpell:
                    return "A royal tutor offers to teach a party member one of three tier-1 class spells.";
                default:
                    return string.Empty;
            }
        }
    }
}
