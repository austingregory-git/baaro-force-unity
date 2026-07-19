using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Wolf : Npc
    {
        public override int BaseStrengthIndex => 1;

        static readonly string WolfModelPath = "Characters/frog_wizard_test";

        public Wolf()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Wolf", 
                characterStats: new CharacterStats(maxHealthPoints: 5, baseAttack: 2, maxMana: 2, movement: 5),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {

                },
                characterSpells: new List<Spell>
                {
                    
                },
                characterModelPath: WolfModelPath,
                specialty: NpcSpecialty.Melee)
        {
            AI = new AggressiveNpcAI();
        }
    }
}