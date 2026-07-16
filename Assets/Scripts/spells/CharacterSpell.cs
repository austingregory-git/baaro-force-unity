namespace BaaroForce.Spells
{
    public class CharacterSpell : Spell
    {
        public CharacterSpell(string name, string description, int manaCost, int actionPointCost, int range, int area, int cooldown,
                              SpellTargetType targetType = SpellTargetType.Enemy)
            : base(name, description, manaCost, actionPointCost, range, area, cooldown, targetType)
        {
        }

        public CharacterSpell(string name, string description)
            : base(name, description)
        {
        }
    }
}