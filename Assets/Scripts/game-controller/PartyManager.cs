using System.Collections.Generic;
using UnityEngine;
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

        /// <summary>The Realm chosen for this game session. Null until set by MainMenu.</summary>
        public Realm? CurrentRealm { get; private set; }

        public void SetRealm(Realm realm) => CurrentRealm = realm;

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
