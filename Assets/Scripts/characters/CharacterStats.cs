namespace BaaroForce.Characters
{
    public class CharacterStats
    {
        public int healthPoints;
        public int baseAttack;
        public int mana;
        public int movement;

        public CharacterStats(int healthPoints, int baseAttack, int mana, int movement)
        {
            this.healthPoints = healthPoints;
            this.baseAttack = baseAttack;
            this.mana = mana;
            this.movement = movement;
        }
    }
}
