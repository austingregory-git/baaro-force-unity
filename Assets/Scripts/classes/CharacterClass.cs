using System.Collections.Generic;

namespace BaaroForce.Classes
{

    public abstract class CharacterClass
    {
        public string classID;
        public List<string> promotions;
        public List<Spell> spells;
        public ClassGrowthWeights classGrowthWeights;
        public Tier tier;
        public ClassSpecialty classSpecialty;

        protected CharacterClass(
            string classID, 
            Tier tier, 
            List<string> promotions, 
            List<Spell> spells, 
            ClassGrowthWeights classGrowthWeights, 
            ClassSpecialty classSpecialty = ClassSpecialty.MELEE)
        {
            this.classID = classID;
            this.tier = tier;
            this.promotions = promotions;
            this.spells = spells;
            this.classGrowthWeights = classGrowthWeights;
            this.classSpecialty = classSpecialty;
        }

        public enum ClassSpecialty
        {
            MELEE,
            RANGED,
            MAGIC,
        }

        public enum Tier
        {
            TIER_ONE,
            TIER_TWO,
            TIER_THREE,
            TIER_FOUR
        }
    }
}
