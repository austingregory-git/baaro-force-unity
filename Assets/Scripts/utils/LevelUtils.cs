using System;
using System.Collections.Generic;

using UnityEngine;

public class LevelUtils : MonoBehaviour
{

    private System.Random random = new System.Random();
    private ClassGenerator classGenerator = new ClassGenerator();

    private RollUtils rollUtils = new RollUtils();

    // Start is called before the first frame update
    void Start()
    {

        for(int i=0; i < 100; i++) {

            int level = random.Next(0, 30);
            ClassStats classStats = classGenerator.GenerateClassStats(level);
            Debug.Log("Before Level --- " + "Level: " + level + " Hp: " + classStats.hp + " Str: " + classStats.str + " Def: " + classStats.def + " Magic: " + classStats.magic + " Dex: " + classStats.dex + " Mana: " + classStats.mana);
            classStats = LevelUp(classStats, 1);
            level++;
            Debug.Log("After Level --- " + "Level: " + level + " Hp: " + classStats.hp + " Str: " + classStats.str + " Def: " + classStats.def + " Magic: " + classStats.magic + " Dex: " + classStats.dex + " Mana: " + classStats.mana);
        }
    }

    ClassStats LevelUp(ClassStats classStats, int levelsIncremented)
    {
        int statsToDistribute = 3;

        for(int i=0; i < levelsIncremented; i++) {
            for(int j=0; j < statsToDistribute; j++) {
                int roll = rollUtils.RollD6(random);
                switch (roll) {
                    case 1: 
                        classStats.hp++;
                        break;
                    case 2:
                        classStats.str++;
                        break;
                    case 3:
                        classStats.def++;
                        break;
                    case 4: 
                        classStats.magic++;
                        break;
                    case 5:
                        classStats.dex++;
                        break;
                    case 6:
                        classStats.mana++;
                        break;
                }
            }
        }
        return classStats;
    }
}
