using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Spells
{
    public static class SpellRegistry
    {
        // ── Name-based lookup ──────────────────────────────────────────────
        // Canonical spell instances keyed by name.
        private static readonly Dictionary<string, Spell> _all =
            new Dictionary<string, Spell>
            {
                { "Charge", new Charge() },
                { "Grit",   new Grit()   },
                { "Cleave", new Cleave() },
                { "Slam",   new Slam()   },
                { "Rally",  new Rally()  },
            };

        // ── Class-based spell pool ─────────────────────────────────────────
        // Maps classID → factories so each character receives a fresh instance.
        // Add new class spells here alongside the class that grants them.
        private static readonly Dictionary<string, List<Func<ClassSpell>>> _byClass =
            new Dictionary<string, List<Func<ClassSpell>>>
            {
                { "Warrior", new List<Func<ClassSpell>> {
                    () => new Charge(), () => new Grit(), () => new Cleave(),
                    () => new Slam(), () => new Rally() } },
            };

        // ------------------------------------------------------------------ //
        // Registration                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Registers a spell so it is recognised during name lookup.</summary>
        public static void Register(Spell spell)
        {
            if (spell != null)
                _all[spell.Name] = spell;
        }

        /// <summary>
        /// Registers a class spell factory so it appears in the random pool for
        /// <paramref name="classID"/>.
        /// </summary>
        public static void RegisterClassSpell(string classID, Func<ClassSpell> factory)
        {
            if (factory == null) return;
            if (!_byClass.ContainsKey(classID))
                _byClass[classID] = new List<Func<ClassSpell>>();
            _byClass[classID].Add(factory);
        }

        // ------------------------------------------------------------------ //
        // Lookup                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the <see cref="Spell"/> for <paramref name="name"/>, or null.</summary>
        public static Spell Get(string name)
        {
            _all.TryGetValue(name, out Spell spell);
            return spell;
        }

        /// <summary>
        /// Returns a randomly selected <see cref="ClassSpell"/> from the pool
        /// registered under <paramref name="classID"/>, or null if the class has
        /// no registered spells.
        /// </summary>
        public static ClassSpell GetRandomClassSpell(string classID)
        {
            if (classID == null) return null;
            if (!_byClass.TryGetValue(classID, out var factories) || factories.Count == 0)
                return null;
            return factories[UnityEngine.Random.Range(0, factories.Count)]();
        }
    }
}