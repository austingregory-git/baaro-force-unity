using System.Collections.Generic;
using BaaroForce.Spells;

namespace BaaroForce.Classes
{
    /// <summary>
    /// Stand-in used when a character promotes into a class ID that has no concrete
    /// <see cref="CharacterClass"/> subclass implemented yet — true for most of
    /// <see cref="ClassTree"/>'s promotion targets today (e.g. Warrior -> Samurai). Carries
    /// the ID/tier/specialty so downstream systems (UI, spell pools) behave sanely; grants
    /// no unique spells of its own until real content replaces it in <see cref="ClassRegistry"/>.
    /// </summary>
    public class PlaceholderCharacterClass : CharacterClass
    {
        public PlaceholderCharacterClass(string classID, Tier tier, ClassSpecialty specialty)
            : base(classID, tier, ClassTree.GetPromotions(classID), new List<Spell>(),
                  new ClassGrowthWeights(0.34, 0.33, 0.33), specialty)
        {
        }
    }
}
