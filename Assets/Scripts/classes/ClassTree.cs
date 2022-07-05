using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassTree : MonoBehaviour
{
    public Dictionary<string, AbstractClass> classMap = new Dictionary<string, AbstractClass>();
    public Dictionary<string, AbstractClass> tierOneMap = new Dictionary<string, AbstractClass>();
    public Dictionary<string, AbstractClass> tierTwoMap = new Dictionary<string, AbstractClass>();
    public Dictionary<string, AbstractClass> tierThreeMap = new Dictionary<string, AbstractClass>();
    public Dictionary<string, AbstractClass> tierFourMap = new Dictionary<string, AbstractClass>();
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

    public void add(AbstractClass c)
    {
        classMap.Add(c.name, c);
    }

    public void addTierOneClasses()
    {
        add(new Mage());
        tierOneMap.Add("Mage", new Mage());
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
