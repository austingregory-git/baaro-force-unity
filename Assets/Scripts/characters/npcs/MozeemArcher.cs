using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Map;

namespace BaaroForce.Characters
{
    public class MozeemArcher : NPC
    {
        public override int BaseStrengthIndex => 1;

        static readonly string MozeemArcherModelPath = "Characters/frog_wizard_test";

        public MozeemArcher()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Mozeem Archer", 
                characterStats: new CharacterStats(maxHealthPoints: 4, baseAttack: 2, maxMana: 2, movement: 3),
                characterRealms: new List<Realm> { Realm.EARTH },
                characterPassiveAbilities: new List<PassiveAbility>
                {

                },
                characterSpells: new List<Spell>
                {
                    
                },
                characterModelPath: MozeemArcherModelPath,
                specialty: NPCSpecialty.RANGED)
        {
            AI = new AggressiveNpcAI();
        }
    }
}