using UnityEngine;
using BaaroForce.Map;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Base class for all spells.
    ///
    /// Data fields (name, cost, range, area, cooldown, targetType) describe the spell
    /// for the UI and targeting system.  Override <see cref="Execute"/> in concrete
    /// subclasses to implement the spell's actual effects.
    /// </summary>
    public abstract class Spell
    {
        public readonly string         Name;
        public readonly string         Description;
        /// <summary>Mana required to cast.</summary>
        public readonly int            ManaCost;
        public readonly int            ActionPointCost;
        /// <summary>Maximum Manhattan-distance at which a target tile can be selected.</summary>
        public readonly int            Range;
        /// <summary>AoE radius around the chosen target tile (0 = single tile). Spells may
        /// override Execute to use their own area logic instead.</summary>
        public readonly int            Area;
        /// <summary>Turns until this spell can be used again (0 = no cooldown).</summary>
        public readonly int            Cooldown;
        /// <summary>Who the spell can be aimed at; drives highlight colour in the UI.</summary>
        public readonly SpellTargetType TargetType;
        /// <summary>The shape of the area-of-effect pattern.
        /// Only meaningful when <see cref="TargetType"/> is <see cref="SpellTargetType.AoE"/>.
        /// Used by <see cref="SpellAreaUtils"/> to resolve the affected tiles.</summary>
        public readonly SpellAreaType AreaType;

        protected Spell(string name, string description, int manaCost, int actionPointCost, int range, int area, int cooldown,
                     SpellTargetType targetType = SpellTargetType.Enemy,
                     SpellAreaType areaType = SpellAreaType.None)
        {
            this.Name        = name;
            this.Description = description;
            this.ManaCost    = manaCost;
            this.ActionPointCost = actionPointCost;
            this.Range       = range;
            this.Area        = area;
            this.Cooldown    = cooldown;
            this.TargetType  = targetType;
            this.AreaType    = areaType;
        }

        /// <summary>Convenience constructor for name/description-only data stubs.</summary>
        protected Spell(string name, string description)
        {
            this.Name        = name;
            this.Description = description;
            this.ManaCost    = 0;
            this.ActionPointCost = 1;
            this.Range       = 0;
            this.Area        = 0;
            this.Cooldown    = 999;
            this.TargetType  = SpellTargetType.Enemy;
            this.AreaType    = SpellAreaType.None;
        }

        /// <summary>
        /// Executes this spell's effects given the cast context.
        /// Returns true if the spell resolved successfully (AP and mana should be consumed).
        /// </summary>
        public abstract bool Execute(SpellContext context);

        /// <summary>
        /// Returns the tile the caster should physically move to before the
        /// spell's effect resolves, or null when no repositioning is needed.
        /// Override in spells that should reposition the caster before their effect
        /// resolves (e.g. <see cref="Charge"/>).
        /// </summary>
        public virtual MapTile GetCasterLandingTile(SpellContext context) => null;
    }
}

/// <summary>Elemental/physical category used for damage-type calculations.</summary>
public enum SpellType
{
    Fire, Water, Earth, Wind, Dark, Light, Physical
}
