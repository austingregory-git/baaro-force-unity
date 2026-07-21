namespace BaaroForce.Loot
{
    /// <summary>Kind of reward a <see cref="LootEntry"/> represents.</summary>
    public enum LootType
    {
        Gold,
        Item
    }

    /// <summary>
    /// A single reward entry granted at the end of a won fight (see <see cref="FightRewardGenerator"/>).
    /// Item support has no backing inventory system yet — DisplayName/Amount are populated so the
    /// Fight Won UI has something to render once items exist, but nothing currently produces them.
    /// </summary>
    public class LootEntry
    {
        public LootType Type { get; private set; }
        public int Amount { get; private set; }
        public string DisplayName { get; private set; }

        public static LootEntry ForGold(int amount) =>
            new LootEntry { Type = LootType.Gold, Amount = amount, DisplayName = "Gold" };

        public static LootEntry ForItem(string name, int amount = 1) =>
            new LootEntry { Type = LootType.Item, Amount = amount, DisplayName = name };
    }
}
