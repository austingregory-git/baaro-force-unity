using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class AlphaWolf : Npc
    {
        static readonly string AlphaWolfProfilePicPath = "winston_profile_pic_128x128";

        public AlphaWolf()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Alpha Wolf", 
                characterStats: new CharacterStats(maxHealthPoints: 6, baseAttack: 3, maxMana: 2, movement: 5),
                characterRealms: new List<Realm> { Realm.Earth },
                characterPassiveAbilities: new List<PassiveAbility>
                {

                },
                characterSpells: new List<Spell>
                {
                    
                },
                characterProfilePicPath: AlphaWolfProfilePicPath,
                specialty: NpcSpecialty.Melee)
        {
            AI = new AggressiveNpcAI();
        }
    }
}