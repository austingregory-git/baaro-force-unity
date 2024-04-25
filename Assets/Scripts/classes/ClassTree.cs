using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassTree : MonoBehaviour
{
    public Dictionary<string, CharacterClass> classMap = new Dictionary<string, CharacterClass>();
    public List<string> tierOneClasses;
    public List<string> tierTwoClasses;
    public List<string> tierThreeClasses;
    public List<string> tierFourClasses;
    public static Dictionary<string, List<string>> promotionsMap = new Dictionary<string, List<string>>();

    public ClassTree()
    {
        initialize();
    }

    public void initialize()
    {
        addTierOneClasses();
        addTierTwoClasses();
        addTierThreeClasses();
        addTierFourClasses();
    }

    private void addTierFourClasses()
    {
    }

    private void addTierThreeClasses()
    {
    }

    private void addTierTwoClasses()
    {
    }

    public void add(CharacterClass c)
    {
        classMap.Add(c.name, c);
    }

    public void addTierOneClasses()
    {
        tierOneClasses.AddRange(new List<string> { "Mage", "Warrior", "Rogue", "Cleric" });
        promotionsMap.Add("Mage", new List<string> { "DarkMage", "LightMage", "EarthMage", "FireMage", "WaterMage", "WindMage", "Scholar" });
    }

    public static List<string> getPromotions(string classID)
    {
        return promotionsMap[classID];
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
