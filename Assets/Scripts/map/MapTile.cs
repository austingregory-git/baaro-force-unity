using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Animations;

namespace BaaroForce.Map
{
    /// <summary>
    /// Represents a single cube tile in the 3D isometric map.
    /// Spawned by MapGenerator via GameObject.CreatePrimitive(PrimitiveType.Cube),
    /// so MeshFilter + MeshRenderer are already present.
    /// </summary>
    public class MapTile : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        // Properties                                                          //
        // ------------------------------------------------------------------ //

        public TerrainTile.TerrainType TerrainType      { get; private set; }
        public bool                IsInDeploymentZone    { get; private set; }

        /// <summary>The single unit (player Character or Npc) occupying this tile, or null.</summary>
        public Character OccupyingUnit { get; private set; }

        /// <summary>The occupying unit if it's a player Character (not an Npc), or null.</summary>
        public Character OccupyingCharacter => OccupyingUnit is Npc ? null : OccupyingUnit;
        /// <summary>The occupying unit if it's an Npc, or null.</summary>
        public Npc       OccupyingNpc       => OccupyingUnit as Npc;

        public bool IsOccupied => OccupyingUnit != null;

        /// <summary>Column index in the grid array.</summary>
        public int GridX { get; private set; }
        /// <summary>Row index in the grid array.</summary>
        public int GridZ { get; private set; }

        /// <summary>The instantiated model (player Character or Npc) on this tile (may be null).</summary>
        public GameObject UnitObject => _unitObject;

        // ------------------------------------------------------------------ //
        // Private state                                                       //
        // ------------------------------------------------------------------ //

        private GameObject _unitObject;
        private GameObject _deploymentOverlay;
        private GameObject _moveHighlightOverlay;
        private GameObject _attackHighlightOverlay;
        private GameObject _spellHighlightOverlay;

        private static readonly Color OverlayColor         = new Color(0.3f, 0.6f, 1f, 0.92f);
        private static readonly Color MoveHighlightColor    = new Color(0.3f, 0.6f, 1f, 0.92f);
        private static readonly Color AttackHighlightColor  = new Color(0.9f, 0.15f, 0.1f, 0.55f);

        // ------------------------------------------------------------------ //
        // Lifecycle                                                           //
        // ------------------------------------------------------------------ //

        // No Awake needed — MeshRenderer/MeshFilter come from CreatePrimitive.

        // ------------------------------------------------------------------ //
        // Initialisation                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Stores the tile's position in the grid array (called by MapGenerator).</summary>
        public void SetGridCoords(int x, int z)
        {
            GridX = x;
            GridZ = z;
        }

        /// <summary>Assigns the terrain colour to the cube's MeshRenderer material.</summary>
        public void Initialize(TerrainTile.TerrainType type)
        {
            TerrainType = type;
            var mat = new Material(Shader.Find("Standard"));
            mat.color = GetTerrainColor(type);
            GetComponent<MeshRenderer>().material = mat;
        }

        // ------------------------------------------------------------------ //
        // Deployment zone                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>Marks this tile as part of the deployment zone and shows/hides the overlay.</summary>
        public void SetDeploymentZone(bool active)
        {
            IsInDeploymentZone = active;

            if (active)
            {
                if (_deploymentOverlay != null) return;

                _deploymentOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _deploymentOverlay.name = "DeploymentOverlay";
                Destroy(_deploymentOverlay.GetComponent<Collider>());

                _deploymentOverlay.transform.SetParent(transform, false);
                // Quad faces +Z by default; rotate 90° on X so it lies flat on the top face.
                // In tile-local space the top face is at y = 0.5; use 0.52 to avoid Z-fighting.
                _deploymentOverlay.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                _deploymentOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _deploymentOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, OverlayColor);
                _deploymentOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (_deploymentOverlay != null)
                {
                    Destroy(_deploymentOverlay);
                    _deploymentOverlay = null;
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Move highlight                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Shows or hides the near-opaque blue overlay used to indicate tiles
        /// the selected character can move to during the player's turn.
        /// </summary>
        public void SetMoveHighlight(bool active)
        {
            if (active)
            {
                if (_moveHighlightOverlay != null) return;

                _moveHighlightOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _moveHighlightOverlay.name = "MoveHighlight";
                Destroy(_moveHighlightOverlay.GetComponent<Collider>());

                _moveHighlightOverlay.transform.SetParent(transform, false);
                // Sit slightly above the deployment overlay (0.52) to avoid Z-fighting.
                _moveHighlightOverlay.transform.localPosition = new Vector3(0f, 0.54f, 0f);
                _moveHighlightOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _moveHighlightOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, MoveHighlightColor);
                _moveHighlightOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (_moveHighlightOverlay != null)
                {
                    Destroy(_moveHighlightOverlay);
                    _moveHighlightOverlay = null;
                }
            }
        }

        /// <summary>
        /// Shows or hides the translucent red overlay used to indicate tiles
        /// within the selected character's attack range during the player's turn.
        /// </summary>
        public void SetAttackHighlight(bool active)
        {
            if (active)
            {
                if (_attackHighlightOverlay != null) return;

                _attackHighlightOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _attackHighlightOverlay.name = "AttackHighlight";
                Destroy(_attackHighlightOverlay.GetComponent<Collider>());

                _attackHighlightOverlay.transform.SetParent(transform, false);
                _attackHighlightOverlay.transform.localPosition = new Vector3(0f, 0.54f, 0f);
                _attackHighlightOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _attackHighlightOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, AttackHighlightColor);
                _attackHighlightOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (_attackHighlightOverlay != null)
                {
                    Destroy(_attackHighlightOverlay);
                    _attackHighlightOverlay = null;
                }
            }
        }

        /// <summary>
        /// Shows or hides a coloured spell-range overlay on this tile.
        /// The colour is determined by the spell's target type and passed in by TurnManager:
        ///   red = enemy spells, green = ally spells, purple = either.
        /// </summary>
        public void SetSpellHighlight(bool active, Color color)
        {
            if (active)
            {
                if (_spellHighlightOverlay != null) return;

                _spellHighlightOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _spellHighlightOverlay.name = "SpellHighlight";
                Destroy(_spellHighlightOverlay.GetComponent<Collider>());

                _spellHighlightOverlay.transform.SetParent(transform, false);
                _spellHighlightOverlay.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                _spellHighlightOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                _spellHighlightOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, color);
                _spellHighlightOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (_spellHighlightOverlay != null)
                {
                    Destroy(_spellHighlightOverlay);
                    _spellHighlightOverlay = null;
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Occupancy                                                           //
        // ------------------------------------------------------------------ //

        // All units currently share Winston's sprite art (see BaaroForce.Animations.SpriteKit) —
        // the only isometric set that exists so far. Once other characters get their own art,
        // this should move onto Character as a per-instance SpriteKit instead of being shared.
        private static readonly SpriteKit DefaultSpriteKit = new SpriteKit(
            backLeftSpritePath: "winston_back_left_128x128",
            backRightSpritePath: "winston_back_right_128x128",
            frontLeftSpritePath: "winston_front_left_128x128",
            frontRightSpritePath: "winston_front_right_128x128",
            idleSpritePaths: null,
            walkSpritePaths: null,
            attackSpritePaths: null,
            deathSpritePaths: null);

        /// <summary>
        /// Places the unit's isometric sprite on top of this tile.
        /// Parented to the grid root (tile's parent) to avoid non-uniform scale distortion.
        /// </summary>
        public void PlaceUnit(Character unit)
        {
            if (IsOccupied) return;
            OccupyingUnit = unit;
            unit.CharacterCurrentTile = this;

            _unitObject = new GameObject($"{(unit is Npc ? "Npc" : "Character")}_{unit.CharacterName}");
            var spriteRenderer = _unitObject.AddComponent<SpriteRenderer>();
            var view = _unitObject.AddComponent<SpriteCharacterView>();
            view.Initialize(DefaultSpriteKit, unit is Npc);

            // Parent to the MapGenerator (tile's parent) — it has uniform scale (1,1,1),
            // so world-space and local-space transforms are equivalent.
            _unitObject.transform.SetParent(transform.parent, false);

            // Sit the sprite on top of this tile in world space.
            float halfTileH = transform.lossyScale.y * 0.5f;
            _unitObject.transform.position = transform.position + Vector3.up * (halfTileH + 0.05f);

            // Scale to 80% of the tile's world footprint. Uses the sprite's own local (pre-rotation)
            // bounds rather than its rotated world-space AABB, since the sprite is tilted to face the
            // isometric camera — the world AABB of a tilted flat quad overstates its on-screen size.
            float spriteLocalSize = Mathf.Max(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y);
            float targetWorldSize = transform.lossyScale.x * 0.8f;
            _unitObject.transform.localScale = Vector3.one * (spriteLocalSize > 0f ? targetWorldSize / spriteLocalSize : 1f);
        }

        /// <summary>Removes the occupying unit from this tile and destroys its model.</summary>
        public void RemoveUnit()
        {
            if (OccupyingUnit != null)
                OccupyingUnit.CharacterCurrentTile = null;

            OccupyingUnit = null;
            if (_unitObject != null)
            {
                Destroy(_unitObject);
                _unitObject = null;
            }
        }

        /// <summary>
        /// Clears occupancy without destroying the model GameObject.
        /// Used by TurnManager during movement animation so the model can be
        /// handed off to the destination tile.
        /// </summary>
        public void ReleaseUnit()
        {
            OccupyingUnit = null;
            _unitObject    = null;   // we no longer own this reference
        }

        /// <summary>
        /// Assigns an already-existing unit model to this tile.
        /// Called by TurnManager after the model has been moved in world-space.
        /// </summary>
        public void AssignUnit(Character unit, GameObject model)
        {
            OccupyingUnit = unit;
            _unitObject    = model;
            unit.CharacterCurrentTile = this;
        }

        // ------------------------------------------------------------------ //
        // Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private static void ApplyTransparency(Material mat, Color color)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;
        }

        private static Color GetTerrainColor(TerrainTile.TerrainType type)
        {
            switch (type)
            {
                case TerrainTile.TerrainType.Grass:    return new Color(0.27f, 0.65f, 0.16f);
                case TerrainTile.TerrainType.Forest:   return new Color(0.07f, 0.35f, 0.07f);
                case TerrainTile.TerrainType.Mountain: return new Color(0.55f, 0.52f, 0.47f);
                case TerrainTile.TerrainType.Water:    return new Color(0.12f, 0.46f, 0.78f);
                case TerrainTile.TerrainType.Desert:   return new Color(0.93f, 0.84f, 0.45f);
                case TerrainTile.TerrainType.Swamp:    return new Color(0.28f, 0.33f, 0.13f);
                case TerrainTile.TerrainType.Volcano:  return new Color(0.72f, 0.16f, 0.05f);
                case TerrainTile.TerrainType.Snow:     return new Color(0.92f, 0.96f, 1.00f);
                case TerrainTile.TerrainType.Plains:   return new Color(0.70f, 0.84f, 0.40f);
                case TerrainTile.TerrainType.Void:     return new Color(0.10f, 0.03f, 0.18f);
                case TerrainTile.TerrainType.Ash:      return new Color(0.50f, 0.50f, 0.50f);
                case TerrainTile.TerrainType.Lava:     return new Color(0.80f, 0.20f, 0.00f);
                case TerrainTile.TerrainType.Tundra:   return new Color(0.85f, 0.90f, 0.95f);
                case TerrainTile.TerrainType.Creek:    return new Color(0.15f, 0.40f, 0.70f);
                case TerrainTile.TerrainType.Ocean:    return new Color(0.05f, 0.25f, 0.50f);
                case TerrainTile.TerrainType.Meadow:   return new Color(0.30f, 0.70f, 0.20f);
                default:                               return Color.white;
            }
        }
    }
}