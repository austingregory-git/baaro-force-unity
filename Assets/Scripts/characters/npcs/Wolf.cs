using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Wolf : NPC
    {
        public override int BaseStrengthIndex => 1;

        static readonly string WolfModelPath = "Characters/frog_wizard_test";

        public Wolf()
            : base(
                //ClassRegistry.Get("Beast"), 
                characterName: "Wolf", 
                characterStats: new CharacterStats(maxHealthPoints: 5, baseAttack: 2, maxMana: 2, movement: 5),
                characterRealms: new List<Realm> { Realm.EARTH },
                characterPassiveAbilities: new List<PassiveAbility>
                {
                    new PassiveAbility("Autumnal Growth",
                        "[Regen] 1 + 0.25 x [Level] health points per turn")
                },
                characterSpells: new List<Spell>
                {
                    new DeathStare(),
                },
                characterModelPath: WolfModelPath)
        {
            AI = new AggressiveNpcAI();
        }
    }
}