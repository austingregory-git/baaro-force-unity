using UnityEngine;
using UnityEngine.UIElements;

namespace BaaroForce.UI
{
    /// <summary>
    /// Bottom-centre Yes/No confirmation toast — same chassis-free "toast" placement and
    /// look as WarningToastUI, but for a player DECISION (End Turn while allies still have
    /// an action point) rather than a passive rejected-action notice: it never auto-hides,
    /// and only proceeds if the player explicitly clicks Yes.
    ///
    /// Added by TurnManager.Initialize() alongside the other combat UI components, same
    /// convention as WarningToastUI — reuses whichever UIDocument is already in the scene.
    /// </summary>
    public class EndTurnConfirmUI : MonoBehaviour
    {
        private VisualElement _toast;
        private Label _label;
        private System.Action _onConfirm;

        private void Awake()     => BuildToast();
        private void OnDestroy() => _toast?.RemoveFromHierarchy();

        private void BuildToast()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[EndTurnConfirmUI] No UIDocument found in scene.");
                return;
            }

            _toast = new VisualElement();
            _toast.AddToClassList("confirm-toast");
            _toast.style.display = DisplayStyle.None;

            _label = new Label();
            _label.AddToClassList("confirm-toast-label");
            _toast.Add(_label);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("confirm-toast-buttons");
            _toast.Add(buttonRow);

            buttonRow.Add(MakeButton("Yes", () =>
            {
                Hide();
                _onConfirm?.Invoke();
            }));
            buttonRow.Add(MakeButton("No", Hide));

            doc.rootVisualElement.Add(_toast);
        }

        private static Button MakeButton(string label, System.Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("action-btn");
            btn.AddToClassList("confirm-toast-btn");
            return btn;
        }

        /// <summary>Shows the prompt with <paramref name="message"/>. <paramref
        /// name="onConfirm"/> fires only if the player clicks Yes; No (or clicking Yes)
        /// just dismisses it either way.</summary>
        public void Show(string message, System.Action onConfirm)
        {
            if (_toast == null) return;

            _label.text  = message;
            _onConfirm   = onConfirm;
            _toast.style.display = DisplayStyle.Flex;
            // Same reasoning as WarningToastUI.Show — re-assert top-of-stack on every show
            // in case a modal opened after this component was first built.
            _toast.BringToFront();
        }

        /// <summary>Hides the prompt without confirming.</summary>
        public void Hide()
        {
            if (_toast != null) _toast.style.display = DisplayStyle.None;
        }
    }
}
