using System.Collections.Generic;

namespace BaaroForce.Classes
{
    public class DarkMage : CharacterClass
    {
        public DarkMage()
            : base("DarkMage", Tier.TIER_TWO, ClassTree.getPromotions("DarkMage"), null, new ClassGrowthWeights(0.4, 0.2, 0.4))
        {
        }
    }
}
