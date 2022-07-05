using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassStats : MonoBehaviour
{
    public int hp;
    public int str;
    public int mr;
    public int def;
    public int magic;
    public int dex;

    public ClassStats(int hp, int str, int def, int mr, int magic, int dex)
    {
        this.hp = hp;
        this.str = str;
        this.mr = mr;
        this.def = def;
        this.magic = magic;
        this.dex = dex;
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
