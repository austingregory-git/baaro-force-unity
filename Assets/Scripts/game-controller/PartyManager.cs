using System.Collections.Generic;
using UnityEngine;
using BaaroForce.ActMap;
using BaaroForce.ActMap.Content;
using BaaroForce.ActMap.Encounters;
using BaaroForce.Characters;
using BaaroForce.Items;
using BaaroForce.Party;
using BaaroForce.Relics;
using BaaroForce.Spells;
using BaaroForce.Utils;

namespace BaaroForce.GameController
{
    /// <summary>
    /// Persistent singleton that holds the player's party across scene loads.
    /// Auto-creates itself on first access so no scene setup is required.
    /// </summary>
    public class PartyManager : MonoBehaviour
    {
        private static PartyManager _instance;

        public static PartyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("[PartyManager]");
                    _instance = go.AddComponent<PartyManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public BaaroForce.Party.Party Party { get; private set; }
        public List<Relic> Relics { get; private set; } = new List<Relic>();

        /// <summary>The Act 1 map and run progress. Created fresh in Awake/ResetForNewRun
        /// alongside Party.</summary>
        public ActRunState ActRun { get; private set; }

        /// <summary>The Realm chosen for this game session. Null until set by MainMenu.</summary>
        public Realm? CurrentRealm { get; private set; }

        /// <summary>How many fights into the run the party currently is (1 = the first fight).
        /// Used to scale things like fight-end gold rewards. Advances via <see cref="AdvanceDepth"/>.</summary>
        public int Depth { get; private set; } = 1;

        /// <summary>Which act the run is currently on (1-4) — determines the floor rarity for
        /// fight-reward equipment/potions, see <see cref="BaaroForce.Loot.FightRewardGenerator"/>.
        /// Nothing advances this yet (only Act 1 exists in-game); it's here so the reward-rarity
        /// system already works once later acts are wired up.</summary>
        public int CurrentAct { get; private set; } = 1;

        /// <summary>Consecutive Normal fights since equipment last hit its rarity-upgrade pity
        /// roll (see <see cref="BaaroForce.Loot.FightRewardGenerator"/>). Resets to 0 on a hit.</summary>
        public int EquipmentPityStreak { get; private set; }

        /// <summary>Same as <see cref="EquipmentPityStreak"/>, tracked independently for potions.</summary>
        public int PotionPityStreak { get; private set; }

        public void SetRealm(Realm realm) => CurrentRealm = realm;

        /// <summary>Advances progression to the next fight's depth.</summary>
        public void AdvanceDepth() => Depth++;

        /// <summary>Advances to the next act, resetting nothing else — Depth keeps climbing
        /// across acts (it tracks total fights this run, not per-act progress).</summary>
        public void AdvanceAct() => CurrentAct++;

        public void ResetEquipmentPityStreak() => EquipmentPityStreak = 0;
        public void IncrementEquipmentPityStreak() => EquipmentPityStreak++;
        public void ResetPotionPityStreak() => PotionPityStreak = 0;
        public void IncrementPotionPityStreak() => PotionPityStreak++;

        /// <summary>Resets the party and progression for a fresh run (e.g. after Game Over).</summary>
        public void ResetForNewRun()
        {
            ClearParty();
            Depth = 1;
            CurrentAct = 1;
            EquipmentPityStreak = 0;
            PotionPityStreak = 0;
            Relics.Clear();
            ActRun = new ActRunState();
            ResetContentShownHistory();
        }

        /// <summary>Clears every "don't repeat until everyone's had a turn" shown-history
        /// cycle across the game's content registries, so a new run starts fresh instead of
        /// inheriting which characters/items/spells/encounters were already shown in the
        /// previous one.</summary>
        private static void ResetContentShownHistory()
        {
            CharacterUtils.ResetShownHistory();
            EquipmentRegistry.ResetShownHistory();
            PotionRegistry.ResetShownHistory();
            RelicRegistry.ResetShownHistory();
            SpellRegistry.ResetOfferedShownHistory();
            ActEventRegistry.ResetShownHistory();
            ActSideQuestRegistry.ResetShownHistory();
            EncounterRegistry.ResetShownHistory();
            RoyalDecree.ResetShownHistory();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Party = new BaaroForce.Party.Party(new List<Character>(), maximumPartySize: 4);
            ActRun = new ActRunState();
        }

        /// <summary>Returns true if the member was added, false if the party is full.</summary>
        public bool AddMember(Character character)
        {
            if (Party.Members.Count >= Party.MaximumPartySize)
                return false;

            Party.Members.Add(character);
            Debug.Log($"[PartyManager] Added '{character.CharacterName}'. Party: {Party.Members.Count}/{Party.MaximumPartySize}");
            return true;
        }

        public void ClearParty() => Party.Members.Clear();
    }
}
