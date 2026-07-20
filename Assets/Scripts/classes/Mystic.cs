using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class Mystic : CharacterClass
    {
        public Mystic()
            : base(
                "Mystic", 
                Tier.TierOne, 
                ClassTree.GetPromotions("Mystic"),
                null, 
                new ClassGrowthWeights(
                    healthPointsGrowthWeight: 0.4, 
                    baseAttackGrowthWeight: 0.25, 
                    manaGrowthWeight: 0.35),
                classSpecialty: ClassSpecialty.Melee)
        {
        }
    }
}