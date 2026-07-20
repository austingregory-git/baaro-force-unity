using UnityEngine;

namespace BaaroForce.Animations
{
    public enum SpriteFacing { FrontLeft, FrontRight, BackLeft, BackRight }

    /// <summary>
    /// Renders a map unit as a flat, camera-facing 2D sprite and swaps between the four
    /// isometric direction sprites in a SpriteKit as it moves. Only the four static angles
    /// are wired up for now — idle/walk/attack/death frame lists on SpriteKit are for later.
    ///
    /// Added by MapTile alongside a SpriteRenderer when a unit is placed on the map.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteCharacterView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite _frontLeft, _frontRight, _backLeft, _backRight;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        /// <summary>Loads the kit's four directional sprites and orients this GameObject to face the (static) main camera.
        /// Enemies spawn facing the opposite iso direction from allies (BackLeft vs. FrontRight) so the two
        /// sides visually face each other across the battlefield instead of both facing the same way.</summary>
        public void Initialize(SpriteKit kit, bool isEnemy = false)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            _frontLeft  = Resources.Load<Sprite>(kit.FrontLeftSpritePath);
            _frontRight = Resources.Load<Sprite>(kit.FrontRightSpritePath);
            _backLeft   = Resources.Load<Sprite>(kit.BackLeftSpritePath);
            _backRight  = Resources.Load<Sprite>(kit.BackRightSpritePath);

            // The isometric camera never rotates during play (see MapGenerator.FitCameraToMap),
            // so matching its rotation once here keeps the flat sprite facing it correctly.
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            SetFacing(isEnemy ? SpriteFacing.BackLeft : SpriteFacing.FrontRight);
        }

        // A translucent, cool-toned tint so an Invisible unit still reads clearly on-screen
        // to the player (see BaaroForce.Statuses.InvisibleStatus) without being fully hidden.
        private static readonly Color InvisibleTint = new Color(0.55f, 0.6f, 0.85f, 0.42f);

        /// <summary>Toggles the translucent, shadow-tinted look used while a unit is Invisible.</summary>
        public void SetInvisible(bool invisible) =>
            _renderer.color = invisible ? InvisibleTint : Color.white;

        public void SetFacing(SpriteFacing facing)
        {
            _renderer.sprite = facing switch
            {
                SpriteFacing.FrontLeft  => _frontLeft,
                SpriteFacing.FrontRight => _frontRight,
                SpriteFacing.BackLeft   => _backLeft,
                SpriteFacing.BackRight  => _backRight,
                _                       => _renderer.sprite,
            };
        }

        /// <summary>
        /// Faces the sprite to match a single grid step (±1 in exactly one axis — this
        /// grid has no diagonal movement). Mapping derived from the fixed isometric camera
        /// in MapGenerator.FitCameraToMap (camera at (d,d,d) looking at the origin):
        /// +X → front-left, -X → back-right, +Z → front-right, -Z → back-left.
        /// </summary>
        public void FaceGridDirection(int deltaX, int deltaZ)
        {
            if (deltaX > 0)      SetFacing(SpriteFacing.FrontLeft);
            else if (deltaX < 0) SetFacing(SpriteFacing.BackRight);
            else if (deltaZ > 0) SetFacing(SpriteFacing.FrontRight);
            else if (deltaZ < 0) SetFacing(SpriteFacing.BackLeft);
        }
    }
}
