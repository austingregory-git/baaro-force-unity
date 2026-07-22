using BaaroForce.GameController;
using BaaroForce.Items;
using BaaroForce.Relics;

namespace BaaroForce.ActMap.Content
{
    /// <summary>Small shared building blocks for Event/SideQuest choice effects, so each
    /// content entry's Apply delegate reads as a one-liner.</summary>
    public static class ActChoiceEffects
    {
        public static void GrantGold(PartyManager manager, int amount) => manager.Party.AddGold(amount);

        public static void GrantPartyExperience(PartyManager manager, int amount)
        {
            foreach (var member in manager.Party.Members)
                member.GrantExperience(amount);
        }

        public static void GrantRelic(PartyManager manager, Rarity rarity) =>
            manager.Relics.Add(RelicRegistry.GetRandom(rarity));

        public static void GrantPotion(PartyManager manager, Rarity rarity) =>
            manager.Party.TryAddPotion(PotionRegistry.GetRandom(rarity));

        /// <summary>Adds a random equipment roll to the party's shared inventory bag (see
        /// <see cref="BaaroForce.Party.Party.TryAddEquipment"/>) rather than force-equipping a
        /// random member — the player equips it themselves from the Inventory screen.</summary>
        public static void GrantEquipment(PartyManager manager, Rarity rarity) =>
            manager.Party.TryAddEquipment(EquipmentRegistry.GetRandom(rarity));
    }
}
