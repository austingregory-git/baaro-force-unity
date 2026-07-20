using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaaroForce.Keywords;

namespace BaaroForce.UI 
{

    /// <summary>
    /// Singleton MonoBehaviour that manages a single floating tooltip panel.
    ///
    /// Call <see cref="Show"/> / <see cref="Hide"/> from pointer-event handlers.
    /// The tooltip automatically parses <c>[KeywordName]</c> tokens in description
    /// strings, colour-highlights each keyword, and appends a short glossary of
    /// those keyword definitions beneath the main description text.
    ///
    /// The tooltip canvas uses a dedicated <see cref="Canvas"/> (sortingOrder 999)
    /// so the panel always renders on top.  A <see cref="CanvasGroup"/> with
    /// <c>blocksRaycasts = false</c> ensures the panel never steals pointer events
    /// from the cards below it.
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        private RectTransform      _panelRect;
        private TextMeshProUGUI    _titleText;
        private TextMeshProUGUI    _bodyText;

        /// <summary>Raw (unformatted) body text for the default view.</summary>
        private string _summaryRaw;
        /// <summary>Raw body text shown while Shift is held; null when there's nothing
        /// beyond the summary (in which case Shift does nothing).</summary>
        private string _detailedRaw;
        private bool   _shiftHeldLastFrame;

        private const string ShiftHint = "\n\n<color=#8a8a8a><i>Hold Shift for a breakdown.</i></color>";

        private const float PanelWidth   = 270f;
        private const float PaddingH     = 12f;
        private const float PaddingV     = 10f;
        private const float TitleSize    = 15f;
        private const float BodySize     = 12f;
        private const float CursorOffset = 14f;

        // ------------------------------------------------------------------ //
        // Lifecycle                                                            //
        // ------------------------------------------------------------------ //

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }

        private void Update()
        {
            if (_panelRect == null || !_panelRect.gameObject.activeSelf) return;

            PositionNearCursor();

            bool shiftHeld = IsShiftHeld();
            if (shiftHeld != _shiftHeldLastFrame)
            {
                _shiftHeldLastFrame = shiftHeld;
                RefreshBodyText();
            }
        }

        // ------------------------------------------------------------------ //
        // Public API                                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Shows the tooltip for the given ability. <paramref name="summaryBody"/> is
        /// shown by default; while the player holds Shift, <paramref name="detailedBody"/>
        /// is shown instead (if supplied and different — pass null when there's nothing
        /// beyond the summary). Both may contain <c>[KeywordName]</c> tokens, which are
        /// colour-highlighted and expanded into a glossary beneath the text.
        /// </summary>
        public void Show(string abilityName, string summaryBody, string detailedBody = null)
        {
            _titleText.text = abilityName;
            _summaryRaw     = summaryBody;
            _detailedRaw    = (detailedBody != null && detailedBody != summaryBody) ? detailedBody : null;
            _shiftHeldLastFrame = IsShiftHeld();

            RefreshBodyText();

            _panelRect.gameObject.SetActive(true);

            // Force the layout to compute so ContentSizeFitter resizes the panel
            // before we position it.
            Canvas.ForceUpdateCanvases();

            PositionNearCursor();
        }

        /// <summary>Hides the tooltip.</summary>
        public void Hide()
        {
            if (_panelRect != null)
                _panelRect.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ //
        // Internal helpers                                                     //
        // ------------------------------------------------------------------ //

        private static bool IsShiftHeld() =>
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        /// <summary>Re-renders the body from whichever raw string is currently active
        /// (summary, or detailed while Shift is held), and re-lays-out the panel so its
        /// size follows the new text length.</summary>
        private void RefreshBodyText()
        {
            bool showDetailed = _shiftHeldLastFrame && _detailedRaw != null;
            string raw = showDetailed ? _detailedRaw : _summaryRaw;

            _bodyText.text = BuildBodyText(raw);
            if (_detailedRaw != null && !showDetailed)
                _bodyText.text += ShiftHint;

            Canvas.ForceUpdateCanvases();
        }

        private static string BuildBodyText(string raw)
        {
            string formatted = KeywordRegistry.FormatDescription(raw);
            List<Keyword> keywords = KeywordRegistry.ExtractKeywords(raw);

            if (keywords.Count == 0) return formatted;

            var sb = new System.Text.StringBuilder(formatted);
            sb.Append("\n");
            foreach (Keyword kw in keywords)
            {
                string hex = ColorUtility.ToHtmlStringRGB(kw.Color);
                sb.Append($"\n<color=#{hex}><b>{kw.Name}</b></color>  {kw.Description}");
            }
            return sb.ToString();
        }

        private void BuildUI()
        {
            // ── Tooltip canvas ─────────────────────────────────────────────────
            var canvasGo = new GameObject("[TooltipCanvas]");
            DontDestroyOnLoad(canvasGo);
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            // No CanvasScaler — positions map 1:1 to screen pixels.
            // No GraphicRaycaster — tooltip is display-only.

            // ── Panel ──────────────────────────────────────────────────────────
            var panelGo = new GameObject("TooltipPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);

            var bg    = panelGo.AddComponent<Image>();
            bg.color  = new Color(0.08f, 0.08f, 0.10f, 0.92f);

            // Pass-through: tooltip never captures pointer events from cards below.
            var cg               = panelGo.AddComponent<CanvasGroup>();
            cg.blocksRaycasts    = false;
            cg.interactable      = false;

            // VerticalLayoutGroup + ContentSizeFitter auto-sizes the panel height.
            var vlg                       = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding                   = new RectOffset(
                Mathf.RoundToInt(PaddingH), Mathf.RoundToInt(PaddingH),
                Mathf.RoundToInt(PaddingV), Mathf.RoundToInt(PaddingV));
            vlg.spacing                   = 4f;
            vlg.childForceExpandWidth     = true;
            vlg.childForceExpandHeight    = false;
            vlg.childAlignment            = TextAnchor.UpperLeft;

            var csf           = panelGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Anchor + pivot at bottom-left: anchoredPosition == screen BL corner.
            _panelRect            = panelGo.GetComponent<RectTransform>();
            _panelRect.anchorMin  = Vector2.zero;
            _panelRect.anchorMax  = Vector2.zero;
            _panelRect.pivot      = Vector2.zero;
            _panelRect.sizeDelta  = new Vector2(PanelWidth, 60f);

            // ── Text children ──────────────────────────────────────────────────
            _titleText = AddTextChild(panelGo.transform, "TooltipTitle",
                TitleSize, bold: true,  color: Color.white);

            _bodyText  = AddTextChild(panelGo.transform, "TooltipBody",
                BodySize,  bold: false, color: new Color(0.85f, 0.85f, 0.85f));

            panelGo.SetActive(false);
        }

        /// <summary>Creates a TextMeshProUGUI child that grows vertically with its content.</summary>
        private static TextMeshProUGUI AddTextChild(Transform parent, string goName,
            float fontSize, bool bold, Color color)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize           = fontSize;
            tmp.fontStyle          = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color              = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2-Bold SDF");

            var csf           = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            return tmp;
        }

        /// <summary>
        /// Positions the tooltip panel near the cursor, flipping sides if it would
        /// extend beyond the screen edges.
        /// </summary>
        private void PositionNearCursor()
        {
            Vector2 mouse   = Input.mousePosition;
            Vector2 size    = _panelRect.sizeDelta;
            float   screenW = Screen.width;
            float   screenH = Screen.height;

            // Default: open to the right of and above the cursor.
            float x = mouse.x + CursorOffset;
            float y = mouse.y + CursorOffset;

            // Flip horizontally if the panel would clip the right edge.
            if (x + size.x > screenW) x = mouse.x - CursorOffset - size.x;
            // Flip vertically if the panel would clip the top edge.
            if (y + size.y > screenH) y = mouse.y - CursorOffset - size.y;

            // Safety clamp so the panel is never off-screen.
            x = Mathf.Clamp(x, 0f, screenW - size.x);
            y = Mathf.Clamp(y, 0f, screenH - size.y);

            _panelRect.anchoredPosition = new Vector2(x, y);
        }
    }
}
