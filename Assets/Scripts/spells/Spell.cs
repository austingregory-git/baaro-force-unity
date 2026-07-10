using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spell
{
    public string name;
    public string description;
    public int cost;
    public int range;
    public int area;
    public int cooldown;

    public Spell(string name, string description, int cost, int range, int area, int cooldown)
    {
        this.name = name;
        this.description = description;
        this.cost = cost;
        this.range = range;
        this.area = area;
        this.cooldown = cooldown;
    }
    public Spell(string name, string description)
    {
        this.name = name;
        this.description = description;
        this.cost = 0;
        this.range = 0;
        this.area = 0;
        this.cooldown = 999;
    }
}
public enum SpellType
{
    FIRE,
    WATER,
    EARTH,
    WIND,
    DARK,
    LIGHT,
    PHYSICAL
}
