using UnityEngine;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>
    /// Represents a single square tile on the map.
    /// Tracks terrain type, occupancy, and deployment-zone membership.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
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

        private SpriteRenderer spriteRenderer;
        private GameObject     deploymentOverlay;
        private GameObject     characterObject;

        private static readonly Color OverlayColor = new Color(0.3f, 0.6f, 1f, 0.45f);

        // ------------------------------------------------------------------ //
        // Lifecycle                                                           //
        // ------------------------------------------------------------------ //

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // ------------------------------------------------------------------ //
        // Initialisation                                                      //
        // ------------------------------------------------------------------ //

        public void Initialize(TerrainTile.TerrainType type, Sprite sharedSprite)
        {
            TerrainType = type;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            spriteRenderer.sprite       = sharedSprite;
            spriteRenderer.color        = GetTerrainColor(type);
            spriteRenderer.sortingOrder = 0;
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

                deploymentOverlay = new GameObject("DeploymentOverlay");
                deploymentOverlay.transform.SetParent(transform, false);
                deploymentOverlay.transform.localPosition = Vector3.zero;
                deploymentOverlay.transform.localScale    = Vector3.one;

                SpriteRenderer or = deploymentOverlay.AddComponent<SpriteRenderer>();
                or.sprite       = spriteRenderer.sprite;   // reuse the shared 1×1 white square
                or.color        = OverlayColor;
                or.sortingOrder = 1;
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

        /// <summary>Spawns a character portrait on this tile and marks it occupied.</summary>
        public void PlaceCharacter(Character character)
        {
            if (IsOccupied) return;

            OccupyingCharacter = character;

            characterObject = new GameObject($"Character_{character.characterName}");
            characterObject.transform.SetParent(transform, false);
            characterObject.transform.localPosition = Vector3.zero;

            Sprite portrait = Resources.Load<Sprite>(character.characterImagePath);

            SpriteRenderer cr = characterObject.AddComponent<SpriteRenderer>();
            cr.sprite       = portrait;
            cr.sortingOrder = 2;

            if (portrait != null)
            {
                // Scale portrait to fill 90 % of the tile, preserving aspect ratio.
                float spriteMax = Mathf.Max(portrait.bounds.size.x, portrait.bounds.size.y);
                float charScale = (spriteMax > 0f) ? 0.9f / spriteMax : 1f;
                characterObject.transform.localScale = new Vector3(charScale, charScale, 1f);
            }
            else
            {
                Debug.LogWarning($"[MapTile] Sprite not found at path: '{character.characterImagePath}'");
            }
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
        // Helpers                                                             //
        // ------------------------------------------------------------------ //

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
