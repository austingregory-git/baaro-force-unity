using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BaaroForce.Keywords
{
    /// <summary>
    /// Central registry mapping keyword names to their <see cref="Keyword"/> definitions.
    ///
    /// Ability descriptions embed keywords using the inline syntax <c>[KeywordName]</c>,
    /// for example: "[Regen] 1 + 0.25 x [Level] health points per turn".
    /// The registry can format that raw string into TextMeshPro rich-text and extract
    /// the referenced keywords so tooltips can display their definitions.
    /// </summary>
    public static class KeywordRegistry
    {
        private static readonly Dictionary<string, Keyword> _all =
            new Dictionary<string, Keyword>
            {
                { "Fear",  new Fear()  },
                { "Regen", new Regen() },
                { "Level", new Level() },
            };

        // ------------------------------------------------------------------ //
        // Registration                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Registers a keyword so it is recognised during parsing.</summary>
        public static void Register(Keyword keyword)
        {
            if (keyword != null)
                _all[keyword.name] = keyword;
        }

        // ------------------------------------------------------------------ //
        // Lookup                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the <see cref="Keyword"/> for <paramref name="name"/>, or null.</summary>
        public static Keyword Get(string name)
        {
            _all.TryGetValue(name, out Keyword kw);
            return kw;
        }

        // ------------------------------------------------------------------ //
        // Description helpers                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Replaces every <c>[KeywordName]</c> token in <paramref name="raw"/> with
        /// a TextMeshPro colour-tag span using the keyword's defined colour.
        /// Unrecognised tokens are emitted as plain text (brackets stripped).
        /// </summary>
        public static string FormatDescription(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            return Regex.Replace(raw, @"\[(\w+)\]", m =>
            {
                string kwName = m.Groups[1].Value;
                Keyword kw = Get(kwName);
                if (kw == null) return kwName;
                return $"<color=#{ColorUtility.ToHtmlStringRGB(kw.color)}><b>{kwName}</b></color>";
            });
        }

        /// <summary>
        /// Returns every distinct <see cref="Keyword"/> referenced in <paramref name="raw"/>,
        /// preserving first-occurrence order.
        /// </summary>
        public static List<Keyword> ExtractKeywords(string raw)
        {
            var result = new List<Keyword>();
            if (string.IsNullOrEmpty(raw)) return result;
            foreach (Match m in Regex.Matches(raw, @"\[(\w+)\]"))
            {
                Keyword kw = Get(m.Groups[1].Value);
                if (kw != null && !result.Contains(kw))
                    result.Add(kw);
            }
            return result;
        }
    }
}
