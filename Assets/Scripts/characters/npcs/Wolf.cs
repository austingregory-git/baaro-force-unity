using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Wolf : NPC
    {
        public override int BaseStrengthIndex => 1;

        static readonly string WOLF_MODEL_PATH = "Characters/frog_wizard_test";

        public Wolf()
            : base(
                //ClassRegistry.Get("Beast"), 
                "Wolf", new CharacterStats(healthPoints: 5, baseAttack: 2, mana: 1, movement: 5),
                new List<Realm> { Realm.EARTH },
                new List<PassiveAbility>
                {
                    new PassiveAbility("Autumnal Growth",
                        "[Regen] 1 + 0.25 x [Level] health points per turn")
                },
                new List<Spell>
                {
                    new DeathStare(),
                },
                WOLF_MODEL_PATH)
        {
            AI = new AggressiveNpcAI();
        }

        public PassiveAbility GetWolfPassiveAbility()
        {
            return new PassiveAbility("Autumnal Growth",
                "[Regen] 1 + 0.25 x [Level] health points per turn");
        }
    }
}