using UnityEngine;
using UnityEngine.UIElements;

namespace BaaroForce.UI
{
    /// <summary>
    /// A simple arrow (a thin shaft plus a two-bar chevron head) drawn directly on a
    /// UIDocument's root, pointing from one VisualElement's edge to another's. Used by
    /// <see cref="CharacterHudController"/> to show which panel a pending action targets.
    ///
    /// Built entirely from thin rotated rectangles — the same `rotate` trick this
    /// project already uses for the movement pips' diamonds — rather than a font glyph.
    /// Each bar's rotation pivot is pinned exactly on the point it needs to meet (the
    /// shaft's pivot is its own left edge; each chevron wing's pivot is its own right
    /// edge, at the arrow's tip), so the head always meets the shaft with no gap,
    /// regardless of the angle — unlike a glyph, whose internal font padding doesn't
    /// rotate predictably.
    ///
    /// Hidden by default. Call <see cref="PointBetween"/> whenever the source/target
    /// panels are both visible and there's something to point at; call <see cref="Hide"/>
    /// otherwise. All bars ignore pointer events so the arrow never blocks clicks.
    /// </summary>
    public sealed class ActionArrowIndicator
    {
        private const float ShaftThickness = 3f;
        private const float WingLength     = 13f;
        private const float WingThickness  = 3f;
        private const float WingSpreadDeg  = 27f; // each wing's angle off the reversed shaft direction

        private readonly VisualElement _shaft;
        private readonly VisualElement _wingA;
        private readonly VisualElement _wingB;
        private bool _visible;

        public ActionArrowIndicator(VisualElement root)
        {
            _shaft = CreateBar(root, pivotOnRight: false);
            _wingA = CreateBar(root, pivotOnRight: true);
            _wingB = CreateBar(root, pivotOnRight: true);
        }

        private static VisualElement CreateBar(VisualElement root, bool pivotOnRight)
        {
            var bar = new VisualElement();
            bar.AddToClassList("action-arrow-shaft");
            bar.style.display = DisplayStyle.None;
            bar.pickingMode = PickingMode.Ignore;
            // Pivot on the edge that needs to stay pinned to a fixed point while the bar
            // rotates around it (the shaft's start, or the tip the wings meet at).
            bar.style.transformOrigin = new StyleTransformOrigin(
                new TransformOrigin(Length.Percent(pivotOnRight ? 100 : 0), Length.Percent(50)));
            root.Add(bar);
            return bar;
        }

        /// <summary>
        /// Points the arrow from the right edge of <paramref name="from"/> to the left
        /// edge of <paramref name="to"/> (at their vertical centres) and shows it.
        /// No-ops (hides instead) if either element hasn't been laid out yet.
        /// </summary>
        public void PointBetween(VisualElement from, VisualElement to)
        {
            Rect fromRect = from.worldBound;
            Rect toRect   = to.worldBound;
            if (fromRect.width <= 0f || toRect.width <= 0f) { Hide(); return; }

            Vector2 start = new Vector2(fromRect.xMax, fromRect.center.y);
            Vector2 end   = new Vector2(toRect.xMin, toRect.center.y);
            Vector2 delta = end - start;
            float   angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            PositionBar(_shaft, start, delta.magnitude, angleDeg, ShaftThickness, pivotOnRight: false);
            PositionBar(_wingA, end, WingLength, angleDeg + 180f - WingSpreadDeg, WingThickness, pivotOnRight: true);
            PositionBar(_wingB, end, WingLength, angleDeg + 180f + WingSpreadDeg, WingThickness, pivotOnRight: true);

            _visible = true;
        }

        private static void PositionBar(VisualElement bar, Vector2 pivotPoint, float length, float angleDeg,
            float thickness, bool pivotOnRight)
        {
            bar.style.display = DisplayStyle.Flex;
            bar.style.height  = thickness;
            bar.style.width   = length;
            bar.style.left    = pivotOnRight ? pivotPoint.x - length : pivotPoint.x;
            bar.style.top     = pivotPoint.y - thickness * 0.5f;
            bar.style.rotate  = new StyleRotate(new Rotate(angleDeg));
        }

        /// <summary>Hides the arrow.</summary>
        public void Hide()
        {
            if (!_visible) return;
            _shaft.style.display = DisplayStyle.None;
            _wingA.style.display = DisplayStyle.None;
            _wingB.style.display = DisplayStyle.None;
            _visible = false;
        }
    }
}
