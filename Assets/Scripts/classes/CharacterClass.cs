using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterClass : MonoBehaviour
{
    public string classID;
    public List<string> promotions;
    public List<Spell> spells;
    public ClassStats classStats;
    public CharacterClass.Tier tier;

    public CharacterClass(string classID, CharacterClass.Tier tier, List<string> promotions, List<Spell> spells, ClassStats classStats) {
        this.classID = classID;
        this.tier = tier;
        this.promotions = promotions;
        this.spells = spells;
        this.classStats = classStats;
    }

    public enum Tier {
        TIER_ONE,
        TIER_TWO,
        TIER_THREE,
        TIER_FOUR
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
