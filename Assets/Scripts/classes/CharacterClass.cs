using System.Collections.Generic;
using BaaroForce.Spells;

namespace BaaroForce.Classes
{

    public abstract class CharacterClass
    {
        public string ClassID;
        public List<string> Promotions;
        public List<Spell> Spells;
        public ClassGrowthWeights ClassGrowthWeights;
        public Tier ClassTier;
        public ClassSpecialty Specialty;

        protected CharacterClass(
            string classID,
            Tier tier,
            List<string> promotions,
            List<Spell> spells,
            ClassGrowthWeights classGrowthWeights,
            ClassSpecialty classSpecialty = ClassSpecialty.Melee)
        {
            this.ClassID = classID;
            this.ClassTier = tier;
            this.Promotions = promotions;
            this.Spells = spells;
            this.ClassGrowthWeights = classGrowthWeights;
            this.Specialty = classSpecialty;
        }

        public enum ClassSpecialty
        {
            Melee,
            Ranged,
            Magic,
        }

        public enum Tier
        {
            TierOne,
            TierTwo,
            TierThree,
            TierFour
        }
    }
}
