using UnityEngine;
using UnityEngine.UIElements;

namespace BaaroForce.UI
{
    /// <summary>
    /// Small transient banner shown top-center of the screen to explain why an
    /// attempted action was rejected — e.g. casting an Enemy-targeted spell at a
    /// tile with no enemy on it. Reuses whichever UIDocument is already in the
    /// scene (the one ActionPanelUI/SpellPanelUI attach to), same chassis/rivet
    /// visual language as the rest of the Combat HUD (see CombatHud.uss).
    ///
    /// Calling <see cref="Show"/> again while a toast is already visible just
    /// swaps the message and restarts the auto-hide timer.
    /// </summary>
    public class WarningToastUI : MonoBehaviour
    {
        private const long DisplayMilliseconds = 2200;

        private VisualElement _toast;
        private Label _label;
        private IVisualElementScheduledItem _hideTimer;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()     => BuildToast();
        private void OnDestroy() => _toast?.RemoveFromHierarchy();

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildToast()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[WarningToastUI] No UIDocument found in scene.");
                return;
            }

            _toast = new VisualElement();
            _toast.AddToClassList("warning-toast");
            _toast.style.display = DisplayStyle.None;
            // Never intercept clicks — the toast is purely informational and
            // must not block grid/tile input underneath it.
            _toast.pickingMode = PickingMode.Ignore;

            _label = new Label();
            _label.AddToClassList("warning-toast-label");
            _toast.Add(_label);

            doc.rootVisualElement.Add(_toast);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Shows <paramref name="message"/> for a couple of seconds, then hides.</summary>
        public void Show(string message)
        {
            if (_toast == null) return;

            _hideTimer?.Pause();

            _label.text = message;
            _toast.style.display = DisplayStyle.Flex;

            _hideTimer = _toast.schedule.Execute(Hide).StartingIn(DisplayMilliseconds);
        }

        /// <summary>Hides the toast immediately.</summary>
        public void Hide()
        {
            if (_toast != null) _toast.style.display = DisplayStyle.None;
        }
    }
}
