using UnityEngine;

namespace BaaroForce.Keywords
{
    public class Keyword
    {
        public string Name;
        public string Description;
        public Color Color { get; set; }

        public Keyword(string name, string description, Color color)
        {
            this.Name = name;
            this.Description = description;
            this.Color = color;
        }

        public Keyword(string name, Color color)
        {
            this.Name = name;
            this.Color = color;
        }
    }
}