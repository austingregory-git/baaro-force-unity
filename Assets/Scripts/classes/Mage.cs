using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mage : CharacterClass
{
    public static new Tier tier = Tier.TIER_ONE;
    public static new string classID = "Mage";
    public static new List<string> promotions = ClassTree.getPromotions(classID);
    public static new List<Spell> spells = null;
    public static new ClassStats classStats = new ClassStats(30, 20, 20, 80, 40, 20);

    // Add this constructor
    public Mage(List<string> promotions, List<Spell> spells, ClassStats classStats) 
        : base(classID, tier, promotions, spells, classStats)
    {

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