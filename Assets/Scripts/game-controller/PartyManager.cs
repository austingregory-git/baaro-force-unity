using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Party;

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

    public Party Party { get; private set; }

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
        Party = new Party(new List<Character>(), maximumPartySize: 4);
    }

    /// <summary>Returns true if the member was added, false if the party is full.</summary>
    public bool AddMember(Character character)
    {
        if (Party.members.Count >= Party.maximumPartySize)
            return false;

        Party.members.Add(character);
        Debug.Log($"[PartyManager] Added '{character.characterName}'. Party: {Party.members.Count}/{Party.maximumPartySize}");
        return true;
    }

    public void ClearParty() => Party.members.Clear();
}
