namespace BaaroForce.Passives
{
    /// <summary>
    /// Long Bow — Hans's signature passive ability.
    /// All spells and basic attacks gain +1 range.
    /// </summary>
    public class LongBow : PassiveAbility
    {
        public LongBow()
            : base("Long Bow",
                  "All spells and attacks gain +1 range.")
        {
        }

        public override int RangeBonus => 1;
    }
}
