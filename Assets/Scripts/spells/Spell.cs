using System;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.Statuses;
using BaaroForce.UI;

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
        /// <summary>Template text for tooltips — may contain <c>{0}</c>/<c>{1}</c>
        /// placeholders filled in by <see cref="ComputeValues"/>'s totals, and
        /// <c>[Keyword]</c> tokens resolved by <see cref="BaaroForce.Keywords.KeywordRegistry"/>.</summary>
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
        /// <summary>If true, this spell can only ever be cast once per battle — <see cref="Cooldown"/> is ignored.</summary>
        public readonly bool           OncePerFight;
        /// <summary>Who the spell can be aimed at; drives highlight colour in the UI.</summary>
        public readonly SpellTargetType TargetType;
        /// <summary>The shape of the area-of-effect pattern.
        /// Only meaningful when <see cref="TargetType"/> is <see cref="SpellTargetType.AoE"/>.
        /// Used by <see cref="SpellAreaUtils"/> to resolve the affected tiles.</summary>
        public readonly SpellAreaType AreaType;
        /// <summary>
        /// Whether an origin-centred area (currently <see cref="SpellAreaType.CircleAround"/>)
        /// should include the tile it's centred on — e.g. the caster's own tile for a
        /// self-centred buff like Rally. Most enemy-targeted AoE spells leave this false so
        /// they don't hit their own tile; buffs/heals centred on the caster often want it true.
        /// </summary>
        public readonly bool IncludeOriginTile;
        /// <summary>The elemental/physical category this spell's effect belongs to, if it
        /// has one — used to colour its selectable-range/self highlight the same way
        /// <see cref="CombatTextColors.ForDamageType"/> colours its floating
        /// damage numbers. Null for spells with no single established type (Rally's attack
        /// buff, Meditate's mana gain, ...), which fall back to a TargetType-based colour
        /// instead (see TurnManager.GetSpellHighlightColor).</summary>
        public readonly SpellType? Type;

        protected Spell(string name, string description, int manaCost, int actionPointCost, int range, int area, int cooldown,
                     SpellTargetType targetType = SpellTargetType.Enemy,
                     SpellAreaType areaType = SpellAreaType.None,
                     bool oncePerFight = false,
                     bool includeOriginTile = false,
                     SpellType? type = null)
        {
            this.Name        = name;
            this.Description = description;
            this.ManaCost    = manaCost;
            this.ActionPointCost = actionPointCost;
            this.Range       = range;
            this.Area        = area;
            this.Cooldown    = cooldown;
            this.OncePerFight = oncePerFight;
            this.TargetType  = targetType;
            this.AreaType    = areaType;
            this.IncludeOriginTile = includeOriginTile;
            this.Type        = type;
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
            this.OncePerFight = false;
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

        /// <summary>
        /// Applies <paramref name="amount"/> damage to <paramref name="target"/>, shows the
        /// floating combat-text number, and — if this brings the target to 0 HP — logs the
        /// defeat and clears its tile. Every damaging spell should route its hit(s) through
        /// this instead of duplicating the TakeDamage/ShowDamage/defeat/RemoveUnit sequence
        /// inline. <paramref name="logTag"/> becomes the "[Tag]" prefix on the defeat log line —
        /// CombatLogUI keys its combat-log filtering off that exact convention, so pass the
        /// same tag the spell's own Debug.Log calls use (e.g. "Bind", "Arcane Beam").
        /// </summary>
        /// <param name="physical">True to route through <see cref="Character.TakePhysicalDamage"/>
        /// (respects Dodge) instead of <see cref="Character.TakeDamage"/>.</param>
        /// <returns>The amount actually dealt, after shield/dodge absorption.</returns>
        protected static int DealDamage(Character target, MapTile targetTile, int amount,
                                         SpellType damageType, string logTag, bool physical = false)
        {
            int dealt = physical ? target.TakePhysicalDamage(amount) : target.TakeDamage(amount);
            FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, damageType);

            if (target.CharacterStats.HealthPoints <= 0)
            {
                Debug.Log($"[{logTag}] '{target.CharacterName}' has been defeated!");
                targetTile.RemoveUnit();
            }

            return dealt;
        }

        /// <summary>
        /// Applies <paramref name="amount"/> healing to <paramref name="target"/> (clamped to
        /// its max HP by <see cref="CharacterStats.Heal"/>) and shows the floating combat-text
        /// number. The healing counterpart to <see cref="DealDamage"/> — every healing spell
        /// should route through this instead of touching <c>HealthPoints</c> directly.
        /// </summary>
        /// <returns>The amount actually restored, after clamping to MaxHealthPoints.</returns>
        protected static int ApplyHealing(Character target, MapTile targetTile, int amount, string logTag)
        {
            int healed = target.CharacterStats.Heal(amount);
            FloatingCombatTextSystem.Instance?.ShowHeal(target, healed);
            return healed;
        }

        /// <summary>
        /// Applies a <see cref="PoisonStatus"/> dealing <paramref name="poisonAmount"/> damage
        /// to <paramref name="target"/> at the start of each of its turns.
        /// </summary>
        protected static void ApplyPoison(Character target, MapTile targetTile, int poisonAmount, string logTag)
        {
            target.ApplyStatus(new PoisonStatus(poisonAmount));
        }

        /// <summary>
        /// Computes this spell's scaling numbers (damage, duration, ...) for
        /// <paramref name="caster"/>, in the same order as the <c>{0}</c>/<c>{1}</c>
        /// placeholders in <see cref="Description"/>. Override alongside Execute so the
        /// tooltip and the actual effect always agree — Execute should read its numbers
        /// from this same method rather than recomputing them. Spells with nothing to
        /// scale (rare) can leave the default empty array.
        /// </summary>
        public virtual ScalingValue[] ComputeValues(Character caster) => Array.Empty<ScalingValue>();

        /// <summary>Description with each scaling value's total substituted in — what the
        /// tooltip shows by default. Virtual so spells whose description depends on
        /// caster-specific state (e.g. Magic Dart's realm-typed damage) can build their
        /// template dynamically instead of using the fixed <see cref="Description"/> field.</summary>
        public virtual string GetSummary(Character caster) =>
            ScalingDescriptionFormatter.GetSummary(Description, ComputeValues(caster));

        /// <summary>Summary plus a full term-by-term breakdown of every scaling value —
        /// what the tooltip shows while Shift is held. Null when there's nothing to add
        /// beyond the summary.</summary>
        public virtual string GetDetailedDescription(Character caster) =>
            ScalingDescriptionFormatter.GetDetailedDescription(Description, ComputeValues(caster));

        /// <summary>
        /// Predicts what casting this spell against <paramref name="target"/> would do,
        /// without applying it — used to preview the pending action on the HUD before the
        /// player commits. Reuses <see cref="ComputeValues"/> for its numbers, same as
        /// Execute; override alongside Execute/ComputeValues so the preview never drifts
        /// from what actually happens. Spells with no meaningful target-facing effect to
        /// preview (rare) can leave the default <see cref="ActionPreview.None"/>.
        /// </summary>
        public virtual ActionPreview GetPreview(Character caster, Character target) => ActionPreview.None;

        /// <summary>The type used to colour this spell's highlight for <paramref name="caster"/>
        /// (see TurnManager.GetSpellHighlightColor). Defaults to the static <see cref="Type"/>
        /// field; override for spells whose type depends on caster state instead of being fixed
        /// at construction (e.g. Magic Dart's realm-randomised damage).</summary>
        public virtual SpellType? GetHighlightType(Character caster) => Type;
    }
}

/// <summary>Elemental/physical category used for damage-type calculations, plus Buff/Debuff
/// for non-damage spells (Rally, ...) that still want a consistent, established highlight
/// colour (see BaaroForce.UI.CombatTextColors.ForDamageType) instead of dealing typed damage.</summary>
public enum SpellType
{
    Fire, Water, Earth, Wind, Dark, Light, Physical, Magical, Buff, Debuff
}
