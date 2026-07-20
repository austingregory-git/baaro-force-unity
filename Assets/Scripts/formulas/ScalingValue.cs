using System.Collections.Generic;
using BaaroForce.Characters;

namespace BaaroForce.Formulas
{
    /// <summary>
    /// A single named computed number (damage, healing, duration, ...) built up from
    /// labelled contributing terms. Spells and passives build one of these instead of
    /// inlining arithmetic, so the exact same computation both drives the actual
    /// effect (<c>Total</c>) and describes itself in a tooltip breakdown (<c>Terms</c>) —
    /// one source of truth, no risk of the two drifting apart.
    /// </summary>
    public sealed class ScalingValue
    {
        /// <summary>A single contributing term. May itself be a composite (e.g. "Total
        /// Attack") expanded into child terms for the detailed breakdown view.</summary>
        public sealed class Term
        {
            public readonly string Label;
            public readonly int Amount;
            public readonly List<Term> Children = new List<Term>();

            public Term(string label, int amount)
            {
                Label  = label;
                Amount = amount;
            }
        }

        /// <summary>Name of what this value represents, e.g. "Damage", "Fear Duration".</summary>
        public readonly string Label;
        public readonly List<Term> Terms = new List<Term>();
        public int Total { get; private set; }

        public ScalingValue(string label)
        {
            Label = label;
        }

        /// <summary>Adds a flat contributing term.</summary>
        public ScalingValue Add(string label, int amount)
        {
            Terms.Add(new Term(label, amount));
            Total += amount;
            return this;
        }

        /// <summary>
        /// Adds a "Total Attack" term expanded into its Base Attack / Attack Bonus
        /// components, for spells whose damage scales off the caster's basic attack.
        /// </summary>
        public ScalingValue AddTotalAttack(CharacterStats stats, string label = "Total Attack")
        {
            var term = new Term(label, stats.TotalAttack);
            term.Children.Add(new Term("Base Attack", stats.BaseAttack));
            if (stats.AttackBonus != 0)
                term.Children.Add(new Term("Attack Bonus", stats.AttackBonus));

            Terms.Add(term);
            Total += stats.TotalAttack;
            return this;
        }
    }
}
