namespace BaaroForce.Items
{
    /// <summary>
    /// A consumable reward dropped by fights or bought at a Village shop. Plain C# class,
    /// same convention as Equipment. No in-combat "use potion" action exists yet — potions
    /// currently just accumulate in <see cref="BaaroForce.Party.Party.Potions"/> as a
    /// foundation for that system.
    /// </summary>
    public class Potion
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Rarity Rarity { get; set; }
        public int HealAmount { get; set; }

        public Potion(string name, string description, Rarity rarity, int healAmount)
        {
            Name = name;
            Description = description;
            Rarity = rarity;
            HealAmount = healAmount;
        }
    }
}
