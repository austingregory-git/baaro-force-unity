using System.Collections.Generic;

namespace BaaroForce.Classes
{
    /// <summary>
    /// Provides a canonical singleton instance of each <see cref="CharacterClass"/>
    /// keyed by <see cref="CharacterClass.ClassID"/>.
    ///
    /// Use <see cref="Get"/> wherever a class object is needed from a string ID —
    /// most commonly in <see cref="BaaroForce.Spells.ClassSpell"/> constructors.
    /// Add new classes to the initialiser block below or register them at runtime
    /// via <see cref="Register"/>.
    /// </summary>
    public static class ClassRegistry
    {
        private static readonly Dictionary<string, CharacterClass> _all =
            new Dictionary<string, CharacterClass>
            {
                { "Warrior",  new Warrior()  },
                { "Mage",     new Mage()     },
                { "Rogue",    new Rogue()    },
                { "Mystic",    new Mystic()    },
                { "Archer",    new Archer()    },
                { "DarkMage", new DarkMage() },
            };

        // ------------------------------------------------------------------ //
        // Lookup                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the canonical <see cref="CharacterClass"/> for
        /// <paramref name="classID"/>, or null if not registered.
        /// </summary>
        public static CharacterClass Get(string classID)
        {
            _all.TryGetValue(classID, out CharacterClass c);
            return c;
        }

        // ------------------------------------------------------------------ //
        // Registration                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Registers (or replaces) a class instance by its classID.</summary>
        public static void Register(CharacterClass characterClass)
        {
            if (characterClass != null)
                _all[characterClass.ClassID] = characterClass;
        }
    }
}
