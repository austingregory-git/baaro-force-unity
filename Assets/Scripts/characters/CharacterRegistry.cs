using System;
using System.Collections.Generic;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Central registry of all playable characters.
    /// Add new characters to <see cref="_factories"/> as they are implemented.
    /// Each entry is a factory function so callers always receive a fresh instance.
    /// </summary>
    public static class CharacterRegistry
    {
        private static readonly List<Func<Character>> _factories = new List<Func<Character>>
        {
            () => new Winston(),
            () => new Beepo(),
            () => new Shopu(),
            () => new Guppy(),
            () => new Buggles(),
            () => new Hans()
        };

        /// <summary>Returns the full list of character factory functions.</summary>
        public static IReadOnlyList<Func<Character>> GetAll() => _factories;

        /// <summary>Registers an additional character factory at runtime.</summary>
        public static void Register(Func<Character> factory)
        {
            if (factory != null)
                _factories.Add(factory);
        }
    }
}