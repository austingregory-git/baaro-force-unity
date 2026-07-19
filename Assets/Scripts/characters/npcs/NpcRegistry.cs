using System;
using System.Collections.Generic;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Central registry of all non-playable characters (NPCs).
    /// Add new NPCs to <see cref="_factories"/> as they are implemented.
    /// Each entry is a factory function so callers always receive a fresh instance.
    /// </summary>
    public static class NPCRegistry
    {
        private static readonly List<Func<NPC>> _factories = new List<Func<NPC>>
        {
            () => new Wolf(),
        };

        /// <summary>Returns the full list of NPC factory functions.</summary>
        public static IReadOnlyList<Func<NPC>> GetAll() => _factories;

        /// <summary>Registers an additional NPC factory at runtime.</summary>
        public static void Register(Func<NPC> factory)
        {
            if (factory != null)
                _factories.Add(factory);
        }
    }
}