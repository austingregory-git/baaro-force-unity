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
    public class Spell
    {
        public string         name;
        public string         description;
        /// <summary>Mana required to cast.</summary>
        public int            cost;
        /// <summary>Maximum Manhattan-distance at which a target tile can be selected.</summary>
        public int            range;
        /// <summary>AoE radius around the chosen target tile (0 = single tile). Spells may
        /// override Execute to use their own area logic instead.</summary>
        public int            area;
        /// <summary>Turns until this spell can be used again (0 = no cooldown).</summary>
        public int            cooldown;
        /// <summary>Who the spell can be aimed at; drives highlight colour in the UI.</summary>
        public SpellTargetType targetType;

        public Spell(string name, string description, int cost, int range, int area, int cooldown,
                     SpellTargetType targetType = SpellTargetType.Enemy)
        {
            this.name        = name;
            this.description = description;
            this.cost        = cost;
            this.range       = range;
            this.area        = area;
            this.cooldown    = cooldown;
            this.targetType  = targetType;
        }

        /// <summary>Convenience constructor for name/description-only data stubs.</summary>
        public Spell(string name, string description)
        {
            this.name        = name;
            this.description = description;
            this.cost        = 0;
            this.range       = 0;
            this.area        = 0;
            this.cooldown    = 999;
            this.targetType  = SpellTargetType.Enemy;
        }

        /// <summary>
        /// Executes this spell's effects given the cast context.
        /// Override in concrete subclasses; the default is a no-op stub.
        /// Returns true if the spell resolved successfully (AP and mana should be consumed).
        /// </summary>
        public virtual bool Execute(SpellContext context)
        {
            Debug.LogWarning($"[Spell] '{name}' has no Execute implementation.");
            return false;
        }

        /// <summary>
        /// Executes this spell's effects when cast by an NPC.
        /// Override in subclasses that should be usable by NPC casters.
        /// Returns true if the spell resolved successfully.
        /// </summary>
        public virtual bool Execute(NpcSpellContext context)
        {
            Debug.LogWarning($"[Spell] '{name}' has no NPC Execute implementation.");
            return false;
        }

        /// <summary>
        /// Returns the tile the caster should physically move to before the
        /// spell's effect resolves, or null when no repositioning is needed.
        /// Override in spells that close distance as part of their effect
        /// (e.g. <see cref="Charge"/>).
        /// </summary>
        public virtual MapTile GetCasterLandingTile(SpellContext context) => null;
    }
}

/// <summary>Elemental/physical category used for damage-type calculations.</summary>
public enum SpellType
{
    FIRE, WATER, EARTH, WIND, DARK, LIGHT, PHYSICAL
}
