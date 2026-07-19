using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class ClassTree
    {
        public Dictionary<string, CharacterClass> ClassMap = new Dictionary<string, CharacterClass>();
        public List<string> TierOneClasses = new List<string>();
        public List<string> TierTwoClasses = new List<string>();
        public List<string> TierThreeClasses = new List<string>();
        public List<string> TierFourClasses = new List<string>();
        private static readonly Dictionary<string, List<string>> _promotionsMap = new Dictionary<string, List<string>>();

        static ClassTree()
        {
            _promotionsMap.Add("Mage", new List<string> { "DarkMage", "LightMage", "EarthMage", "FireMage", "WaterMage", "WindMage", "Scholar" });
            _promotionsMap.Add("Warrior", new List<string> { "Soldier", "Samurai", "Paladin", "Thug", "Duelist", "StreetFighter", "NatureWarrior" });
            _promotionsMap.Add("Cleric", new List<string> { "Deacon", "Monk", "Druid", "Paladin", "LightMage", "Scholar" });
            _promotionsMap.Add("DarkMage", new List<string> { "TwilightMage", "Undead", "Demon", "LunarMage", "VoidMage" });
        }

        public ClassTree()
        {
            Initialize();
        }

        public void Initialize()
        {
            AddTierOneClasses();
            AddTierTwoClasses();
            AddTierThreeClasses();
            AddTierFourClasses();
        }

        private void AddTierFourClasses()
        {
        }

        private void AddTierThreeClasses()
        {
        }

        private void AddTierTwoClasses()
        {
        }

        public void Add(CharacterClass c)
        {
            ClassMap.Add(c.ClassID, c);
        }

        public void AddTierOneClasses()
        {
            TierOneClasses.AddRange(new List<string> { "Mage", "Warrior", "Rogue", "Archer", "Cleric" });
        }

        public static List<string> GetPromotions(string classID)
        {
            return _promotionsMap[classID];
        }
    }
}

