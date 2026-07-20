using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class Archer : CharacterClass
    {
        public Archer()
            : base(
                "Archer", 
                Tier.TierOne, 
                ClassTree.GetPromotions("Archer"),
                null, 
                new ClassGrowthWeights(
                    healthPointsGrowthWeight: 0.4,
                    baseAttackGrowthWeight: 0.5, 
                    manaGrowthWeight: 0.1),
                classSpecialty: ClassSpecialty.Ranged)
        {
        }
    }
}