namespace BaaroForce.Spells
{
    public class CharacterSpell : Spell
    {
        public CharacterSpell(string name, string description, int cost, int range, int area, int cooldown)
            : base(name, description, cost, range, area, cooldown)
        {
        }
        public CharacterSpell(string name, string description)
            : base(name, description)
        {
        }
    }
}