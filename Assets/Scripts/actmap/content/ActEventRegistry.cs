using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Items;
using BaaroForce.Utils;
using static BaaroForce.ActMap.Content.ActChoiceEffects;

namespace BaaroForce.ActMap.Content
{
    /// <summary>
    /// One authored Event per realm — real text+choice content proving out the data-driven
    /// framework, per the design doc's "Events" section (risk/reward, world-building, on
    /// average a boost). Expand freely; <see cref="GetRandom"/> falls back gracefully if a
    /// realm ever has none.
    /// </summary>
    public static class ActEventRegistry
    {
        private static readonly Dictionary<Realm, List<ActEvent>> _byRealm = new Dictionary<Realm, List<ActEvent>>
        {
            { Realm.Fire, new List<ActEvent> { new ActEvent(
                "The Cinderwright's Bargain", Realm.Fire,
                "A soot-streaked artisan tends a forge that never seems to cool. \"Trade with me,\" she says, " +
                "\"and walk away richer one way or another.\"",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take her guaranteed payment", "She presses a stack of coin into your hands.",
                        m => GrantGold(m, 40)),
                    new ActChoiceOption("Gamble on what's behind the forge door",
                        "The door creaks open on something worth the risk.",
                        m => { if (UnityEngine.Random.value < 0.5f) GrantRelic(m, Rarity.Common); else GrantGold(m, 15); }),
                }) } },

            { Realm.Water, new List<ActEvent> { new ActEvent(
                "The Drowned Shrine", Realm.Water,
                "A half-submerged shrine hums beneath the waves. Diving deeper promises more, but the current is strong.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Skim the shallows", "You surface with a handful of clean, cold coin.",
                        m => GrantGold(m, 40)),
                    new ActChoiceOption("Dive for the shrine's heart",
                        "Your lungs burn, but your hand closes around something valuable.",
                        m => { if (UnityEngine.Random.value < 0.5f) GrantRelic(m, Rarity.Common); else GrantGold(m, 15); }),
                }) } },

            { Realm.Earth, new List<ActEvent> { new ActEvent(
                "The Mozeem Trade Post", Realm.Earth,
                "A wary Mozeem trader has goods to spare, if you're willing to haggle for the better cut.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Accept the fair trade", "The trader nods and hands over a fair sum.",
                        m => GrantGold(m, 40)),
                    new ActChoiceOption("Push for the good stuff",
                        "The trader grumbles, then relents — this was worth pushing for.",
                        m => { if (UnityEngine.Random.value < 0.5f) GrantRelic(m, Rarity.Common); else GrantGold(m, 15); }),
                }) } },

            { Realm.Wind, new List<ActEvent> { new ActEvent(
                "The Gale-Torn Caravan", Realm.Wind,
                "A merchant caravan, scattered by a sudden storm, has cargo strewn across the hillside.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Gather what's easy to reach", "You collect a modest handful of coin.",
                        m => GrantGold(m, 40)),
                    new ActChoiceOption("Chase the cargo the wind carried off",
                        "You catch up to it just before it tumbles off the cliff edge.",
                        m => { if (UnityEngine.Random.value < 0.5f) GrantRelic(m, Rarity.Common); else GrantGold(m, 15); }),
                }) } },

            { Realm.Light, new List<ActEvent> { new ActEvent(
                "The Sunlit Toll", Realm.Light,
                "A radiant, unmanned shrine offers a toll bowl and an unlocked reliquary beside it — an obvious test.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Pay the toll honestly", "The bowl brightens and returns your generosity threefold.",
                        m => GrantGold(m, 40)),
                    new ActChoiceOption("Take from the reliquary instead",
                        "Nothing strikes you down — whether that's luck or the shrine's actual intent, you're not sure.",
                        m => { if (UnityEngine.Random.value < 0.5f) GrantRelic(m, Rarity.Common); else GrantGold(m, 15); }),
                }) } },

            { Realm.Dark, new List<ActEvent> { new ActEvent(
                "The Whispering Vault", Realm.Dark,
                "A locked vault door bears a warning and a keyhole shaped like nothing you own. Something inside is listening.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Pry the lesser panel open", "A small stash spills out, unguarded.",
                        m => GrantGold(m, 40)),
                    new ActChoiceOption("Force the vault itself",
                        "The door gives way at the last moment, and whatever was listening lets you keep what you found.",
                        m => { if (UnityEngine.Random.value < 0.5f) GrantRelic(m, Rarity.Common); else GrantGold(m, 15); }),
                }) } },
        };

        // An event drawn here won't be offered again until every other event in that
        // realm has had a turn.
        private static readonly Dictionary<Realm, HashSet<string>> _shownByRealm =
            new Dictionary<Realm, HashSet<string>>();

        private static HashSet<string> ShownSet(Realm realm)
        {
            if (!_shownByRealm.TryGetValue(realm, out HashSet<string> set))
                _shownByRealm[realm] = set = new HashSet<string>();
            return set;
        }

        /// <summary>Clears the shown-event cycle for every realm. Call at the start of a new
        /// run so history from a previous run doesn't bleed into the next one.</summary>
        public static void ResetShownHistory() => _shownByRealm.Clear();

        /// <summary>Returns the realm's authored event, or null if none exists yet.</summary>
        public static ActEvent GetRandom(Realm realm)
        {
            if (!_byRealm.TryGetValue(realm, out List<ActEvent> pool) || pool.Count == 0)
                return null;
            return WeightedCyclePicker.PickOne(pool, identity: e => e.Title, weight: _ => 1f, ShownSet(realm));
        }

        public static void Register(ActEvent activeEvent)
        {
            if (activeEvent == null) return;
            if (!_byRealm.ContainsKey(activeEvent.Realm))
                _byRealm[activeEvent.Realm] = new List<ActEvent>();
            _byRealm[activeEvent.Realm].Add(activeEvent);
        }
    }
}
