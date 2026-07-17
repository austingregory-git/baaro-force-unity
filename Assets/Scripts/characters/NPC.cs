using System.Collections.Generic;
using BaaroForce.Classes;
using BaaroForce.Passives;
using BaaroForce.Spells;

namespace BaaroForce.Characters
{
    public class NPC : Character
    {
        public NPCSpecialty Specialty { get; set; } = NPCSpecialty.MELEE;

        /// <summary>
        /// Base perceived difficulty of this NPC type (independent of level).
        /// Override in each concrete NPC subclass.
        /// </summary>
        public virtual int BaseStrengthIndex => 1;

        /// <summary>Effective strength used when building an enemy pack: BaseStrengthIndex × Level.</summary>
        public int StrengthIndex => BaseStrengthIndex * Level;

        /// <summary>The AI strategy that drives this NPC's decisions during the enemy turn.
        /// Set in each concrete subclass constructor.  NPCs with a null AI are skipped
        /// during the enemy turn.</summary>
        public NpcAI AI { get; set; }

        public NPC(
                        string characterName,
                        CharacterStats characterStats,
                        List<Realm> characterRealms,
                        List<PassiveAbility> characterPassiveAbilities,
                        List<Spell> characterSpells,
                        string characterModelPath,
                        NPCSpecialty specialty = NPCSpecialty.MELEE)
            : base(
                characterClass: null,
                characterName: characterName,
                characterStats: characterStats,
                characterRealms: characterRealms,
                characterPassiveAbilities: characterPassiveAbilities,
                characterSpells: characterSpells,
                characterModelPath: characterModelPath)
        {
            Specialty = specialty;
        }

        public enum NPCSpecialty
        {
            MELEE,
            RANGED,
            MAGIC,
        }
    }
}
