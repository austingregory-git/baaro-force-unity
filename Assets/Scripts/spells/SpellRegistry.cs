using System;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Utils;

namespace BaaroForce.Spells
{
    public static class SpellRegistry
    {
        // ── Name-based lookup ──────────────────────────────────────────────
        // Canonical spell instances keyed by name.
        private static readonly Dictionary<string, Spell> _all =
            new Dictionary<string, Spell>
            {
                { "Charge",         new Charge()       },
                { "Grit",           new Grit()         },
                { "Cleave",         new Cleave()       },
                { "Slam",           new Slam()         },
                { "Rally",          new Rally()        },
                { "Evasion",        new Evasion()      },
                { "Mug",            new Mug()          },
                { "Shiv",           new Shiv()         },
                { "Backstab",       new Backstab()     },
                { "Throwing Knife", new ThrowingKnife()},
                { "Meditate",       new Meditate()     },
                { "Magic Dart",     new MagicDart()    },
                { "Arcane Beam",    new ArcaneBeam()   },
                { "Bind",           new Bind()         },
                { "Arcane Explosion", new ArcaneExplosion() },
                { "Aim",            new Aim()          },
                { "Craft Arrows",   new CraftArrows()  },
                { "Double Shot",    new DoubleShot()   },
                { "Piercing Shot",  new PiercingShot() },
                { "Volley",         new Volley()       },
                { "Heal",           new Heal()         },
                { "Poison",         new Poison()       },
                { "Rejuvenate",     new Rejuvenate()   },
                { "Vine Lash",      new VineLash()     },
                { "Empower",        new Empower()      },
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
                { "Rogue", new List<Func<ClassSpell>> {
                    () => new Evasion(), () => new Mug(), () => new Shiv(),
                    () => new Backstab(), () => new ThrowingKnife() } },
                { "Mage", new List<Func<ClassSpell>> {
                    () => new Meditate(), () => new MagicDart(), () => new ArcaneBeam(), () => new Bind(),
                    () => new ArcaneExplosion() } },
                { "Archer", new List<Func<ClassSpell>> {
                    () => new Aim(), () => new CraftArrows(), () => new DoubleShot(),
                    () => new PiercingShot(), () => new Volley() } },
                { "Mystic", new List<Func<ClassSpell>> {
                    () => new Heal(), () => new Poison(), () => new Rejuvenate(),
                    () => new VineLash(), () => new Empower() } },
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

        // Per-class shown history for player-facing spell OFFERS (e.g. the Royal Decree
        // "learn a spell" pick) — kept separate from GetRandomClassSpell above, which is
        // used for automatic character-creation spell assignment and shouldn't share a
        // cycle with what's been shown to the player in a choice screen.
        private static readonly Dictionary<string, HashSet<string>> _offeredShownByClass =
            new Dictionary<string, HashSet<string>>();

        // Which classes have been offered a spell choice since the last time every class
        // had a turn — kept separate from _offeredShownByClass so the two cycles (which
        // class shows up / which spell within it) don't interfere with each other.
        private static readonly HashSet<string> _offeredShownClassIDs = new HashSet<string>();

        private static HashSet<string> OfferedShownSet(string classID)
        {
            if (!_offeredShownByClass.TryGetValue(classID, out HashSet<string> set))
                _offeredShownByClass[classID] = set = new HashSet<string>();
            return set;
        }

        /// <summary>Clears the shown-spell-offer cycle for every class. Call at the start of
        /// a new run so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetOfferedShownHistory()
        {
            _offeredShownByClass.Clear();
            _offeredShownClassIDs.Clear();
        }

        /// <summary>
        /// Returns up to <paramref name="count"/> distinct <see cref="ClassSpell"/>s offered
        /// to the player from <paramref name="classID"/>'s pool — e.g. a Royal Decree "learn a
        /// spell" choice. A spell offered here won't be offered again until every other spell
        /// in that class has had a turn.
        /// </summary>
        public static List<ClassSpell> GetRandomClassSpellsToOffer(string classID, int count)
        {
            if (classID == null || !_byClass.TryGetValue(classID, out var factories) || factories.Count == 0)
                return new List<ClassSpell>();

            List<Func<ClassSpell>> picked = WeightedCyclePicker.PickMany(
                factories, identity: f => f().Name, weight: _ => 1f,
                count: count, shownHistory: OfferedShownSet(classID));

            var result = new List<ClassSpell>(picked.Count);
            foreach (Func<ClassSpell> factory in picked) result.Add(factory());
            return result;
        }

        /// <summary>
        /// Picks <paramref name="count"/> distinct classes from <paramref name="classIDs"/>
        /// (preferring ones not yet offered since the last full cycle) and returns one
        /// randomly offered spell from each — e.g. for the Royal Decree "learn a spell"
        /// choice, so it doesn't always surface the same class or the same spell.
        /// </summary>
        public static List<ClassSpell> GetRandomClassSpellOffers(IReadOnlyList<string> classIDs, int count)
        {
            List<string> chosenClasses = WeightedCyclePicker.PickMany(
                classIDs, identity: c => c, weight: _ => 1f,
                count: count, shownHistory: _offeredShownClassIDs);

            var result = new List<ClassSpell>(chosenClasses.Count);
            foreach (string classID in chosenClasses)
            {
                List<ClassSpell> offered = GetRandomClassSpellsToOffer(classID, 1);
                if (offered.Count > 0) result.Add(offered[0]);
            }
            return result;
        }
    }
}