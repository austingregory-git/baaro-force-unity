
namespace BaaroForce.Classes
{
    public class ClassGrowthWeights
    {
        public double HealthPointsGrowthWeight;
        public double BaseAttackGrowthWeight;
        public double ManaGrowthWeight;

        public ClassGrowthWeights(double healthPointsGrowthWeight, double baseAttackGrowthWeight, double manaGrowthWeight)
        {
            this.HealthPointsGrowthWeight = healthPointsGrowthWeight;
            this.BaseAttackGrowthWeight = baseAttackGrowthWeight;
            this.ManaGrowthWeight = manaGrowthWeight;
        }
    }
}
