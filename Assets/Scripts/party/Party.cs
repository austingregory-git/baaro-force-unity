using BaaroForce.Characters;
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
    }
}