using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class ClassTree
    {
        public Dictionary<string, CharacterClass> classMap = new Dictionary<string, CharacterClass>();
        public List<string> tierOneClasses = new List<string>();
        public List<string> tierTwoClasses = new List<string>();
        public List<string> tierThreeClasses = new List<string>();
        public List<string> tierFourClasses = new List<string>();
        private static readonly Dictionary<string, List<string>> promotionsMap = new Dictionary<string, List<string>>();

        static ClassTree()
        {
            promotionsMap.Add("Mage", new List<string> { "DarkMage", "LightMage", "EarthMage", "FireMage", "WaterMage", "WindMage", "Scholar" });
            promotionsMap.Add("Warrior", new List<string> { "Soldier", "Samurai", "Paladin", "Thug", "Duelist", "StreetFighter", "NatureWarrior" });
            promotionsMap.Add("Cleric", new List<string> { "Deacon", "Monk", "Druid", "Paladin", "LightMage", "Scholar" });
            promotionsMap.Add("DarkMage", new List<string> { "TwilightMage", "Undead", "Demon", "LunarMage", "VoidMage" });
        }

        public ClassTree()
        {
            initialize();
        }

        public void initialize()
        {
            addTierOneClasses();
            addTierTwoClasses();
            addTierThreeClasses();
            addTierFourClasses();
        }

        private void addTierFourClasses()
        {
        }

        private void addTierThreeClasses()
        {
        }

        private void addTierTwoClasses()
        {
        }

        public void add(CharacterClass c)
        {
            classMap.Add(c.classID, c);
        }

        public void addTierOneClasses()
        {
            tierOneClasses.AddRange(new List<string> { "Mage", "Warrior", "Rogue", "Archer", "Cleric" });
        }

        public static List<string> getPromotions(string classID)
        {
            return promotionsMap[classID];
        }
    }
}

