using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Map;

namespace BaaroForce.Characters
{
    public class MozeemElder : Npc
    {
        public override int BaseStrengthIndex => 1;

        static readonly string MozeemElderModelPath = "Characters/frog_wizard_test";

        public MozeemElder()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Mozeem Elder", 
                characterStats: new CharacterStats(maxHealthPoints: 5, baseAttack: 1, maxMana: 8, movement: 2),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {

                },
                characterSpells: new List<Spell>
                {
                    
                },
                characterModelPath: MozeemElderModelPath,
                specialty: NpcSpecialty.Magic)
        {
            AI = new AggressiveNpcAI();
        }
    }
}