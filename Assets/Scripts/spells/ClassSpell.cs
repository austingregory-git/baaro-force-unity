using BaaroForce.Classes;

namespace BaaroForce.Spells
{
    public abstract class ClassSpell : Spell
    {
        public CharacterClass CharacterClass { get; set; }

        protected ClassSpell(CharacterClass characterClass, string name, string description, int manaCost, int actionPointCost, int range, int area, int cooldown,
                          SpellTargetType targetType = SpellTargetType.Enemy,
                          SpellAreaType areaType = SpellAreaType.None,
                          bool oncePerFight = false,
                          bool includeOriginTile = false)
            : base(name, description, manaCost, actionPointCost, range, area, cooldown, targetType, areaType, oncePerFight, includeOriginTile)
        {
            this.CharacterClass = characterClass;
        }

        protected ClassSpell(CharacterClass characterClass, string name, string description)
            : base(name, description)
        {
            this.CharacterClass = characterClass;
        }
    }
}