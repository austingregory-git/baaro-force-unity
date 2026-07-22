using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaaroForce.Passives
{
    public static class PassiveAbilityRegistry
    {
        // ── Name-based lookup ──────────────────────────────────────────────
        // Canonical spell instances keyed by name.
        private static readonly Dictionary<string, PassiveAbility> _all =
            new Dictionary<string, PassiveAbility>
            {
                { "Autumnal Growth", new AutumnalGrowth() },
                { "In The Trees",    new InTheTrees()     },
                { "Bubble Shield",   new BubbleShield()   },
            };

        // ── Class-based talent pool ────────────────────────────────────────
        // Maps classID → factories so each character receives a fresh instance.
        // Drawn from at Level 3 (see Character.GrantExperience). Seeded with existing
        // "signature" passives reused as generic class talents — a foundation pool
        // until dedicated tier-1 talents are authored per class.
        private static readonly Dictionary<string, List<Func<PassiveAbility>>> _byClass =
            new Dictionary<string, List<Func<PassiveAbility>>>
            {
                { "Warrior", new List<Func<PassiveAbility>> { () => new BurningShield(), () => new AutumnalGrowth() } },
                { "Rogue",   new List<Func<PassiveAbility>> { () => new InTheTrees(),    () => new BubbleShield()   } },
                { "Mage",    new List<Func<PassiveAbility>> { () => new BubbleShield(),  () => new AutumnalGrowth() } },
                { "Archer",  new List<Func<PassiveAbility>> { () => new LongBow(),       () => new InTheTrees()     } },
                { "Mystic",  new List<Func<PassiveAbility>> { () => new AutumnalGrowth(),() => new SpiritualProtector() } },
            };

        // ------------------------------------------------------------------ //
        // Registration                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Registers a passive ability so it is recognised during name lookup.</summary>
        public static void Register(PassiveAbility ability)
        {
            if (ability != null)
                _all[ability.Name] = ability;
        }

            /// <summary>
            /// Registers a class spell factory so it appears in the random pool for
            /// <paramref name="classID"/>.
            /// </summary>
            public static void RegisterClassPassive(string classID, Func<PassiveAbility> factory)
            {
                if (factory == null) return;
                if (!_byClass.ContainsKey(classID))
                    _byClass[classID] = new List<Func<PassiveAbility>>();
                _byClass[classID].Add(factory);
            }

        // ------------------------------------------------------------------ //
        // Lookup                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the <see cref="PassiveAbility"/> for <paramref name="name"/>, or null.</summary>
        public static PassiveAbility Get(string name)
        {
            _all.TryGetValue(name, out PassiveAbility ability);
            return ability;
        }

        /// <summary>
        /// Returns a randomly selected <see cref="PassiveAbility"/> from the pool
        /// registered under <paramref name="classID"/>, or null if the class has
        /// no registered passives.
        /// </summary>
        public static PassiveAbility GetRandomClassPassive(string classID)
        {
            if (classID == null) return null;
            if (!_byClass.TryGetValue(classID, out var factories) || factories.Count == 0)
                return null;
            return factories[UnityEngine.Random.Range(0, factories.Count)]();
        }
    }
}