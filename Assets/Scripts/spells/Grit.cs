using BaaroForce.Classes;

namespace BaaroForce.Spells
{
    public class Grit : ClassSpell
    {
        public Grit() : base(
            characterClass: ClassRegistry.Get("Warrior"), 
            name: "Grit", 
            description: "Gain 3 + 0.5 x [Level] maximum health for the fight.") { }
    }
}