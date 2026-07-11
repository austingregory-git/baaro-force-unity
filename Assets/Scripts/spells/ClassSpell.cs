using BaaroForce.Classes;

namespace BaaroForce.Spells
{
    public class ClassSpell : Spell
    {
        public CharacterClass characterClass { get; set; }

        public ClassSpell(CharacterClass characterClass, string name, string description, int cost, int range, int area, int cooldown)
            : base(name, description, cost, range, area, cooldown)
        {
            this.characterClass = characterClass;
        }

        public ClassSpell(CharacterClass characterClass, string name, string description)
            : base(name, description)
        {
            this.characterClass = characterClass;
        }
    }
}