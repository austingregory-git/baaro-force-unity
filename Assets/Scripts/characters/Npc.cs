using System.Collections.Generic;
using BaaroForce.Animations;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class Npc : Character
    {
        public NpcSpecialty Specialty { get; set; }

        /// <summary>The AI strategy that drives this Npc's decisions during the enemy turn.
        /// Set in each concrete subclass constructor.  Npcs with a null AI are skipped
        /// during the enemy turn.</summary>
        public NpcAI AI { get; set; }

        /// <summary>True if this Npc can see through Invisible (see Character.IsInvisible) and
        /// may still target an otherwise-hidden character. False for most Npcs — override in
        /// a subclass to create a "sees through stealth" enemy type.</summary>
        public virtual bool IgnoresInvisibility => false;

        public Npc(
                        string characterName,
                        CharacterStats characterStats,
                        List<Realm> characterRealms,
                        List<PassiveAbility> characterPassiveAbilities,
                        List<Spell> characterSpells,
                        string characterProfilePicPath,
                        NpcSpecialty specialty = NpcSpecialty.Melee,
                        SpriteKit characterSpriteKit = null)
            : base(
                characterClass: null,
                characterName: characterName,
                characterStats: characterStats,
                characterRealms: characterRealms,
                characterPassiveAbilities: characterPassiveAbilities,
                characterSpells: characterSpells,
                characterProfilePicPath: characterProfilePicPath,
                characterSpriteKit: characterSpriteKit)
        {
            Specialty = specialty;
        }

        public enum NpcSpecialty
        {
            Melee,
            Ranged,
            Magic,
        }
    }
}
