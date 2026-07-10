namespace BaaroForce.Passives
{
    public class PassiveAbility
    {
        private string name;
        private string description;

        public string Name        => name;
        public string Description => description;

        public PassiveAbility(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }
}