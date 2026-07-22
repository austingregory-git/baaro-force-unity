using BaaroForce.Characters;
using BaaroForce.Items;
using System.Collections.Generic;

namespace BaaroForce.Party
{
    public class Party
    {
        /// <summary>Shared capacity across <see cref="EquipmentBag"/> + <see cref="Potions"/>
        /// combined — Gold is unlimited and doesn't count against this.</summary>
        public const int InventoryCapacity = 32;

        public List<Character> Members;
        public int MaximumPartySize;

        /// <summary>Currency shared across the whole party, earned via spells like Mug and
        /// (eventually) loot/items.</summary>
        public int Gold { get; private set; }

        /// <summary>Potions held by the party. No in-combat "use potion" action exists yet —
        /// this is the reward-side foundation for that. Add via <see cref="TryAddPotion"/> so
        /// the shared inventory cap is respected.</summary>
        public List<Potion> Potions { get; } = new List<Potion>();

        /// <summary>Unequipped equipment held by the party — the Inventory screen's Equipment
        /// tab. Add via <see cref="TryAddEquipment"/> so the shared inventory cap is respected.</summary>
        public List<Equipment> EquipmentBag { get; } = new List<Equipment>();

        public Party(List<Character> members, int maximumPartySize)
        {
            this.Members = members;
            this.MaximumPartySize = maximumPartySize;
        }

        /// <summary>Adds gold to the party's shared purse. Non-positive amounts are ignored.</summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
        }

        /// <summary>Spends gold if the party can afford it. Returns true if the purchase succeeded.</summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        /// <summary>Combined Equipment + Potion count against <see cref="InventoryCapacity"/>.</summary>
        public int InventoryUsed => EquipmentBag.Count + Potions.Count;

        /// <summary>True if the shared inventory has room for <paramref name="count"/> more items.</summary>
        public bool HasInventorySpace(int count = 1) => InventoryUsed + count <= InventoryCapacity;

        /// <summary>Adds equipment to the bag if there's room. Returns false (item not added) if full.</summary>
        public bool TryAddEquipment(Equipment equipment)
        {
            if (equipment == null || !HasInventorySpace()) return false;
            EquipmentBag.Add(equipment);
            return true;
        }

        /// <summary>Adds a potion to the bag if there's room. Returns false (item not added) if full.</summary>
        public bool TryAddPotion(Potion potion)
        {
            if (potion == null || !HasInventorySpace()) return false;
            Potions.Add(potion);
            return true;
        }

        /// <summary>Permanently removes an item from the equipment bag. Returns true if it was present.</summary>
        public bool RemoveEquipment(Equipment equipment) => EquipmentBag.Remove(equipment);

        /// <summary>Permanently removes a potion from the bag. Returns true if it was present.</summary>
        public bool RemovePotion(Potion potion) => Potions.Remove(potion);
    }
}