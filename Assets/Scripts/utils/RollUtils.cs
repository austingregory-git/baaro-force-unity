using System;
using System.Collections.Generic;

using UnityEngine;

public class RollUtils : MonoBehaviour
{
    public int RollD6(System.Random random)
    {
        return random.Next(0, 6);
    }

    
    public int RollD20(System.Random random)
    {
        return random.Next(0, 20);
    }

    public int RollD100(System.Random random)
    {
        return random.Next(0, 100);
    }

}
