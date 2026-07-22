using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;
using BaaroForce.Map;

namespace BaaroForce.Characters
{
    public class MozeemArcher : Npc
    {
        static readonly string MozeemArcherProfilePicPath = "winston_profile_pic_128x128";

        public MozeemArcher()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Mozeem Archer", 
                characterStats: new CharacterStats(maxHealthPoints: 4, baseAttack: 2, maxMana: 2, movement: 3),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {

                },
                characterSpells: new List<Spell>
                {
                    
                },
                characterProfilePicPath: MozeemArcherProfilePicPath,
                specialty: NpcSpecialty.Ranged)
        {
            AI = new AggressiveNpcAI();
        }
    }
}