using System.Collections;
using UnityEngine;
using TMPro;
using BaaroForce.Characters;
using BaaroForce.Statuses;

namespace BaaroForce.UI
{
    /// <summary>
    /// Singleton that spawns short-lived floating combat text above units — damage numbers
    /// colour-coded by damage type (see <see cref="CombatTextColors"/>), and status-effect
    /// names colour-coded by buff/debuff.
    ///
    /// Unlike <see cref="TooltipSystem"/> (one persistent panel reused for every hover), each
    /// call here spawns its own independent popup GameObject that animates — a quick
    /// overshoot-and-settle scale pop, a steady rise with a gentle side-to-side sway, then a
    /// fade — and destroys itself once its lifetime elapses. Several can be alive at once
    /// (e.g. Cleave hitting three enemies in one swing).
    ///
    /// World position is resolved to screen space once at spawn time via the unit's current
    /// <see cref="SpriteRenderer"/> bounds; the popup does not keep tracking the unit
    /// afterwards, so it's safe even if that unit dies and its model is destroyed a moment later.
    /// </summary>
    public class FloatingCombatTextSystem : MonoBehaviour
    {
        public static FloatingCombatTextSystem Instance { get; private set; }

        // ── Tunables ─────────────────────────────────────────────────────────
        private const float FontSize      = 22f;
        private const float RiseDistance  = 70f;   // screen pixels travelled upward over the lifetime
        private const float SwayAmplitude = 10f;   // screen pixels of side-to-side drift
        private const float Lifetime      = 1.1f;  // seconds before the popup fully fades and despawns
        private const float PopDuration   = 0.15f; // seconds for the initial scale-bounce
        private const float SpawnJitterX  = 14f;   // random horizontal offset so simultaneous hits don't overlap exactly

        // A quick overshoot-then-settle pop, same bounce language as the character-select
        // card hover animation (see CharacterCardHandler.BounceCurve).
        private static readonly AnimationCurve PopCurve = new AnimationCurve(
            new Keyframe(0f,   0f,  0f,  4f),
            new Keyframe(0.6f, 1.2f, 2f, -3f),
            new Keyframe(1f,   1f,  0f,  0f));

        private Canvas _canvas;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("[FloatingCombatTextCanvas]");
            DontDestroyOnLoad(canvasGo);
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 998; // below TooltipSystem's 999, above the map/HUD.
            // No CanvasScaler — positions map 1:1 to screen pixels, matching TooltipSystem.
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Pops a damage number above <paramref name="target"/>, coloured by damage type.</summary>
        public void ShowDamage(Character target, int amount, SpellType damageType) =>
            Spawn(target, amount.ToString(), CombatTextColors.ForDamageType(damageType));

        /// <summary>Pops a status-effect name above <paramref name="target"/>, coloured by buff/debuff.</summary>
        public void ShowStatus(Character target, string statusName, StatusEffect.StatusEffectType effectType) =>
            Spawn(target, statusName, CombatTextColors.ForStatusEffect(effectType));

        /// <summary>Pops a health-gain number above <paramref name="target"/> in light green.
        /// No-ops for zero/negative amounts (e.g. healing that was fully wasted at max HP).</summary>
        public void ShowHeal(Character target, int amount)
        {
            if (amount <= 0) return;
            Spawn(target, $"+{amount}", CombatTextColors.HealColor);
        }

        // ── Internals ────────────────────────────────────────────────────────

        private void Spawn(Character target, string text, Color color)
        {
            if (_canvas == null) return;

            Vector3? worldPos = GetHeadPosition(target);
            if (worldPos == null) return;

            Vector2 screenPos = Camera.main != null
                ? (Vector2)Camera.main.WorldToScreenPoint(worldPos.Value)
                : (Vector2)worldPos.Value;

            var go = new GameObject("FloatingText_" + text);
            go.transform.SetParent(_canvas.transform, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text       = text;
            tmp.fontSize   = FontSize;
            tmp.fontStyle  = FontStyles.Bold;
            tmp.color      = color;
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.font       = Resources.Load<TMP_FontAsset>("Fonts/Baloo2-Bold SDF");
            // Dark outline keeps the text legible against any background, since this
            // palette is deliberately pale (it matches the character-select realm colours).
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(13, 13, 13, 230);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160f, 40f);

            float jitterX = Random.Range(-SpawnJitterX, SpawnJitterX);
            rect.anchoredPosition = screenPos + new Vector2(jitterX, 0f);

            StartCoroutine(Animate(go, rect));
        }

        private static IEnumerator Animate(GameObject go, RectTransform rect)
        {
            Vector2 startPos   = rect.anchoredPosition;
            float   swayPhase  = Random.Range(0f, Mathf.PI * 2f);
            var     canvasGroup = go.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < Lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Lifetime);

                // Rise + gentle horizontal sway so the popup feels alive rather than a stiff
                // straight line — the sway eases out over the lifetime.
                float y = startPos.y + RiseDistance * t;
                float x = startPos.x + Mathf.Sin(t * Mathf.PI * 2f + swayPhase) * SwayAmplitude * (1f - t);
                rect.anchoredPosition = new Vector2(x, y);

                // Quick overshoot pop on the way in, settling to normal scale.
                float popT  = Mathf.Clamp01(elapsed / PopDuration);
                float scale = Mathf.LerpUnclamped(0.5f, 1f, PopCurve.Evaluate(popT));
                rect.localScale = Vector3.one * scale;

                // Fade out over the back half of the lifetime.
                canvasGroup.alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;

                yield return null;
            }

            Destroy(go);
        }

        /// <summary>The world-space point just above the unit's rendered sprite, or null if
        /// the character currently has no live model on the map (e.g. already removed).</summary>
        private static Vector3? GetHeadPosition(Character target)
        {
            GameObject unitObj = target?.CharacterCurrentTile?.UnitObject;
            if (unitObj == null) return null;

            var renderer = unitObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
                return new Vector3(unitObj.transform.position.x, renderer.bounds.max.y, unitObj.transform.position.z);

            return unitObj.transform.position + Vector3.up;
        }
    }
}
