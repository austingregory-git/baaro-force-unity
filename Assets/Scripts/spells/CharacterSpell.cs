namespace BaaroForce.Spells
{
    public abstract class CharacterSpell : Spell
    {
        protected CharacterSpell(string name, string description, int manaCost, int actionPointCost, int range, int area, int cooldown,
                              SpellTargetType targetType = SpellTargetType.Enemy,
                              SpellAreaType areaType = SpellAreaType.None,
                              bool oncePerFight = false,
                              SpellType? type = null)
            : base(name, description, manaCost, actionPointCost, range, area, cooldown, targetType, areaType, oncePerFight: oncePerFight, type: type)
        {
        }

        protected CharacterSpell(string name, string description)
            : base(name, description)
        {
        }
    }
}