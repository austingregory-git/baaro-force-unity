using UnityEngine;

namespace BaaroForce.Characters
{
    public class CharacterStats
    {
        /// <summary>Maximum health points (set at character creation, never changes).</summary>
        public int MaxHealthPoints;
        /// <summary>Current health points. Character is defeated when this reaches 0.</summary>
        public int HealthPoints;
        /// <summary>Unmodified base attack value.</summary>
        public int BaseAttack;
        /// <summary>
        /// Bonus attack accumulated from equipment, buffs, or other effects.
        /// Increase this when a weapon or effect grants additional attack.
        /// </summary>
        public int AttackBonus;
        /// <summary>Effective attack used in damage calculations (baseAttack + attackBonus).</summary>
        public int TotalAttack => BaseAttack + AttackBonus;

        public int SpellPowerBonus;  // bonus to spell power from equipment, buffs, or other effects
        public int MaxMana;
        public int Mana;
        public int Movement;
        /// <summary>Actions (attack / spell / item) available per turn. Defaults to 1.</summary>
        public int MaxActionPoints;
        public int ShieldPoints;  // temporary shield that absorbs damage before health is affected

        public CharacterStats(int maxHealthPoints, int baseAttack, int maxMana, int movement,
                              int maxActionPoints = 1)
        {
            this.MaxHealthPoints = maxHealthPoints;
            this.HealthPoints    = maxHealthPoints;
            this.BaseAttack      = baseAttack;
            this.AttackBonus     = 0;
            this.MaxMana         = maxMana;
            this.Mana            = maxMana;
            this.Movement        = movement;
            this.MaxActionPoints = maxActionPoints;
            this.ShieldPoints     = 0;
        }

        /// <summary>
        /// Applies incoming damage, spending ShieldPoints first unless <paramref name="ignoreShield"/>
        /// is set (e.g. true/piercing damage). The single entry point for damage across the game —
        /// every spell/passive/basic-attack should route damage through this rather than touching
        /// HealthPoints directly, so shields stay consistent everywhere.
        /// </summary>
        /// <returns>The amount that actually landed on HealthPoints, after any shield absorption.</returns>
        public int TakeDamage(int amount, bool ignoreShield = false)
        {
            if (amount <= 0) return 0;

            if (!ignoreShield && ShieldPoints > 0)
            {
                int absorbed = Mathf.Min(ShieldPoints, amount);
                ShieldPoints -= absorbed;
                amount -= absorbed;
            }

            HealthPoints -= amount;
            return amount;
        }

        /// <summary>
        /// Applies healing, clamped so HealthPoints never exceeds MaxHealthPoints. The
        /// symmetric counterpart to <see cref="TakeDamage"/> — every heal source should
        /// route through this rather than touching HealthPoints directly, so the actual
        /// (post-clamp) amount gained is always known at the call site.
        /// </summary>
        /// <returns>The amount actually gained, after clamping to MaxHealthPoints.</returns>
        public int Heal(int amount)
        {
            if (amount <= 0) return 0;

            int before = HealthPoints;
            HealthPoints = Mathf.Min(HealthPoints + amount, MaxHealthPoints);
            return HealthPoints - before;
        }

        /// <summary>
        /// Non-mutating preview of what <see cref="TakeDamage"/> would do — used to show
        /// a predicted HP/shield outcome before the player commits to an action. Mirrors
        /// TakeDamage's shield-first absorption exactly, without touching this instance.
        /// </summary>
        public (int predictedHp, int predictedShield) PeekDamage(int amount, bool ignoreShield = false)
        {
            if (amount <= 0) return (HealthPoints, ShieldPoints);

            int shield = ShieldPoints;
            if (!ignoreShield && shield > 0)
            {
                int absorbed = Mathf.Min(shield, amount);
                shield -= absorbed;
                amount -= absorbed;
            }

            return (HealthPoints - amount, shield);
        }

        /// <summary>
        /// Non-mutating preview of what <see cref="Heal"/> would do — mirrors its
        /// max-HP clamp exactly, without touching this instance.
        /// </summary>
        public int PeekHeal(int amount) =>
            amount <= 0 ? HealthPoints : Mathf.Min(HealthPoints + amount, MaxHealthPoints);
    }
}
