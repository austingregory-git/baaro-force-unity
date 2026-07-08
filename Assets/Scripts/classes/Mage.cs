using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class Mage : CharacterClass
    {
        public Mage()
            : base("Mage", Tier.TIER_ONE, ClassTree.getPromotions("Mage"), null, new ClassGrowthWeights(0.3, 0.1, 0.6))
        {
        }
    }
}