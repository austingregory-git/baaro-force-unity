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
        private GameObject _zoneOfControlOutline;
        private GameObject _hoverOutline;
        private GameObject _selectionOutline;

        private static readonly Color OverlayColor         = new Color(0.3f, 0.6f, 1f, 0.92f);
        private static readonly Color MoveHighlightColor    = new Color(0.3f, 0.6f, 1f, 0.92f);
        private static readonly Color AttackHighlightColor  = new Color(0.9f, 0.15f, 0.1f, 0.55f);
        private static readonly Color ZoneOfControlColor    = new Color(1f, 0.65f, 0.1f, 0.85f);
        private static readonly Color HoverHighlightColor   = new Color(0.79f, 0.64f, 0.35f, 0.8f);
        // rgb(243,230,200) — the Combat HUD's own cream "important text" tone (see
        // CombatHud.uss .unit-name/.stat-num). Reads as "selected" without colliding with
        // any other tile color already in use (blue=move, red=attack, amber=ZoC, gold=hover).
        private static readonly Color SelectionOutlineColor = new Color(0.953f, 0.902f, 0.784f, 0.95f);

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

        /// <summary>Assigns the terrain texture to the cube's MeshRenderer material and spawns
        /// any terrain props (trees, mountain peak) on top of it.</summary>
        public void Initialize(TerrainTile.TerrainType type)
        {
            TerrainType = type;
            var mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = TerrainTextureRegistry.GetTexture(type);
            GetComponent<MeshRenderer>().material = mat;

            SpawnTerrainProps(type);
        }

        // ------------------------------------------------------------------ //
        // Terrain props (trees, mountain peak)                                //
        // ------------------------------------------------------------------ //

        private static readonly Color TrunkColor    = new Color(0.36f, 0.24f, 0.14f);
        private static readonly Color CanopyColor    = new Color(0.10f, 0.32f, 0.11f);
        private static readonly Color PeakRockColor  = new Color(0.45f, 0.43f, 0.40f);
        private static readonly Color PeakCapColor   = new Color(0.88f, 0.90f, 0.92f);

        private void SpawnTerrainProps(TerrainTile.TerrainType type)
        {
            switch (type)
            {
                case TerrainTile.TerrainType.Forest:   SpawnForestProps();  break;
                case TerrainTile.TerrainType.Mountain: SpawnMountainProp(); break;
            }
        }

        /// <summary>2-3 small trees scattered around the tile. Parented to transform.parent
        /// (the grid root), same trick PlaceUnit uses below — this tile's own transform has
        /// non-uniform scale (TileSize, TileHeight, TileSize), so a prop parented directly to
        /// it would have its height squashed by TileHeight (0.25 by default).</summary>
        private void SpawnForestProps()
        {
            float tileWorldSize = transform.lossyScale.x;
            float halfTileH     = transform.lossyScale.y * 0.5f;
            Vector3 basePos     = transform.position + Vector3.up * halfTileH;

            int treeCount = Random.Range(2, 4);
            for (int i = 0; i < treeCount; i++)
            {
                Vector3 jitter = new Vector3(
                    Random.Range(-0.28f, 0.28f), 0f, Random.Range(-0.28f, 0.28f)) * tileWorldSize;
                BuildTree(basePos + jitter, tileWorldSize);
            }
        }

        private void BuildTree(Vector3 worldPos, float tileWorldSize)
        {
            var root = new GameObject("Tree");
            root.transform.SetParent(transform.parent, false);
            root.transform.position = worldPos;

            float trunkHeight = tileWorldSize * 0.22f;
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            Destroy(trunk.GetComponent<Collider>());
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            trunk.transform.localScale    = new Vector3(tileWorldSize * 0.045f, trunkHeight * 0.5f, tileWorldSize * 0.045f);
            trunk.GetComponent<MeshRenderer>().material = SolidMaterial(TrunkColor);

            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(root.transform, false);
            canopy.transform.localPosition = new Vector3(0f, trunkHeight * 0.85f, 0f);
            var canopyFilter   = canopy.AddComponent<MeshFilter>();
            canopyFilter.mesh  = ProceduralMeshFactory.CreateCone(tileWorldSize * 0.16f, tileWorldSize * 0.32f, 8);
            var canopyRenderer = canopy.AddComponent<MeshRenderer>();
            canopyRenderer.material = SolidMaterial(CanopyColor);
        }

        /// <summary>One two-tone peak (grey rock base, pale cap overlapping its upper third)
        /// centred on the tile. Same grid-root parenting as SpawnForestProps, for the same
        /// non-uniform-scale reason.</summary>
        private void SpawnMountainProp()
        {
            float tileWorldSize = transform.lossyScale.x;
            float halfTileH     = transform.lossyScale.y * 0.5f;
            Vector3 basePos     = transform.position + Vector3.up * halfTileH;

            var root = new GameObject("MountainPeak");
            root.transform.SetParent(transform.parent, false);
            root.transform.position = basePos;

            float baseRadius = tileWorldSize * 0.40f;
            float baseHeight = tileWorldSize * 0.60f;
            var baseCone = new GameObject("PeakBase");
            baseCone.transform.SetParent(root.transform, false);
            var baseFilter = baseCone.AddComponent<MeshFilter>();
            baseFilter.mesh = ProceduralMeshFactory.CreateCone(baseRadius, baseHeight, 10);
            baseCone.AddComponent<MeshRenderer>().material = SolidMaterial(PeakRockColor);

            float capRadius = baseRadius * 0.42f;
            float capHeight = baseHeight * 0.40f;
            var capCone = new GameObject("PeakCap");
            capCone.transform.SetParent(root.transform, false);
            capCone.transform.localPosition = new Vector3(0f, baseHeight * 0.66f, 0f);
            var capFilter = capCone.AddComponent<MeshFilter>();
            capFilter.mesh = ProceduralMeshFactory.CreateCone(capRadius, capHeight, 10);
            capCone.AddComponent<MeshRenderer>().material = SolidMaterial(PeakCapColor);
        }

        private static Material SolidMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            return mat;
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
        /// Shows or hides the near-opaque overlay used to indicate tiles the selected
        /// character can move to during the player's turn. Defaults to blue; callers pass
        /// a distinct color (e.g. amber) for a reachable tile that also lies within an
        /// enemy's Zone of Control, so leaving it costs extra without needing a second
        /// stacked overlay (see <see cref="SetZoneOfControlHighlight"/> for unreachable
        /// zone tiles, where no move overlay exists to color instead).
        /// </summary>
        public void SetMoveHighlight(bool active, Color? color = null)
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
                ApplyTransparency(mat, color ?? MoveHighlightColor);
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
        /// Shows or hides a thin amber line around this tile's top-face edges, marking it as
        /// inside an enemy's Zone of Control while it is NOT currently in the selected
        /// character's movable range — i.e. there is no <see cref="SetMoveHighlight"/> overlay
        /// here to recolor instead. An outline rather than a filled overlay so the warning
        /// reads clearly without covering the terrain underneath — less imposing than the
        /// original translucent quad, in keeping with the outline being the norm for tile
        /// highlighting now (see SetHoverHighlight/SetSelectionOutline).
        /// </summary>
        public void SetZoneOfControlHighlight(bool active) =>
            SetOutline(ref _zoneOfControlOutline, active, ZoneOfControlColor, 0.545f);

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

        /// <summary>
        /// Shows or hides a thin gold line around this tile's top-face edges, marking its
        /// occupant as the currently hovered/targeted unit — driven by TurnManager regardless
        /// of whether the hover came from the mouse over the 3D tile or a party-frame status
        /// bar (see TurnManager.SetHoveredTarget). Sits above every filled overlay (0.56) so
        /// it stays visible even while a move/attack/spell/zone-of-control highlight is also
        /// active, but below the selection outline (0.58).
        /// </summary>
        public void SetHoverHighlight(bool active) =>
            SetOutline(ref _hoverOutline, active, HoverHighlightColor, 0.56f);

        /// <summary>
        /// Shows or hides a thin cream line around this tile's top-face edges, marking it as
        /// the tile currently selected for inspection (see TurnManager.SetInspectedTile).
        /// The "norm" for tile highlighting going forward — thin edge outline rather than a
        /// filled overlay — so it can sit on top of every other highlight (0.58) without
        /// obscuring the terrain texture or whatever's underneath it.
        /// </summary>
        public void SetSelectionOutline(bool active) =>
            SetOutline(ref _selectionOutline, active, SelectionOutlineColor, 0.58f);

        /// <summary>Shared implementation for the hover/selection edge-outline highlights — a
        /// closed 4-point LineRenderer loop around the tile's top-face perimeter, parented the
        /// same way the filled overlays are so it inherits the tile's world-space footprint.</summary>
        private void SetOutline(ref GameObject outlineObj, bool active, Color color, float yOffset)
        {
            if (active)
            {
                if (outlineObj != null) return;

                outlineObj = new GameObject("TileOutline");
                outlineObj.transform.SetParent(transform, false);
                outlineObj.transform.localPosition = new Vector3(0f, yOffset, 0f);

                var lr = outlineObj.AddComponent<LineRenderer>();
                lr.useWorldSpace  = false;
                lr.loop           = true;
                lr.positionCount  = 4;
                lr.SetPositions(new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3( 0.5f, 0f, -0.5f),
                    new Vector3( 0.5f, 0f,  0.5f),
                    new Vector3(-0.5f, 0f,  0.5f),
                });
                lr.widthMultiplier = 0.045f;
                lr.material         = new Material(Shader.Find("Sprites/Default"));
                lr.startColor       = lr.endColor = color;
            }
            else if (outlineObj != null)
            {
                Destroy(outlineObj);
                outlineObj = null;
            }
        }

        // ------------------------------------------------------------------ //
        // Occupancy                                                           //
        // ------------------------------------------------------------------ //

        // Fallback sprite art (Winston's) for characters that don't yet have their own
        // SpriteKit set via Character.CharacterSpriteKit.
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
            view.Initialize(unit.CharacterSpriteKit ?? DefaultSpriteKit, unit is Npc);

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
    }
}