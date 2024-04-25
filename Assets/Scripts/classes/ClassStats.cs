using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassStats : MonoBehaviour
{
    public int hp;
    public int str;
    public int def;
    public int magic;
    public int dex;
    public int mana;

    public ClassStats(int hp, int str, int def, int magic, int dex, int mana)
    {
        this.hp = hp;
        this.str = str;
        this.def = def;
        this.magic = magic;
        this.dex = dex;
        this.mana = mana;
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
