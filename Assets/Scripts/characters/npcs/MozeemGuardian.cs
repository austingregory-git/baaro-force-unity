using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Map;

namespace BaaroForce.Characters
{
    public class MozeemGuardian : Npc
    {
        public override int BaseStrengthIndex => 1;

        static readonly string MozeemGuardianModelPath = "Characters/frog_wizard_test";

        public MozeemGuardian()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Mozeem Guardian", 
                characterStats: new CharacterStats(maxHealthPoints: 6, baseAttack: 2, maxMana: 3, movement: 2),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {

                },
                characterSpells: new List<Spell>
                {
                    
                },
                characterModelPath: MozeemGuardianModelPath,
                specialty: NpcSpecialty.Melee)
        {
            AI = new AggressiveNpcAI();
        }
    }
}