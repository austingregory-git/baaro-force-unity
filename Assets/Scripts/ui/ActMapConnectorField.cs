using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BaaroForce.UI
{
    /// <summary>
    /// Draws the lines connecting Act Map node badges to each other — including the
    /// diverging (one node into two fork options) and converging (two fork options back
    /// into one node) cases a fork needs, which a single fixed-width CSS box between
    /// slots can't represent. A single absolutely-positioned VisualElement covering the
    /// whole node path (see ActMapView), painted with runtime Painter2D rather than
    /// built from more VisualElements, since the endpoints are arbitrary points, not a
    /// fixed rectangle.
    /// </summary>
    public class ActMapConnectorField : VisualElement
    {
        private readonly struct Line
        {
            public readonly Vector2 From;
            public readonly Vector2 To;
            public readonly Color Color;
            public readonly float Width;

            public Line(Vector2 from, Vector2 to, Color color, float width)
            {
                From = from;
                To = to;
                Color = color;
                Width = width;
            }
        }

        private readonly List<Line> _lines = new List<Line>();

        public ActMapConnectorField()
        {
            AddToClassList("act-map-connector-field");
            // Purely decorative — never eat clicks meant for the node badges drawn on top.
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        /// <summary>Replaces every line and repaints. Points are in this element's own
        /// local space (see ActMapView.AddLine, which converts each node badge's
        /// worldBound center via this element's WorldToLocal).</summary>
        public void SetLines(List<(Vector2 from, Vector2 to, Color color, float width)> lines)
        {
            _lines.Clear();
            foreach (var l in lines)
                _lines.Add(new Line(l.from, l.to, l.color, l.width));
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_lines.Count == 0) return;

            Painter2D painter = ctx.painter2D;
            foreach (Line line in _lines)
            {
                painter.strokeColor = line.Color;
                painter.lineWidth = line.Width;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(line.From);
                painter.LineTo(line.To);
                painter.Stroke();
            }
        }
    }
}
