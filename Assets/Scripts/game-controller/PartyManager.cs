using System.Collections.Generic;
using UnityEngine;
using BaaroForce.ActMap;
using BaaroForce.Characters;
using BaaroForce.Party;
using BaaroForce.Relics;

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

        public void SetRealm(Realm realm) => CurrentRealm = realm;

        /// <summary>Advances progression to the next fight's depth.</summary>
        public void AdvanceDepth() => Depth++;

        /// <summary>Resets the party and progression for a fresh run (e.g. after Game Over).</summary>
        public void ResetForNewRun()
        {
            ClearParty();
            Depth = 1;
            Relics.Clear();
            ActRun = new ActRunState();
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
