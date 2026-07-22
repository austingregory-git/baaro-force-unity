using BaaroForce.Items;

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
    /// Equipment/Potion payloads are populated when <see cref="Type"/> is <see cref="LootType.Item"/>
    /// — exactly one of the two is set — so <c>TurnManager.ClaimLoot</c> can grant the right thing.
    /// </summary>
    public class LootEntry
    {
        public LootType Type { get; private set; }
        public int Amount { get; private set; }
        public string DisplayName { get; private set; }
        public Equipment Equipment { get; private set; }
        public Potion Potion { get; private set; }

        public static LootEntry ForGold(int amount) =>
            new LootEntry { Type = LootType.Gold, Amount = amount, DisplayName = "Gold" };

        public static LootEntry ForEquipment(Equipment equipment) =>
            new LootEntry { Type = LootType.Item, Amount = 1, DisplayName = equipment.Name, Equipment = equipment };

        public static LootEntry ForPotion(Potion potion) =>
            new LootEntry { Type = LootType.Item, Amount = 1, DisplayName = potion.Name, Potion = potion };
    }
}
