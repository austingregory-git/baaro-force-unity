using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaaroForce.Characters;
using BaaroForce.Keywords;
using BaaroForce.Spells;

namespace BaaroForce.UI
{
    /// <summary>
    /// Left-side spell action panel shown whenever a party character is selected
    /// on the map during the player turn.
    ///
    /// Each spell appears as a clickable row.  Hovering any row shows a tooltip
    /// with the full description, range, and mana cost via <see cref="TooltipSystem"/>.
    /// Rows for spells the character cannot currently afford are dimmed and
    /// non-interactable.
    ///
    /// Built entirely in code — no prefabs or scene setup required.
    /// </summary>
    public class SpellPanelUI : MonoBehaviour
    {
        /// <summary>
        /// Fired when the player clicks a spell row.
        /// Wire this to <c>TurnManager.ActivateSpell</c>.
        /// </summary>
        public Action<Spell> OnSpellSelected;

        /// <summary>Fired when the player clicks the Back button.</summary>
        public Action OnBackClicked;

        /// <summary>True while the panel is actively displayed.</summary>
        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        private GameObject _canvasGo;
        private GameObject _panelRoot;
        private Transform  _listParent;

        // ── Layout constants (in 1280×720 reference pixels) ─────────────────
        private const float PanelWidth  = 200f;
        private const float PaddingH    = 10f;
        private const float PaddingV    = 10f;
        private const float TitleHeight = 28f;
        private const float RowHeight   = 40f;
        private const float RowSpacing  = 5f;
        private const float CostWidth   = 44f;
        private const string FontPath   = "Fonts/Baloo2-Bold SDF";

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()  => BuildCanvas();
        private void OnDestroy()
        {
            if (_canvasGo != null) Destroy(_canvasGo);
        }

        // ── Canvas / panel construction ──────────────────────────────────────

        private void BuildCanvas()
        {
            _canvasGo = new GameObject("[SpellPanelCanvas]");

            var canvas          = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler                  = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode           = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution   = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight    = 0.5f;

            _canvasGo.AddComponent<GraphicRaycaster>();

            // ── Panel background ─────────────────────────────────────────────
            _panelRoot = new GameObject("SpellPanel");
            _panelRoot.transform.SetParent(_canvasGo.transform, false);

            var bg      = _panelRoot.AddComponent<Image>();
            var sprite  = Resources.Load<Sprite>("character_action_panel_512x512");
            if (sprite != null)
                bg.sprite = sprite;
            else
                bg.color = new Color(0.55f, 0.82f, 0.94f, 0.95f);

            var panelRect              = _panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin         = new Vector2(0f, 0.5f);
            panelRect.anchorMax         = new Vector2(0f, 0.5f);
            panelRect.pivot             = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition  = new Vector2(10f, 0f);
            panelRect.sizeDelta         = new Vector2(PanelWidth, 100f);  // height set in Show()

            // ── Spell list container (VerticalLayoutGroup) ───────────────────
            var listGo = new GameObject("SpellList");
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

        /// <summary>Populates the panel with the character's spells and shows it.</summary>
        public void Show(Character character)
        {
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                Destroy(_listParent.GetChild(i).gameObject);

            if (character?.characterSpells == null || character.characterSpells.Count == 0)
            {
                _panelRoot.SetActive(false);
                return;
            }

            AddBackButton();
            AddTitle($"{character.characterName}'s Spells");

            foreach (Spell spell in character.characterSpells)
                AddSpellRow(spell, character.characterStats.mana);

            // Children: 1 back + 1 title + N spells  →  N+1 gaps between them.
            int   n = character.characterSpells.Count;
            float h = PaddingV * 2
                      + RowHeight            // back button row
                      + TitleHeight          // title
                      + n * RowHeight        // spell rows
                      + (n + 1) * RowSpacing;

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
            var go = new GameObject("Title");
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

        private void AddBackButton()
        {
            var backGo  = new GameObject("Back");
            backGo.transform.SetParent(_listParent, false);

            var backBg  = backGo.AddComponent<Image>();
            backBg.color = new Color(0.82f, 0.85f, 0.92f, 0.40f);

            var le           = backGo.AddComponent<LayoutElement>();
            le.preferredHeight = RowHeight;
            le.flexibleWidth   = 1f;

            var btn         = backGo.AddComponent<Button>();
            btn.targetGraphic = backBg;
            btn.onClick.AddListener(() => OnBackClicked?.Invoke());

            var colors              = btn.colors;
            colors.normalColor       = Color.white;
            colors.highlightedColor  = new Color(0.85f, 0.92f, 1.00f);
            colors.pressedColor      = new Color(0.65f, 0.80f, 1.00f);
            btn.colors              = colors;

            var labelGo  = new GameObject("Label");
            labelGo.transform.SetParent(backGo.transform, false);

            var tmp      = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "\u2190 Back";
            tmp.fontSize  = 12f;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color     = new Color(0.20f, 0.30f, 0.50f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.font      = Resources.Load<TMP_FontAsset>(FontPath);

            var lr             = labelGo.GetComponent<RectTransform>();
            lr.anchorMin        = Vector2.zero;
            lr.anchorMax        = Vector2.one;
            lr.sizeDelta        = new Vector2(-12f, 0f);
            lr.anchoredPosition = new Vector2(8f, 0f);
        }

        private void AddSpellRow(Spell spell, int currentMana)
        {
            bool canAfford = currentMana >= spell.manaCost;

            // ── Row container ────────────────────────────────────────────────
            var rowGo   = new GameObject($"Row_{spell.name}");
            rowGo.transform.SetParent(_listParent, false);

            var rowBg   = rowGo.AddComponent<Image>();
            rowBg.color = canAfford
                ? new Color(1f, 1f, 1f, 0.45f)
                : new Color(0.6f, 0.6f, 0.6f, 0.25f);

            var le           = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = RowHeight;
            le.flexibleWidth   = 1f;

            var hlg                    = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding                = new RectOffset(6, 6, 4, 4);
            hlg.spacing                = 4f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment         = TextAnchor.MiddleLeft;

            // ── Button (click to cast) ────────────────────────────────────────
            var btn          = rowGo.AddComponent<Button>();
            btn.targetGraphic = rowBg;
            btn.interactable  = canAfford;

            Spell captured = spell;
            btn.onClick.AddListener(() => OnSpellSelected?.Invoke(captured));

            var colors              = btn.colors;
            colors.normalColor       = Color.white;
            colors.highlightedColor  = new Color(0.85f, 0.92f, 1.00f);
            colors.pressedColor      = new Color(0.65f, 0.80f, 1.00f);
            colors.disabledColor     = new Color(0.55f, 0.55f, 0.55f, 0.55f);
            btn.colors              = colors;

            // ── Tooltip on hover ─────────────────────────────────────────────
            var tt = rowGo.AddComponent<TooltipTrigger>();
            tt.Initialize(spell.name, BuildTooltipBody(spell));

            // ── Spell name label ─────────────────────────────────────────────
            var nameGo = new GameObject("SpellName");
            nameGo.transform.SetParent(rowGo.transform, false);

            var nameTmp         = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text         = spell.name;
            nameTmp.fontSize     = 13f;
            nameTmp.fontStyle    = canAfford ? FontStyles.Normal : FontStyles.Italic;
            nameTmp.color        = canAfford
                ? new Color(0.05f, 0.15f, 0.35f)
                : new Color(0.38f, 0.38f, 0.42f);
            nameTmp.alignment    = TextAlignmentOptions.MidlineLeft;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.font         = Resources.Load<TMP_FontAsset>(FontPath);

            var nameLe         = nameGo.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1f;

            // ── Mana manaCost label (only shown when manaCost > 0) ───────────────────
            if (spell.manaCost > 0)
            {
                var manaCostGo = new GameObject("Cost");
                manaCostGo.transform.SetParent(rowGo.transform, false);

                var manaCostTmp      = manaCostGo.AddComponent<TextMeshProUGUI>();
                manaCostTmp.text      = $"{spell.manaCost}MP";
                manaCostTmp.fontSize  = 11f;
                manaCostTmp.color     = canAfford
                    ? new Color(0.10f, 0.30f, 0.80f)
                    : new Color(0.40f, 0.40f, 0.55f);
                manaCostTmp.alignment = TextAlignmentOptions.MidlineRight;
                manaCostTmp.font      = Resources.Load<TMP_FontAsset>(FontPath);

                var manaCostLe           = manaCostGo.AddComponent<LayoutElement>();
                manaCostLe.preferredWidth = CostWidth;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string BuildTooltipBody(Spell spell)
        {
            var sb = new StringBuilder();
            sb.Append(KeywordRegistry.FormatDescription(spell.description));

            if (spell.targetType == SpellTargetType.Self)
                sb.Append("\n<i>Targets self — no aim required.</i>");
            else if (spell.range > 0)
                sb.Append($"\nRange: {spell.range} tiles");

            if (spell.manaCost > 0)
                sb.Append($"\nMana cost: {spell.manaCost}");

            if (spell.cooldown > 0 && spell.cooldown < 999)
                sb.Append($"\nCooldown: {spell.cooldown} turn(s)");

            return sb.ToString();
        }
    }
}
