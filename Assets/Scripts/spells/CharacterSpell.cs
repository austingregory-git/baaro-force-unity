namespace BaaroForce.Spells
{
    public class CharacterSpell : Spell
    {
        public CharacterSpell(string name, string description, int cost, int range, int area, int cooldown,
                              SpellTargetType targetType = SpellTargetType.Enemy)
            : base(name, description, cost, range, area, cooldown, targetType)
        {
        }

        public CharacterSpell(string name, string description)
            : base(name, description)
        {
        }
    }
}