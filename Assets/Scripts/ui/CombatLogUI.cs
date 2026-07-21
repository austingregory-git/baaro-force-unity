using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Statuses;

namespace BaaroForce.UI
{
    /// <summary>
    /// Bottom-left scrolling combat log, built in the same chassis/rivet style as the rest
    /// of the Combat HUD (see CombatHud.uss). Rather than duplicating logic at every attack/
    /// spell/passive call site, this listens directly to Unity's own log stream
    /// (<see cref="Application.logMessageReceived"/>) — every combat system in this project
    /// already writes a human-readable <c>Debug.Log($"[Source] '{Actor}' did X to '{Target}'...")</c>
    /// line at the moment something happens, so this filters that stream down to the
    /// player-relevant subset and re-colours it using the same palettes as the floating
    /// combat text (<see cref="CombatTextColors"/>) and ability tooltips (KeywordRegistry).
    ///
    /// New spells/passives need no extra wiring to appear here — they're picked up
    /// automatically as long as they follow the existing "[Tag] '...' " logging convention.
    /// </summary>
    public class CombatLogUI : MonoBehaviour
    {
        private const int MaxEntries = 150;

        // Tags whose Debug.Log output is entirely internal/non-combat and should never
        // appear in the log (unrelated placeholder logging, or per-NPC chatter that's
        // already implied by "Enemy turn started" / the attack lines themselves).
        private static readonly HashSet<string> ExcludedTags = new HashSet<string>
        {
            "GameRunner", "AggressiveNpcAI", "RallyStatus", "PartyManager",
        };

        // TurnManager's own log lines are a mix of real combat outcomes and internal
        // bookkeeping ("Checking...", validation failures already shown via the warning
        // toast, raw tile coordinates). Only lines matching one of these are combat-log
        // worthy; everything else from that tag is noise.
        private static readonly string[] TurnManagerAllowSubstrings =
        {
            "turn started",
            "Executing start-of-combat passive",
            "Executing passive ability",
            "All allies defeated",
            "All enemies defeated",
            "attacks '",
            "has been defeated",
        };

        private static readonly Regex TagRegex        = new Regex(@"^\[(.+?)\]\s*(.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex QuotedNameRegex  = new Regex(@"'([^']+)'", RegexOptions.Compiled);
        private static readonly Regex DamageRegex      = new Regex(@"\b(\d+)\s+(?:(Physical|Magical|Fire|Water|Earth|Wind|Dark|Light)\s+)?damage\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ManaGainRegex    = new Regex(@"\bgained\s+\d+\s+mana\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HealGainRegex    = new Regex(@"\bgained\s+\d+\s+HP\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ShieldGainRegex  = new Regex(@"\b\d+\s+shield points\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GoldGainRegex    = new Regex(@"\bsteals\s+\d+\s+gold\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Reuse the exact same buff/debuff colours the floating combat text and status
        // chips already use, rather than inventing a parallel palette.
        private static readonly Color BuffColor   = CombatTextColors.ForStatusEffect(StatusEffect.StatusEffectType.Buff);
        private static readonly Color DebuffColor = CombatTextColors.ForStatusEffect(StatusEffect.StatusEffectType.Debuff);
        private static readonly Color ShieldColor = new Color(0.55f, 0.75f, 0.95f);

        private static readonly Dictionary<string, Color> NamedStatusColors =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                { "Fear",          DebuffColor },
                { "Root",          DebuffColor },
                { "Silence",       DebuffColor },
                { "Regen",         BuffColor   },
                { "Dodge",         BuffColor   },
                { "Bubble Shield", BuffColor   },
                { "Invisible",     BuffColor   },
            };

        private const string DefeatHex = "E36E6B"; // matches .fight-result-title-loss
        private const string WinHex    = "E8C25E"; // matches .fight-result-title-win
        private const string TurnHex   = "C9A35A"; // matches the gold rivet/accent colour

        private VisualElement _panel;
        private ScrollView _scroll;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()      => BuildPanel();
        private void OnEnable()   => Application.logMessageReceived += HandleLog;
        private void OnDisable()  => Application.logMessageReceived -= HandleLog;
        private void OnDestroy()  => _panel?.RemoveFromHierarchy();

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildPanel()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[CombatLogUI] No UIDocument found in scene.");
                return;
            }

            _panel = new VisualElement();
            _panel.AddToClassList("combat-log-panel");
            _panel.pickingMode = PickingMode.Ignore;

            var chassis = new VisualElement();
            chassis.AddToClassList("chassis");
            chassis.AddToClassList("combat-log-chassis");
            _panel.Add(chassis);

            chassis.Add(MakeRivet("rivet-tl"));
            chassis.Add(MakeRivet("rivet-tr"));
            chassis.Add(MakeRivet("rivet-bl"));
            chassis.Add(MakeRivet("rivet-br"));

            var title = new Label("COMBAT LOG");
            title.AddToClassList("combat-log-title");
            chassis.Add(title);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.AddToClassList("combat-log-scroll");
            _scroll.pickingMode = PickingMode.Ignore;
            chassis.Add(_scroll);

            doc.rootVisualElement.Add(_panel);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        // ── Log filtering ────────────────────────────────────────────────────

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Log) return; // warnings are validation failures, already shown via WarningToastUI

            Match tagMatch = TagRegex.Match(message);
            if (!tagMatch.Success) return; // untagged Debug.Log calls aren't combat output

            string tag  = tagMatch.Groups[1].Value;
            string body = tagMatch.Groups[2].Value;
            if (string.IsNullOrEmpty(body) || ExcludedTags.Contains(tag)) return;

            if (tag == "TurnManager" && !ContainsAny(body, TurnManagerAllowSubstrings)) return;

            Append(Colorize(body));
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            foreach (string needle in needles)
                if (haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        // ── Colouring ────────────────────────────────────────────────────────

        private static string Colorize(string body)
        {
            string text = QuotedNameRegex.Replace(body, "<b>$1</b>");

            text = DamageRegex.Replace(text, m =>
            {
                string typeWord = m.Groups[2].Success ? m.Groups[2].Value : "Physical";
                string hex = Enum.TryParse(typeWord, true, out SpellType type)
                    ? ColorUtility.ToHtmlStringRGB(CombatTextColors.ForDamageType(type))
                    : ColorUtility.ToHtmlStringRGB(CombatTextColors.ForDamageType(SpellType.Physical));
                return $"<color=#{hex}>{m.Value}</color>";
            });

            text = ManaGainRegex.Replace(text, m => $"<color=#{ColorUtility.ToHtmlStringRGB(CombatTextColors.ManaColor)}>{m.Value}</color>");
            text = HealGainRegex.Replace(text, m => $"<color=#{ColorUtility.ToHtmlStringRGB(CombatTextColors.HealColor)}>{m.Value}</color>");
            text = GoldGainRegex.Replace(text, m => $"<color=#{ColorUtility.ToHtmlStringRGB(CombatTextColors.GoldColor)}>{m.Value}</color>");
            text = ShieldGainRegex.Replace(text, m => $"<color=#{ColorUtility.ToHtmlStringRGB(ShieldColor)}>{m.Value}</color>");

            foreach (var kvp in NamedStatusColors)
            {
                string hex = ColorUtility.ToHtmlStringRGB(kvp.Value);
                text = Regex.Replace(text, $@"\b{Regex.Escape(kvp.Key)}\b", m => $"<color=#{hex}>{m.Value}</color>", RegexOptions.IgnoreCase);
            }

            if (body.IndexOf("defeated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                body.IndexOf("Game over", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"<color=#{DefeatHex}>{text}</color>";

            if (body.IndexOf("Fight won", StringComparison.OrdinalIgnoreCase) >= 0)
                return $"<color=#{WinHex}>{text}</color>";

            if (body.EndsWith("turn started.", StringComparison.OrdinalIgnoreCase))
                return $"<color=#{TurnHex}><i>{text}</i></color>";

            return text;
        }

        // ── Rendering ────────────────────────────────────────────────────────

        private void Append(string richText)
        {
            if (_scroll == null) return;

            var entry = new Label(richText);
            entry.AddToClassList("combat-log-entry");
            _scroll.Add(entry);

            while (_scroll.contentContainer.childCount > MaxEntries)
                _scroll.contentContainer.RemoveAt(0);

            _scroll.schedule.Execute(() => _scroll.ScrollTo(entry));
        }
    }
}
