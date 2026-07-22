using UnityEngine;
using UnityEngine.EventSystems;

namespace BaaroForce.Map
{
    /// <summary>
    /// Mouse-scroll zoom for the isometric map camera, anchored to the cursor (the world
    /// point under the mouse stays under the mouse as you zoom, like a map/graphics editor)
    /// rather than always zooming toward the view center. Only ever touches Camera.main's
    /// orthographicSize and position — the Combat HUD is built on its own screen-space UI
    /// (UIDocument overlay panels, TooltipSystem's separate overlay Canvas), which isn't part
    /// of the 3D scene the camera renders "zoomed," so it's untouched by construction.
    /// </summary>
    public class MapCameraZoom : MonoBehaviour
    {
        // Fraction of the current orthographic size zoomed per scroll notch — multiplicative
        // rather than a flat world-unit step so it feels the same regardless of map size.
        private const float ZoomFactor = 0.12f;

        private float _maxOrthoSize; // fully zoomed out — same framing FitCameraToMap always used
        private float _minOrthoSize; // fully zoomed in

        /// <summary>
        /// (Re)configures the zoom range for a newly generated map and applies its starting
        /// zoom, centred on the map (no cursor to anchor to yet). <paramref name="fitOrthoSize"/>
        /// is the "whole map fits on screen" size MapGenerator.FitCameraToMap already computes
        /// — kept as the zoomed-out bound so scrolling out never loses the map. Larger grids
        /// start more zoomed in since fitting a 16x16/24x24 grid at once makes individual tiles
        /// too small to read; 8x8 keeps today's fully-fit starting view.
        /// </summary>
        public void Initialize(int gridSize, float fitOrthoSize)
        {
            _maxOrthoSize = fitOrthoSize;
            _minOrthoSize = fitOrthoSize * 0.35f;

            float startFactor = gridSize <= 8 ? 1f : gridSize <= 16 ? 0.62f : 0.45f;
            Camera cam = Camera.main;
            if (cam != null) cam.orthographicSize = fitOrthoSize * startFactor;
        }

        private void Update()
        {
            // Don't zoom the map while scrolling over a HUD element (e.g. a future
            // scrollable panel) — same guard TurnManager uses before map-click/hover input.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float newSize = Mathf.Clamp(
                cam.orthographicSize * (1f - scroll * ZoomFactor), _minOrthoSize, _maxOrthoSize);
            if (Mathf.Approximately(newSize, cam.orthographicSize)) return;

            // Keep the ground-plane point under the cursor fixed on screen across the zoom —
            // find it before and after the size change, then pan the camera by the difference.
            bool hadAnchor = TryGetGroundPoint(cam, Input.mousePosition, out Vector3 worldBefore);

            cam.orthographicSize = newSize;

            if (hadAnchor && TryGetGroundPoint(cam, Input.mousePosition, out Vector3 worldAfter))
                cam.transform.position += worldBefore - worldAfter;
        }

        /// <summary>Where the mouse ray (at the camera's *current* orthographic size) crosses
        /// the map's ground plane (Y=0) — same technique TurnManager/DeploymentManager already
        /// use to resolve a clicked/hovered tile from screen space.</summary>
        private static bool TryGetGroundPoint(Camera cam, Vector3 screenPos, out Vector3 point)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = Vector3.zero;
            return false;
        }
    }
}
