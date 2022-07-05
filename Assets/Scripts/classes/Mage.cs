using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mage : AbstractClass
{
    public static new AbstractClass.Tier tier = AbstractClass.Tier.TIER_ONE;
    public static new string classID = "Mage";
    public static new List<string> promotions = ClassTree.getPromotions(classID);
    //public static final List<AbstractSpell> spells = SpellTree.getSpells(classID);
    public static new List<AbstractSpell> spells = null;
    public static new ClassStats classStats = new ClassStats(30, 20, 20, 60, 80, 40);


    public Mage() : base(classID, tier, ClassTree.getPromotions(classID), null, classStats)
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
