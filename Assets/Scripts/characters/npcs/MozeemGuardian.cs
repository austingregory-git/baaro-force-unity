using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Map;

namespace BaaroForce.Characters
{
    public class MozeemGuardian : Npc
    {
        static readonly string MozeemGuardianProfilePicPath = "winston_profile_pic_128x128";

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
                characterProfilePicPath: MozeemGuardianProfilePicPath,
                specialty: NpcSpecialty.Melee)
        {
            AI = new AggressiveNpcAI();
        }
    }
}