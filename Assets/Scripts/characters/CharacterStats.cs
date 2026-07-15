namespace BaaroForce.Characters
{
    public class CharacterStats
    {
        /// <summary>Maximum health points (set at character creation, never changes).</summary>
        public int maxHealthPoints;
        /// <summary>Current health points. Character is defeated when this reaches 0.</summary>
        public int healthPoints;
        /// <summary>Unmodified base attack value.</summary>
        public int baseAttack;
        /// <summary>
        /// Bonus attack accumulated from equipment, buffs, or other effects.
        /// Increase this when a weapon or effect grants additional attack.
        /// </summary>
        public int attackBonus;
        /// <summary>Effective attack used in damage calculations (baseAttack + attackBonus).</summary>
        public int TotalAttack => baseAttack + attackBonus;
        public int maxMana;
        public int mana;
        public int movement;
        /// <summary>Actions (attack / spell / item) available per turn. Defaults to 1.</summary>
        public int maxActionPoints;

        public CharacterStats(int maxHealthPoints, int baseAttack, int maxMana, int movement,
                              int maxActionPoints = 1)
        {
            this.maxHealthPoints = maxHealthPoints;
            this.healthPoints    = maxHealthPoints;
            this.baseAttack      = baseAttack;
            this.attackBonus     = 0;
            this.maxMana         = maxMana;
            this.mana            = maxMana;
            this.movement        = movement;
            this.maxActionPoints = maxActionPoints;
        }
    }
}
