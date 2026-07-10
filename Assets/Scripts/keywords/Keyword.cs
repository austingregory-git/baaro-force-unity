using UnityEngine;

namespace BaaroForce.Keywords
{
    public class Keyword
    {
        public string name;
        public string description;
        public Color color { get; set; }

        public Keyword(string name, string description, Color color)
        {
            this.name = name;
            this.description = description;
            this.color = color;
        }
    }
}