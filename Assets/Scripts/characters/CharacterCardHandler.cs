using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using BaaroForce.GameController;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Attached to each character card in the selection screen.
    /// Plays a bouncy scale animation on hover and adds the character to
    /// the party then loads MapScene on click.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class CharacterCardHandler : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private const string MapSceneName = "MapScene";

        // The hovered scale relative to base (10% larger).
        private const float HoverScale = 1.1f;

        public Character Character { get; private set; }

        private Vector3 _baseScale;
        private Coroutine _activeCoroutine;

        // Bounce curve: 0 → 1.2 (overshoot) → 0.9 (undershoot) → 1.0 (settle)
        // Applied via LerpUnclamped so the card visually overshoots HoverScale then settles.
        private static readonly AnimationCurve BounceCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.00f, 0f,  4f),
            new Keyframe(0.40f, 1.20f, 2f, -3f),
            new Keyframe(0.70f, 0.90f, 1f,  2f),
            new Keyframe(1.00f, 1.00f, 0f,  0f)
        );

        public void Initialize(Character character)
        {
            Character = character;
            _baseScale = transform.localScale;
        }

        // ------------------------------------------------------------------ //
        // Pointer events                                                       //
        // ------------------------------------------------------------------ //

        public void OnPointerEnter(PointerEventData _) => Bounce(BounceIn());
        public void OnPointerExit(PointerEventData _)  => Bounce(BounceOut());

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only respond to left-click.
            if (eventData.button != PointerEventData.InputButton.Left) return;

            PartyManager.Instance.AddMember(Character);
            SceneManager.LoadScene(MapSceneName);
        }

        // ------------------------------------------------------------------ //
        // Animation coroutines                                                 //
        // ------------------------------------------------------------------ //

        private void Bounce(IEnumerator routine)
        {
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(routine);
        }

        /// <summary>Scales from current to HoverScale using the bouncy curve.</summary>
        private IEnumerator BounceIn()
        {
            float duration = 0.22f;
            float elapsed  = 0f;
            Vector3 start  = transform.localScale;
            Vector3 target = _baseScale * HoverScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float curveValue = BounceCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                transform.localScale = Vector3.LerpUnclamped(start, target, curveValue);
                yield return null;
            }

            transform.localScale = target;
            _activeCoroutine = null;
        }

        /// <summary>Eases smoothly back to base scale.</summary>
        private IEnumerator BounceOut()
        {
            float duration = 0.12f;
            float elapsed  = 0f;
            Vector3 start  = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.localScale = Vector3.Lerp(start, _baseScale, t);
                yield return null;
            }

            transform.localScale = _baseScale;
            _activeCoroutine = null;
        }
    }
}
