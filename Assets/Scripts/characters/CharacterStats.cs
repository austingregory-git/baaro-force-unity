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
    }
}
