using UnityEngine;
using BaaroForce.Characters;

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
        public Character           OccupyingCharacter    { get; private set; }
        public NPC                 OccupyingNpc          { get; private set; }
        public bool                IsOccupied            => OccupyingCharacter != null || OccupyingNpc != null;

        /// <summary>Column index in the grid array.</summary>
        public int GridX { get; private set; }
        /// <summary>Row index in the grid array.</summary>
        public int GridZ { get; private set; }

        /// <summary>The instantiated player-character model on this tile (may be null).</summary>
        public GameObject CharacterObject => characterObject;
        /// <summary>The instantiated NPC model on this tile (may be null).</summary>
        public GameObject NpcObject => npcObject;

        // ------------------------------------------------------------------ //
        // Private state                                                       //
        // ------------------------------------------------------------------ //

        private GameObject characterObject;
        private GameObject npcObject;
        private GameObject deploymentOverlay;
        private GameObject moveHighlightOverlay;
        private GameObject attackHighlightOverlay;
        private GameObject spellHighlightOverlay;

        private static readonly Color OverlayColor         = new Color(0.3f, 0.6f, 1f, 0.45f);
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
                if (deploymentOverlay != null) return;

                deploymentOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                deploymentOverlay.name = "DeploymentOverlay";
                Destroy(deploymentOverlay.GetComponent<Collider>());

                deploymentOverlay.transform.SetParent(transform, false);
                // Quad faces +Z by default; rotate 90° on X so it lies flat on the top face.
                // In tile-local space the top face is at y = 0.5; use 0.52 to avoid Z-fighting.
                deploymentOverlay.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                deploymentOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                deploymentOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, OverlayColor);
                deploymentOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (deploymentOverlay != null)
                {
                    Destroy(deploymentOverlay);
                    deploymentOverlay = null;
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
                if (moveHighlightOverlay != null) return;

                moveHighlightOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                moveHighlightOverlay.name = "MoveHighlight";
                Destroy(moveHighlightOverlay.GetComponent<Collider>());

                moveHighlightOverlay.transform.SetParent(transform, false);
                // Sit slightly above the deployment overlay (0.52) to avoid Z-fighting.
                moveHighlightOverlay.transform.localPosition = new Vector3(0f, 0.54f, 0f);
                moveHighlightOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                moveHighlightOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, MoveHighlightColor);
                moveHighlightOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (moveHighlightOverlay != null)
                {
                    Destroy(moveHighlightOverlay);
                    moveHighlightOverlay = null;
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
                if (attackHighlightOverlay != null) return;

                attackHighlightOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                attackHighlightOverlay.name = "AttackHighlight";
                Destroy(attackHighlightOverlay.GetComponent<Collider>());

                attackHighlightOverlay.transform.SetParent(transform, false);
                attackHighlightOverlay.transform.localPosition = new Vector3(0f, 0.54f, 0f);
                attackHighlightOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                attackHighlightOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, AttackHighlightColor);
                attackHighlightOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (attackHighlightOverlay != null)
                {
                    Destroy(attackHighlightOverlay);
                    attackHighlightOverlay = null;
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
                if (spellHighlightOverlay != null) return;

                spellHighlightOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
                spellHighlightOverlay.name = "SpellHighlight";
                Destroy(spellHighlightOverlay.GetComponent<Collider>());

                spellHighlightOverlay.transform.SetParent(transform, false);
                spellHighlightOverlay.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                spellHighlightOverlay.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                spellHighlightOverlay.transform.localScale    = Vector3.one;

                var mat = new Material(Shader.Find("Standard"));
                ApplyTransparency(mat, color);
                spellHighlightOverlay.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                if (spellHighlightOverlay != null)
                {
                    Destroy(spellHighlightOverlay);
                    spellHighlightOverlay = null;
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Occupancy                                                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Loads the character's 3D model and places it on top of this tile.
        /// Parented to the grid root (tile's parent) to avoid non-uniform scale distortion.
        /// </summary>
        public void PlaceCharacter(Character character)
        {
            if (IsOccupied) return;
            OccupyingCharacter = character;

            var prefab = Resources.Load<GameObject>(character.characterModelPath);
            if (prefab != null)
            {
                characterObject = Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning($"[MapTile] Model not found at '{character.characterModelPath}'. Using fallback.");
                characterObject = CreateFallback();
            }

            characterObject.name = $"Character_{character.characterName}";

            // Parent to the MapGenerator (tile's parent) — it has uniform scale (1,1,1),
            // so world-space and local-space transforms are equivalent.
            characterObject.transform.SetParent(transform.parent, false);

            // Sit model on top of this tile in world space.
            float halfTileH = transform.lossyScale.y * 0.5f;
            characterObject.transform.position = transform.position + Vector3.up * (halfTileH + 0.05f);

            // Scale to 80 % of the tile's world footprint, preserving model proportions.
            ScaleToFit(characterObject, transform.lossyScale.x * 0.8f);
        }

        /// <summary>Removes the occupying character from this tile and destroys its model.</summary>
        public void RemoveCharacter()
        {
            OccupyingCharacter = null;
            if (characterObject != null)
            {
                Destroy(characterObject);
                characterObject = null;
            }
        }

        /// <summary>
        /// Clears occupancy without destroying the model GameObject.
        /// Used by TurnManager during movement animation so the model can be
        /// handed off to the destination tile.
        /// </summary>
        public void ReleaseCharacter()
        {
            OccupyingCharacter = null;
            characterObject    = null;   // we no longer own this reference
        }

        /// <summary>
        /// Assigns an already-existing character model to this tile.
        /// Called by TurnManager after the model has been moved in world-space.
        /// </summary>
        public void AssignCharacter(Character character, GameObject model)
        {
            OccupyingCharacter = character;
            characterObject    = model;
        }

        /// <summary>
        /// Loads the NPC's 3D model and places it on top of this tile.
        /// Parented to the grid root (tile's parent) to avoid non-uniform scale distortion.
        /// </summary>
        public void PlaceNpc(NPC npc)
        {
            if (IsOccupied) return;
            OccupyingNpc = npc;

            var prefab = Resources.Load<GameObject>(npc.characterModelPath);
            if (prefab != null)
            {
                npcObject = Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning($"[MapTile] NPC model not found at '{npc.characterModelPath}'. Using fallback.");
                npcObject = CreateFallback();
            }

            npcObject.name = $"NPC_{npc.characterName}";
            npcObject.transform.SetParent(transform.parent, false);

            float halfTileH = transform.lossyScale.y * 0.5f;
            npcObject.transform.position = transform.position + Vector3.up * (halfTileH + 0.05f);

            ScaleToFit(npcObject, transform.lossyScale.x * 0.8f);
        }

        /// <summary>Removes the occupying NPC from this tile.</summary>
        public void RemoveNpc()
        {
            OccupyingNpc = null;
            if (npcObject != null)
            {
                Destroy(npcObject);
                npcObject = null;
            }
        }

        /// <summary>
        /// Clears NPC occupancy without destroying the model GameObject.
        /// Used by TurnManager during NPC movement animation so the model can be
        /// handed off to the destination tile.
        /// </summary>
        public void ReleaseNpc()
        {
            OccupyingNpc = null;
            npcObject    = null;   // we no longer own this reference
        }

        /// <summary>
        /// Assigns an already-existing NPC model to this tile.
        /// Called by TurnManager after the model has been moved in world-space.
        /// </summary>
        public void AssignNpc(NPC npc, GameObject model)
        {
            OccupyingNpc = npc;
            npcObject    = model;
        }

        // ------------------------------------------------------------------ //
        // Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private static void ScaleToFit(GameObject obj, float targetWorldSize)
        {
            obj.transform.localScale = Vector3.one;   // reset before measuring
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                obj.transform.localScale = Vector3.one * targetWorldSize;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);

            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            obj.transform.localScale = Vector3.one * (maxDim > 0f ? targetWorldSize / maxDim : 1f);
        }

        private static GameObject CreateFallback()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Standard")) { color = Color.magenta };
            go.GetComponent<MeshRenderer>().material = mat;
            return go;
        }

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
                case TerrainTile.TerrainType.GRASS:    return new Color(0.27f, 0.65f, 0.16f);
                case TerrainTile.TerrainType.FOREST:   return new Color(0.07f, 0.35f, 0.07f);
                case TerrainTile.TerrainType.MOUNTAIN: return new Color(0.55f, 0.52f, 0.47f);
                case TerrainTile.TerrainType.WATER:    return new Color(0.12f, 0.46f, 0.78f);
                case TerrainTile.TerrainType.DESERT:   return new Color(0.93f, 0.84f, 0.45f);
                case TerrainTile.TerrainType.SWAMP:    return new Color(0.28f, 0.33f, 0.13f);
                case TerrainTile.TerrainType.VOLCANO:  return new Color(0.72f, 0.16f, 0.05f);
                case TerrainTile.TerrainType.SNOW:     return new Color(0.92f, 0.96f, 1.00f);
                case TerrainTile.TerrainType.PLAINS:   return new Color(0.70f, 0.84f, 0.40f);
                case TerrainTile.TerrainType.VOID:     return new Color(0.10f, 0.03f, 0.18f);
                case TerrainTile.TerrainType.ASH:      return new Color(0.50f, 0.50f, 0.50f);
                case TerrainTile.TerrainType.LAVA:     return new Color(0.80f, 0.20f, 0.00f);
                case TerrainTile.TerrainType.TUNDRA:   return new Color(0.85f, 0.90f, 0.95f);
                case TerrainTile.TerrainType.CREEK:    return new Color(0.15f, 0.40f, 0.70f);
                case TerrainTile.TerrainType.OCEAN:    return new Color(0.05f, 0.25f, 0.50f);
                case TerrainTile.TerrainType.MEADOW:   return new Color(0.30f, 0.70f, 0.20f);
                default:                               return Color.white;
            }
        }
    }
}
