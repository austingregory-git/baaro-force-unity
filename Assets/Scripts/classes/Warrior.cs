using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class Warrior : CharacterClass
    {
        public Warrior()
            : base("Warrior", Tier.TIER_ONE, ClassTree.getPromotions("Warrior"), null, new ClassGrowthWeights(
                healthPointsGrowthWeight: 0.5, 
                baseAttackGrowthWeight: 0.4, 
                manaGrowthWeight: 0.1))
        {
        }
    }
}