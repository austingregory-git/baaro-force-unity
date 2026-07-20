using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.Spells;

namespace BaaroForce.UI
{
    /// <summary>
    /// Left-side spell action panel shown whenever a party character is selected
    /// on the map during the player turn. Same chassis/rivet style as the Combat HUD
    /// and ActionPanelUI (see CombatHud.uss) — built in code, no prefabs required.
    ///
    /// Each spell appears as a clickable row showing its mana cost, or a cooldown/
    /// once-per-fight badge in place of the cost when it isn't currently castable.
    /// Hovering a row shows the full description (plus live cooldown state) via
    /// <see cref="TooltipSystem"/>.
    /// </summary>
    public class SpellPanelUI : MonoBehaviour
    {
        /// <summary>Fired when the player clicks a castable spell row.</summary>
        public Action<Spell> OnSpellSelected;

        /// <summary>Fired when the player clicks the Back button.</summary>
        public Action OnBackClicked;

        /// <summary>
        /// Supplies the number of rounds left before a spell is castable again
        /// (0 = usable now, int.MaxValue = used, once-per-fight). Wired by TurnManager.
        /// </summary>
        public Func<Character, Spell, int> GetCooldownRemaining;

        private VisualElement _panel;
        private VisualElement _list;
        private Label _title;

        /// <summary>True while the panel is actively displayed.</summary>
        public bool IsVisible => _panel != null && _panel.style.display == DisplayStyle.Flex;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()     => BuildPanel();
        private void OnDestroy() => _panel?.RemoveFromHierarchy();

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildPanel()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[SpellPanelUI] No UIDocument found in scene.");
                return;
            }

            _panel = new VisualElement();
            _panel.AddToClassList("hud-panel");
            _panel.AddToClassList("action-panel");
            _panel.style.display = DisplayStyle.None;

            var chassis = new VisualElement();
            chassis.AddToClassList("chassis");
            chassis.AddToClassList("action-chassis");
            _panel.Add(chassis);

            chassis.Add(MakeRivet("rivet-tl"));
            chassis.Add(MakeRivet("rivet-tr"));
            chassis.Add(MakeRivet("rivet-bl"));
            chassis.Add(MakeRivet("rivet-br"));

            var backRow = new VisualElement();
            backRow.AddToClassList("spell-row");
            var backLabel = new Label("← Back");
            backLabel.AddToClassList("spell-name");
            backRow.Add(backLabel);
            backRow.RegisterCallback<ClickEvent>(_ => OnBackClicked?.Invoke());
            chassis.Add(backRow);

            _title = new Label();
            _title.AddToClassList("unit-name");
            _title.AddToClassList("action-title");
            chassis.Add(_title);

            _list = new VisualElement();
            _list.AddToClassList("action-list");
            chassis.Add(_list);

            doc.rootVisualElement.Add(_panel);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Populates the panel with the character's spells and shows it.</summary>
        public void Show(Character character)
        {
            if (_panel == null) return;
            _list.Clear();

            if (character?.CharacterSpells == null || character.CharacterSpells.Count == 0)
            {
                _panel.style.display = DisplayStyle.None;
                return;
            }

            _title.text = $"{character.CharacterName}'s Spells";

            foreach (Spell spell in character.CharacterSpells)
                _list.Add(BuildSpellRow(spell, character));

            _panel.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            if (_panel != null) _panel.style.display = DisplayStyle.None;
        }

        // ── Row builder ──────────────────────────────────────────────────────

        private VisualElement BuildSpellRow(Spell spell, Character character)
        {
            int cooldownRemaining = GetCooldownRemaining?.Invoke(character, spell) ?? 0;
            bool onCooldown = cooldownRemaining > 0;
            bool canAfford  = character.CharacterStats.Mana >= spell.ManaCost;
            bool usable     = canAfford && !onCooldown;

            var row = new VisualElement();
            row.AddToClassList("spell-row");
            if (!usable) row.AddToClassList("spell-row--disabled");

            var nameLabel = new Label(spell.Name);
            nameLabel.AddToClassList("spell-name");
            if (!usable) nameLabel.AddToClassList("spell-name--disabled");
            row.Add(nameLabel);

            if (spell.OncePerFight)
                row.Add(MakeBadge("1x", "spell-badge-once"));

            if (cooldownRemaining == int.MaxValue)
                row.Add(MakeBadge("Used", "spell-badge-used"));
            else if (onCooldown)
                row.Add(MakeBadge($"CD {cooldownRemaining}", "spell-badge-cooldown"));
            else if (spell.ManaCost > 0)
                row.Add(MakeBadge($"{spell.ManaCost}MP", "spell-badge-cost"));

            // Always register the click, even when disabled — TurnManager.ActivateSpell
            // re-checks affordability/cooldown itself and surfaces a warning toast when it
            // can't be cast, instead of the click just silently doing nothing.
            Spell capturedSpell = spell;
            row.RegisterCallback<ClickEvent>(_ => OnSpellSelected?.Invoke(capturedSpell));

            string summaryBody  = BuildTooltipBody(spell, character, cooldownRemaining, detailed: false);
            string detailedBody = BuildTooltipBody(spell, character, cooldownRemaining, detailed: true);
            row.RegisterCallback<PointerEnterEvent>(_ => TooltipSystem.Instance?.Show(spell.Name, summaryBody, detailedBody));
            row.RegisterCallback<PointerLeaveEvent>(_ => TooltipSystem.Instance?.Hide());

            return row;
        }

        private static Label MakeBadge(string text, string variantClass)
        {
            var badge = new Label(text);
            badge.AddToClassList("spell-badge");
            badge.AddToClassList(variantClass);
            return badge;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string BuildTooltipBody(Spell spell, Character character, int cooldownRemaining, bool detailed)
        {
            // Pass the raw (un-formatted) text through — TooltipSystem itself resolves
            // [Keyword] tokens and appends the glossary; formatting it here too would
            // strip the brackets before TooltipSystem sees them and silently drop the
            // glossary.
            string core = detailed
                ? (spell.GetDetailedDescription(character) ?? spell.GetSummary(character))
                : spell.GetSummary(character);

            var sb = new StringBuilder();
            sb.Append(core);

            if (spell.TargetType == SpellTargetType.Self)
                sb.Append("\n<i>Targets self — no aim required.</i>");
            else if (spell.Range > 0)
                sb.Append($"\nRange: {spell.Range} tiles");

            if (spell.ManaCost > 0)
                sb.Append($"\nMana cost: {spell.ManaCost}");

            if (spell.OncePerFight)
                sb.Append("\n<i>Once per fight.</i>");
            else if (spell.Cooldown > 0 && spell.Cooldown < 999)
                sb.Append($"\nCooldown: {spell.Cooldown} turn(s)");

            if (cooldownRemaining == int.MaxValue)
                sb.Append("\n<color=#e07a7a>Already used this fight.</color>");
            else if (cooldownRemaining > 0)
                sb.Append($"\n<color=#e07a7a>On cooldown: {cooldownRemaining} round(s) left.</color>");

            return sb.ToString();
        }
    }
}
