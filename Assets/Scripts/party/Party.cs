using BaaroForce.Characters;
using System.Collections.Generic;

namespace BaaroForce.Party
{
    public class Party
    {
        public List<Character> members;
        public int maximumPartySize;

        public Party(List<Character> members, int maximumPartySize)
        {
            this.members = members;
            this.maximumPartySize = maximumPartySize;
        }
    }
}