using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Wolf : Npc
    {
        public override int BaseStrengthIndex => 1;

        static readonly string WolfProfilePicPath = "winston_profile_pic_128x128";

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
                characterProfilePicPath: WolfProfilePicPath,
                specialty: NpcSpecialty.Melee)
        {
            AI = new AggressiveNpcAI();
        }
    }
}