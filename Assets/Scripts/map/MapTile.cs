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

        public TerrainTile.TerrainType TerrainType   { get; private set; }
        public bool                IsInDeploymentZone { get; private set; }
        public bool                IsOccupied    => OccupyingCharacter != null;
        public Character           OccupyingCharacter { get; private set; }

        // ------------------------------------------------------------------ //
        // Private state                                                       //
        // ------------------------------------------------------------------ //

        private GameObject characterObject;
        private GameObject deploymentOverlay;

        private static readonly Color OverlayColor = new Color(0.3f, 0.6f, 1f, 0.45f);

        // ------------------------------------------------------------------ //
        // Lifecycle                                                           //
        // ------------------------------------------------------------------ //

        // No Awake needed — MeshRenderer/MeshFilter come from CreatePrimitive.

        // ------------------------------------------------------------------ //
        // Initialisation                                                      //
        // ------------------------------------------------------------------ //

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

        /// <summary>Removes the occupying character from this tile.</summary>
        public void RemoveCharacter()
        {
            OccupyingCharacter = null;
            if (characterObject != null)
            {
                Destroy(characterObject);
                characterObject = null;
            }
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
