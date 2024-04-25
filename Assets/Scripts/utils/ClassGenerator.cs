using System;
using System.Collections.Generic;

using UnityEngine;

public class ClassGenerator : MonoBehaviour
{

    private System.Random random = new System.Random();

    // Start is called before the first frame update
    void Start()
    {
        //testing... call this 100 times
        for(int i=0; i < 100; i++) {

            int level = random.Next(0, 30);
            ClassStats classStats= GenerateClassStats(level);

            Debug.Log("Level: " + level);
            Debug.Log("Hp: " + classStats.hp);
            Debug.Log("Str: " + classStats.str);
            Debug.Log("Def: " + classStats.def);
            Debug.Log("Magic: " + classStats.magic);
            Debug.Log("Dex: " + classStats.dex);
            Debug.Log("Mana: " + classStats.mana);
        }
    }

    public ClassStats GenerateClassStats(int level)
    {
        int lowerBound = (level/2)+4;
        int upperBound = (level/2)+12;

        int hp = random.Next(lowerBound, upperBound);
        int str = random.Next(lowerBound, upperBound);
        int def = random.Next(lowerBound, upperBound);
        int magic = random.Next(lowerBound, upperBound);
        int dex = random.Next(lowerBound, upperBound);
        int mana = random.Next(lowerBound, upperBound);

        ClassStats classStats = new ClassStats(hp, str, def, magic, dex, mana);
        return classStats;
    }
}
