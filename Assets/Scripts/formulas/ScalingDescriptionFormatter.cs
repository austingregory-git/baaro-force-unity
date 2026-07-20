using System.Text;

namespace BaaroForce.Formulas
{
    /// <summary>
    /// Renders a description template + its computed <see cref="ScalingValue"/>s into
    /// the two forms a tooltip needs: a clean one-line summary with just the totals
    /// plugged in, and a detailed breakdown listing every contributing term.
    /// Shared by <c>Spell</c> and <c>PassiveAbility</c> so both ability hierarchies get
    /// identical formatting without duplicating this logic.
    /// </summary>
    public static class ScalingDescriptionFormatter
    {
        /// <summary>
        /// Formats <paramref name="descriptionTemplate"/> with each value's <c>Total</c>
        /// substituted for its <c>{N}</c> placeholder. Returns the template unchanged
        /// when there are no values (nothing to compute).
        /// </summary>
        public static string GetSummary(string descriptionTemplate, ScalingValue[] values)
        {
            if (values == null || values.Length == 0) return descriptionTemplate;

            var totals = new object[values.Length];
            for (int i = 0; i < values.Length; i++)
                totals[i] = values[i].Total;

            return string.Format(descriptionTemplate, totals);
        }

        /// <summary>
        /// Returns the summary text followed by a labelled term breakdown for every
        /// value, or null when there are no values (i.e. nothing beyond the summary
        /// to show — callers should fall back to the summary alone).
        /// </summary>
        public static string GetDetailedDescription(string descriptionTemplate, ScalingValue[] values)
        {
            if (values == null || values.Length == 0) return null;

            var sb = new StringBuilder(GetSummary(descriptionTemplate, values));
            foreach (ScalingValue value in values)
            {
                sb.Append("\n\n<b>").Append(value.Label).Append("</b>");
                foreach (ScalingValue.Term term in value.Terms)
                    AppendTerm(sb, term, indent: 1);
                sb.Append("\n  <i>Total: ").Append(value.Total).Append("</i>");
            }
            return sb.ToString();
        }

        private static void AppendTerm(StringBuilder sb, ScalingValue.Term term, int indent)
        {
            string pad = new string(' ', indent * 2);
            sb.Append('\n').Append(pad).Append(term.Label).Append(": ").Append(term.Amount);
            foreach (ScalingValue.Term child in term.Children)
                AppendTerm(sb, child, indent + 1);
        }
    }
}
