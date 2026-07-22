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
            manager.Party.Potions.Add(PotionRegistry.GetRandom(rarity));

        public static void GrantEquipmentToRandomMember(PartyManager manager, Rarity rarity)
        {
            if (manager.Party.Members.Count == 0) return;
            var member = manager.Party.Members[UnityEngine.Random.Range(0, manager.Party.Members.Count)];
            member.AddEquipment(EquipmentRegistry.GetRandom(rarity));
        }
    }
}
