
namespace BaaroForce.Classes
{
    public class ClassGrowthWeights
    {
        public double healthPointsGrowthWeight;
        public double baseAttackGrowthWeight;
        public double manaGrowthWeight;

        public ClassGrowthWeights(double healthPointsGrowthWeight, double baseAttackGrowthWeight, double manaGrowthWeight)
        {
            this.healthPointsGrowthWeight = healthPointsGrowthWeight;
            this.baseAttackGrowthWeight = baseAttackGrowthWeight;
            this.manaGrowthWeight = manaGrowthWeight;
        }
    }
}
