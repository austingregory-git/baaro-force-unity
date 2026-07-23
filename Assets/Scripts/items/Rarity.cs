namespace BaaroForce.Items
{
    /// <summary>Quality tier shared by equipment, potions, and relics. Ordered low to high —
    /// see <see cref="BaaroForce.Loot.FightRewardGenerator"/> for the per-act floor rarity
    /// (Act 1 floors at Common, Act 2 at Uncommon, Act 3 at Rare, Act 4 at Epic) that steps
    /// through these values.</summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
