using System;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Characters;

namespace BaaroForce.UI
{
    /// <summary>
    /// Left-side character action panel, built in the same chassis/rivet style as the
    /// Combat HUD (see CharacterHudController / CombatHud.uss). Shown whenever a party
    /// member is selected during the player turn, offering Movement, Attack, Spells,
    /// Items, and Wait as clickable alternatives to the keyboard shortcuts.
    ///
    /// Built entirely in code — no prefabs or scene setup required. Reuses whichever
    /// UIDocument/PanelSettings is already in the scene (the one CharacterHudController
    /// sets up), so CombatHud.uss is already applied by the time this adds elements to it.
    /// </summary>
    public class ActionPanelUI : MonoBehaviour
    {
        public Action OnMoveClicked;
        public Action OnAttackClicked;
        public Action OnSpellsClicked;
        public Action OnItemsClicked;
        public Action OnWaitClicked;

        private VisualElement _panel;
        private Label _title;
        private Button _spellsButton;

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
                Debug.LogWarning("[ActionPanelUI] No UIDocument found in scene.");
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

            _title = new Label();
            _title.AddToClassList("unit-name");
            _title.AddToClassList("action-title");
            chassis.Add(_title);

            var list = new VisualElement();
            list.AddToClassList("action-list");
            chassis.Add(list);

            list.Add(MakeButton("Movement", "action-btn-move",   () => OnMoveClicked?.Invoke()));
            list.Add(MakeButton("Attack",   "action-btn-attack", () => OnAttackClicked?.Invoke()));
            _spellsButton = MakeButton("Spells", "action-btn-spells", () => OnSpellsClicked?.Invoke());
            list.Add(_spellsButton);
            list.Add(MakeButton("Items",    "action-btn-items",  () => OnItemsClicked?.Invoke()));
            list.Add(MakeButton("Wait",     "action-btn-wait",   () => OnWaitClicked?.Invoke()));

            doc.rootVisualElement.Add(_panel);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        private static Button MakeButton(string label, string variantClass, Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("action-btn");
            btn.AddToClassList(variantClass);
            return btn;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Rebuilds and shows the panel for <paramref name="character"/>.</summary>
        public void Show(Character character)
        {
            if (_panel == null) return;
            _title.text = character.CharacterName;
            _spellsButton.SetEnabled(character.CharacterSpells?.Count > 0);
            _panel.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            if (_panel != null) _panel.style.display = DisplayStyle.None;
        }
    }
}
