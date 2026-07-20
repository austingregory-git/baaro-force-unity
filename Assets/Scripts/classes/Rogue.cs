using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class Rogue : CharacterClass
    {
        public Rogue()
            : base(
                "Rogue", 
                Tier.TierOne, 
                ClassTree.GetPromotions("Rogue"),
                null, 
                new ClassGrowthWeights(
                    healthPointsGrowthWeight: 0.5, 
                    baseAttackGrowthWeight: 0.4, 
                    manaGrowthWeight: 0.1),
                classSpecialty: ClassSpecialty.Melee)
        {
        }
    }
}