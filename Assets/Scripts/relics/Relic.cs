using BaaroForce.Items;

namespace BaaroForce.Relics
{
    /// <summary>
    /// A passive run-wide reward carried in <see cref="BaaroForce.GameController.PartyManager.Relics"/>.
    /// Plain C# class (was an empty, unused MonoBehaviour stub) so it matches the rest of the
    /// data model. TurnManager.CheckAndHandleTurnStartRelics/CheckAndHandleTurnEndRelics remain
    /// no-op hooks for future relic effects — this pass only adds the data shape and a seed pool.
    /// </summary>
    public class Relic
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Rarity Rarity { get; set; }

        public Relic(string name, string description, Rarity rarity)
        {
            Name = name;
            Description = description;
            Rarity = rarity;
        }
    }
}
