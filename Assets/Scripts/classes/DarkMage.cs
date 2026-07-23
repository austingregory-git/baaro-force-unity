using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class DarkMage : CharacterClass
    {
        public DarkMage()
            : base(
                "Dark Mage", 
                Tier.TierTwo, 
                ClassTree.GetPromotions("Dark Mage"),
                null, 
                new ClassGrowthWeights(
                    healthPointsGrowthWeight: 0.4,
                    baseAttackGrowthWeight: 0.2,
                    manaGrowthWeight: 0.4),
                classSpecialty: ClassSpecialty.Magic)
        {
        }
    }
}
