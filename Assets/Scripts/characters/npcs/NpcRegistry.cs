using System;
using System.Collections.Generic;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Central registry of all non-playable characters (Npcs).
    /// Add new Npcs to <see cref="_factories"/> as they are implemented.
    /// Each entry is a factory function so callers always receive a fresh instance.
    /// </summary>
    public static class NpcRegistry
    {
        private static readonly List<Func<Npc>> _factories = new List<Func<Npc>>
        {
            () => new Wolf(),
        };

        /// <summary>Returns the full list of Npc factory functions.</summary>
        public static IReadOnlyList<Func<Npc>> GetAll() => _factories;

        /// <summary>Registers an additional Npc factory at runtime.</summary>
        public static void Register(Func<Npc> factory)
        {
            if (factory != null)
                _factories.Add(factory);
        }
    }
}