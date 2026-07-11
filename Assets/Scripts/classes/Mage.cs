using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class Mage : CharacterClass
    {
        public Mage()
            : base(
                "Mage", 
                Tier.TIER_ONE, ClassTree.getPromotions("Mage"), 
                null, 
                new ClassGrowthWeights(
                    healthPointsGrowthWeight: 0.3, 
                    baseAttackGrowthWeight: 0.1, 
                    manaGrowthWeight: 0.6),
                classSpecialty: ClassSpecialty.MAGIC)
        {   
        }
    }
}