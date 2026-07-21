using UnityEngine;
using BaaroForce;
using BaaroForce.Characters;
using BaaroForce.Statuses;

namespace BaaroForce.UI
{
    /// <summary>
    /// Colour palette for floating combat text (see <see cref="FloatingCombatTextSystem"/>).
    ///
    /// Damage-type colours intentionally reuse the exact realm background palette from the
    /// character-selection screen (<see cref="CharacterSelectionManager.GetRealmBackgroundColor"/>)
    /// so the two screens read as one consistent colour language. Physical damage has no
    /// matching realm, so it reuses the AoE spell-preview highlight orange instead (see
    /// TurnManager.UpdateSpellPreview / GetSpellHighlightColor).
    /// </summary>
    public static class CombatTextColors
    {
        /// <summary>Matches the AoE targeting-preview highlight colour (TurnManager.UpdateSpellPreview).</summary>
        private static readonly Color PhysicalColor = new Color(1f, 0.5f, 0f);

        /// <summary>Matches the Magical keyword's colour (KeywordRegistry) — arcane damage
        /// with no elemental realm, e.g. Arcane Beam.</summary>
        private static readonly Color MagicalColor = new Color(0.65f, 0.35f, 0.95f);

        private static readonly Color BuffColor   = new Color(0.47f, 0.78f, 0.55f); // green
        private static readonly Color DebuffColor = new Color(0.78f, 0.63f, 0.90f); // light purple
        private static readonly Color NeutralColor = new Color(0.95f, 0.90f, 0.78f); // cream, for Custom effects

        /// <summary>Colour for health-gain numbers (Grit, regen, etc.) — a paler green than
        /// <see cref="BuffColor"/> so the two read as related but distinct signals.</summary>
        public static readonly Color HealColor = new Color(0.65f, 0.92f, 0.65f); // light green

        /// <summary>Colour for gold-gain numbers (Mug, loot, etc.).</summary>
        public static readonly Color GoldColor = new Color(0.95f, 0.80f, 0.25f); // gold/yellow
        public static readonly Color ManaColor = new Color(0.25f, 0.50f, 0.95f); // blue

        /// <summary>Colour for a damage number of the given elemental/physical type.</summary>
        public static Color ForDamageType(SpellType type)
        {
            if (type == SpellType.Physical) return PhysicalColor;
            if (type == SpellType.Magical) return MagicalColor;
            // Buff/Debuff aren't damage types — they let a non-damage spell (Rally, ...) still
            // reuse the same established colour language as ForStatusEffect's status chips.
            if (type == SpellType.Buff) return BuffColor;
            if (type == SpellType.Debuff) return DebuffColor;

            Realm? realm = type switch
            {
                SpellType.Fire  => Realm.Fire,
                SpellType.Water => Realm.Water,
                SpellType.Earth => Realm.Earth,
                SpellType.Wind  => Realm.Wind,
                SpellType.Dark  => Realm.Dark,
                SpellType.Light => Realm.Light,
                _ => (Realm?)null,
            };
            return CharacterSelectionManager.GetRealmBackgroundColor(realm);
        }

        /// <summary>Colour for a status-effect name, based on whether it helps or hurts the target.</summary>
        public static Color ForStatusEffect(StatusEffect.StatusEffectType effectType)
        {
            switch (effectType)
            {
                case StatusEffect.StatusEffectType.Buff:   return BuffColor;
                case StatusEffect.StatusEffectType.Debuff: return DebuffColor;
                default:                                    return NeutralColor;
            }
        }
    }
}
