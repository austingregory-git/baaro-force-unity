using BaaroForce.Classes;

namespace BaaroForce.Spells
{
    public class Charge : ClassSpell
    {
        public Charge() : base(
            characterClass: ClassRegistry.Get("Warrior"), 
            name: "Charge", 
            description: "Charge at an enemy, dealing damage equal to your basic attack.") { }
    }
}