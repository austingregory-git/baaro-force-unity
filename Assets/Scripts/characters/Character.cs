using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public CharacterClass characterClass { get; set; }
    public string characterName { get; set; }

    public Character(CharacterClass characterClass, string characterName)
    {
        this.characterClass = characterClass;
        this.characterName = characterName;
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