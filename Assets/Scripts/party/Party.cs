using BaaroForce.Characters;
using System.Collections.Generic;

namespace BaaroForce.Party
{
    public class Party
    {
        public List<Character> Members;
        public int MaximumPartySize;

        public Party(List<Character> members, int maximumPartySize)
        {
            this.Members = members;
            this.MaximumPartySize = maximumPartySize;
        }
    }
}