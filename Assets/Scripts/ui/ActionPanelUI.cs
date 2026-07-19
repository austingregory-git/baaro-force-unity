using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaaroForce.Characters;

namespace BaaroForce.UI
{
    /// <summary>
    /// Left-side character action panel.  Shown whenever a party member is
    /// selected during the player turn, offering Movement, Attack, Spells and
    /// Items as clickable alternatives to the keyboard shortcuts.
    ///
    /// Built entirely in code — no prefabs or scene setup required.
    /// Uses the same background sprite as <see cref="SpellPanelUI"/>.
    /// </summary>
    public class ActionPanelUI : MonoBehaviour
    {
        public Action OnMoveClicked;
        public Action OnAttackClicked;
        public Action OnSpellsClicked;
        public Action OnItemsClicked;

        private GameObject _canvasGo;
        private GameObject _panelRoot;
        private Transform  _listParent;

        /// <summary>True while the panel is actively displayed.</summary>
        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        // ── Layout constants (1280×720 reference pixels) ─────────────────────
        private const float PanelWidth  = 200f;
        private const float PaddingH    = 10f;
        private const float PaddingV    = 10f;
        private const float TitleHeight = 28f;
        private const float RowHeight   = 40f;
        private const float RowSpacing  = 5f;
        private const string FontPath   = "Fonts/Baloo2-Bold SDF";

        // Action accent colours
        private static readonly Color ColMove   = new Color(0.20f, 0.50f, 0.90f);
        private static readonly Color ColAttack = new Color(0.85f, 0.25f, 0.15f);
        private static readonly Color ColSpells = new Color(0.50f, 0.10f, 0.80f);
        private static readonly Color ColItems  = new Color(0.10f, 0.60f, 0.25f);

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()     => BuildCanvas();
        private void OnDestroy() { if (_canvasGo != null) Destroy(_canvasGo); }

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildCanvas()
        {
            _canvasGo = new GameObject("[ActionPanelCanvas]");

            var canvas          = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler                 = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight   = 0.5f;

            _canvasGo.AddComponent<GraphicRaycaster>();

            // ── Panel background ─────────────────────────────────────────────
            _panelRoot = new GameObject("ActionPanel");
            _panelRoot.transform.SetParent(_canvasGo.transform, false);

            var bg     = _panelRoot.AddComponent<Image>();
            var sprite = Resources.Load<Sprite>("character_action_panel_512x512");
            if (sprite != null)
                bg.sprite = sprite;
            else
                bg.color = new Color(0.55f, 0.82f, 0.94f, 0.95f);

            var panelRect              = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin         = new Vector2(0f, 0.5f);
            panelRect.anchorMax         = new Vector2(0f, 0.5f);
            panelRect.pivot             = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition  = new Vector2(10f, 0f);
            panelRect.sizeDelta         = new Vector2(PanelWidth, 100f);

            // ── Action list container (VerticalLayoutGroup) ──────────────────
            var listGo = new GameObject("ActionList");
            listGo.transform.SetParent(_panelRoot.transform, false);

            var vlg                    = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding                = new RectOffset(
                                             Mathf.RoundToInt(PaddingH),
                                             Mathf.RoundToInt(PaddingH),
                                             Mathf.RoundToInt(PaddingV),
                                             Mathf.RoundToInt(PaddingV));
            vlg.spacing                = RowSpacing;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperLeft;

            var listRect   = listGo.GetComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero;
            listRect.anchorMax = Vector2.one;
            listRect.offsetMin = Vector2.zero;
            listRect.offsetMax = Vector2.zero;

            _listParent = listGo.transform;
            _panelRoot.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Rebuilds and shows the panel for <paramref name="character"/>.</summary>
        public void Show(Character character)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                Destroy(_listParent.GetChild(i).gameObject);

            AddTitle(character.CharacterName);

            bool hasSpells = character.CharacterSpells?.Count > 0;

            AddActionRow("Movement", ColMove,   () => OnMoveClicked?.Invoke(),   enabled: true);
            AddActionRow("Attack",   ColAttack, () => OnAttackClicked?.Invoke(), enabled: true);
            AddActionRow("Spells",   ColSpells, () => OnSpellsClicked?.Invoke(), enabled: hasSpells);
            AddActionRow("Items",    ColItems,  () => OnItemsClicked?.Invoke(),  enabled: true);

            // 1 title + 4 rows + 4 gaps between them
            float h = PaddingV * 2 + TitleHeight + 4f * RowHeight + 4f * RowSpacing;
            _panelRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, h);
            _panelRoot.SetActive(true);
        }

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        // ── Row builders ─────────────────────────────────────────────────────

        private void AddTitle(string text)
        {
            var go  = new GameObject("Title");
            go.transform.SetParent(_listParent, false);

            var tmp      = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = 13f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = new Color(0.05f, 0.15f, 0.35f);
            tmp.font      = Resources.Load<TMP_FontAsset>(FontPath);

            var le           = go.AddComponent<LayoutElement>();
            le.preferredHeight = TitleHeight;
            le.flexibleWidth   = 1f;
        }

        private void AddActionRow(string label, Color labelColor, Action onClick,
                                   bool enabled = true)
        {
            var rowGo  = new GameObject($"Action_{label}");
            rowGo.transform.SetParent(_listParent, false);

            var rowBg  = rowGo.AddComponent<Image>();
            rowBg.color = enabled
                ? new Color(1f, 1f, 1f, 0.40f)
                : new Color(0.6f, 0.6f, 0.6f, 0.22f);

            var le           = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = RowHeight;
            le.flexibleWidth   = 1f;

            var btn          = rowGo.AddComponent<Button>();
            btn.targetGraphic = rowBg;
            btn.interactable  = enabled;
            btn.onClick.AddListener(() => onClick());

            var colors              = btn.colors;
            colors.normalColor       = Color.white;
            colors.highlightedColor  = new Color(0.85f, 0.92f, 1.00f);
            colors.pressedColor      = new Color(0.65f, 0.80f, 1.00f);
            colors.disabledColor     = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            btn.colors              = colors;

            var labelGo  = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);

            var tmp      = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 13f;
            tmp.fontStyle = enabled ? FontStyles.Bold : FontStyles.Italic;
            tmp.color     = enabled ? labelColor : new Color(0.38f, 0.38f, 0.42f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font      = Resources.Load<TMP_FontAsset>(FontPath);

            var lr             = labelGo.GetComponent<RectTransform>();
            lr.anchorMin        = Vector2.zero;
            lr.anchorMax        = Vector2.one;
            lr.sizeDelta        = Vector2.zero;
            lr.anchoredPosition = Vector2.zero;
        }
    }
}
