using BaaroForce.Characters;
using BaaroForce.Items;
using System.Collections.Generic;

namespace BaaroForce.Party
{
    public class Party
    {
        public List<Character> Members;
        public int MaximumPartySize;

        /// <summary>Currency shared across the whole party, earned via spells like Mug and
        /// (eventually) loot/items.</summary>
        public int Gold { get; private set; }

        /// <summary>Potions accumulated from fight rewards and the Village shop. No in-combat
        /// "use potion" action exists yet — this is the reward-side foundation for that.</summary>
        public List<Potion> Potions { get; } = new List<Potion>();

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
    }
}