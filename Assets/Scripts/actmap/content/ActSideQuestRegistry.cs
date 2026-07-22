using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.Items;
using static BaaroForce.ActMap.Content.ActChoiceEffects;

namespace BaaroForce.ActMap.Content
{
    /// <summary>
    /// One authored SideQuest per realm — real text+choice content, per the design doc's
    /// "Side Quests" section (low-risk, near-guaranteed-positive: gold + XP plus a
    /// choice-specific third reward). Expand freely; <see cref="GetRandom"/> falls back
    /// gracefully if a realm ever has none.
    /// </summary>
    public static class ActSideQuestRegistry
    {
        private const int Gold = 25;
        private const int Xp = 5;

        private static readonly Dictionary<Realm, List<ActSideQuest>> _byRealm = new Dictionary<Realm, List<ActSideQuest>>
        {
            { Realm.Fire, new List<ActSideQuest> { new ActSideQuest(
                "Kindling the Watchfires", Realm.Fire,
                "A village elder asks your party to relight the hillside watchfires before nightfall. Simple work, honest pay.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take the coin purse", "The elder pays you well and sends you on your way.",
                        m => { GrantGold(m, Gold); GrantPartyExperience(m, Xp); }),
                    new ActChoiceOption("Ask for supplies instead", "The elder digs through a chest and hands over a healing draught.",
                        m => { GrantGold(m, Gold / 2); GrantPartyExperience(m, Xp); GrantPotion(m, Rarity.Common); }),
                }) } },

            { Realm.Water, new List<ActSideQuest> { new ActSideQuest(
                "The Tangled Nets", Realm.Water,
                "Fisherfolk offer payment to clear a snarl of nets fouling the harbor mouth.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take the coin purse", "Grateful, the fisherfolk pay in full.",
                        m => { GrantGold(m, Gold); GrantPartyExperience(m, Xp); }),
                    new ActChoiceOption("Ask for gear instead", "They offer up a piece of equipment from their stores instead.",
                        m => { GrantGold(m, Gold / 2); GrantPartyExperience(m, Xp); GrantEquipmentToRandomMember(m, Rarity.Common); }),
                }) } },

            { Realm.Earth, new List<ActSideQuest> { new ActSideQuest(
                "Mending the Root Wall", Realm.Earth,
                "A Mozeem settlement needs help bracing a wall of living roots before the next storm.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take the coin purse", "The settlement pays in full for the day's labor.",
                        m => { GrantGold(m, Gold); GrantPartyExperience(m, Xp); }),
                    new ActChoiceOption("Ask for supplies instead", "They send you off with a healing draught from their stores.",
                        m => { GrantGold(m, Gold / 2); GrantPartyExperience(m, Xp); GrantPotion(m, Rarity.Common); }),
                }) } },

            { Realm.Wind, new List<ActSideQuest> { new ActSideQuest(
                "Chasing a Runaway Kite-Sail", Realm.Wind,
                "A trader's cargo-kite has slipped its mooring and is dragging supplies across the plain.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take the coin purse", "The trader is relieved and pays generously.",
                        m => { GrantGold(m, Gold); GrantPartyExperience(m, Xp); }),
                    new ActChoiceOption("Ask for gear instead", "The trader offers a piece of equipment salvaged from the cargo.",
                        m => { GrantGold(m, Gold / 2); GrantPartyExperience(m, Xp); GrantEquipmentToRandomMember(m, Rarity.Common); }),
                }) } },

            { Realm.Light, new List<ActSideQuest> { new ActSideQuest(
                "Escorting the Dawn Procession", Realm.Light,
                "A small procession asks for an escort through a stretch of road with a bad reputation.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take the coin purse", "The procession's elder pays your party in full.",
                        m => { GrantGold(m, Gold); GrantPartyExperience(m, Xp); }),
                    new ActChoiceOption("Ask for a blessing instead", "The procession shares a small relic from their reliquary.",
                        m => { GrantGold(m, Gold / 2); GrantPartyExperience(m, Xp); GrantRelic(m, Rarity.Common); }),
                }) } },

            { Realm.Dark, new List<ActSideQuest> { new ActSideQuest(
                "Clearing the Old Crypt Door", Realm.Dark,
                "A caretaker wants help clearing rubble from a sealed crypt entrance — nothing dangerous, just heavy.",
                new List<ActChoiceOption>
                {
                    new ActChoiceOption("Take the coin purse", "The caretaker pays your party for the labor.",
                        m => { GrantGold(m, Gold); GrantPartyExperience(m, Xp); }),
                    new ActChoiceOption("Ask to keep a trinket instead", "The caretaker lets you keep a small relic found in the rubble.",
                        m => { GrantGold(m, Gold / 2); GrantPartyExperience(m, Xp); GrantRelic(m, Rarity.Common); }),
                }) } },
        };

        /// <summary>Returns the realm's authored side quest, or null if none exists yet.</summary>
        public static ActSideQuest GetRandom(Realm realm)
        {
            if (!_byRealm.TryGetValue(realm, out List<ActSideQuest> pool) || pool.Count == 0)
                return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public static void Register(ActSideQuest sideQuest)
        {
            if (sideQuest == null) return;
            if (!_byRealm.ContainsKey(sideQuest.Realm))
                _byRealm[sideQuest.Realm] = new List<ActSideQuest>();
            _byRealm[sideQuest.Realm].Add(sideQuest);
        }
    }
}
