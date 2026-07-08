using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>
    /// Procedurally generates a 2D square-tile grid whose terrain distribution
    /// is driven by the chosen Realm and MapSize.
    /// 
    /// Scene setup:
    ///   1. Create a GameObject named "MapGenerator" and attach this script.
    ///   2. Set the Main Camera to Orthographic (recommended: Clear Flags = Solid Color).
    ///   3. Press Play — the map generates automatically.
    ///   4. Right-click this component in the Inspector → "Regenerate Map" to rebuild.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        public MapSize mapSize   = MapSize.SMALL;
        public Realm   realmType = Realm.EARTH;

        [Header("Tile Appearance")]
        [Tooltip("World-unit side length of each tile.")]
        public float tileSize = 1f;
        [Tooltip("Gap between tiles in world units.")]
        public float tileGap  = 0.05f;

        // Shared 1×1 white sprite — created once, reused for every tile.
        private Sprite tileSprite;
        private MapTile[,] tiles;
        private DeploymentManager deploymentManager;

        private void Start()
        {
            // Use the session realm from PartyManager when running from the normal game flow.
            // Falls back to the Inspector value when testing MapScene in isolation.
            if (PartyManager.Instance.CurrentRealm.HasValue)
                realmType = PartyManager.Instance.CurrentRealm.Value;

            GenerateMap();
        }

        [ContextMenu("Regenerate Map")]
        public void GenerateMap()
        {
            ClearExistingTiles();
            CreateSharedSprite();

            int size = (int)mapSize;
            tiles = new MapTile[size, size];

            List<TerrainTile.TerrainType> pool = BuildWeightedPool(
                RealmTerrainWeights.GetWeights(realmType));

            float step    = tileSize + tileGap;
            float originX = -(size * step) / 2f + step / 2f;
            float originY = -(size * step) / 2f + step / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    TerrainTile.TerrainType terrain = pool[Random.Range(0, pool.Count)];
                    tiles[x, y] = SpawnTile(x, y, terrain,
                                            originX + x * step,
                                            originY + y * step);
                }
            }

            FitCameraToMap(size, step);

            // Set up the deployment phase.
            if (deploymentManager == null)
                deploymentManager = gameObject.AddComponent<DeploymentManager>();
            deploymentManager.Initialize(tiles, size, step, originX, originY);
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                              //
        // ------------------------------------------------------------------ //

        private MapTile SpawnTile(int x, int y, TerrainTile.TerrainType terrain,
                                  float worldX, float worldY)
        {
            GameObject obj = new GameObject($"Tile_{x}_{y}");
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = new Vector3(worldX, worldY, 0f);
            obj.transform.localScale    = new Vector3(tileSize, tileSize, 1f);

            // SpriteRenderer is added by [RequireComponent] on MapTile.
            MapTile tile = obj.AddComponent<MapTile>();
            tile.Initialize(terrain, tileSprite);
            return tile;
        }

        /// <summary>Builds a flat list where each entry appears <c>weight</c> times.</summary>
        private static List<TerrainTile.TerrainType> BuildWeightedPool(
            Dictionary<TerrainTile.TerrainType, int> weights)
        {
            var pool = new List<TerrainTile.TerrainType>();
            foreach (var pair in weights)
            {
                for (int i = 0; i < pair.Value; i++)
                    pool.Add(pair.Key);
            }
            return pool;
        }

        /// <summary>One shared 1×1 white sprite that tiles colour themselves.</summary>
        private void CreateSharedSprite()
        {
            if (tileSprite != null) return;

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            // pixelsPerUnit = 1 → the sprite is exactly 1 Unity unit wide/tall.
            tileSprite = Sprite.Create(tex,
                                       new Rect(0, 0, 1, 1),
                                       new Vector2(0.5f, 0.5f),
                                       1f);
        }

        private void ClearExistingTiles()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            tiles             = null;
            tileSprite        = null;
            deploymentManager = null;   // AddComponent re-creates it on next GenerateMap()
            DeploymentManager existing = GetComponent<DeploymentManager>();
            if (existing != null) DestroyImmediate(existing);
        }

        /// <summary>
        /// Repositions and sizes an Orthographic camera to frame the entire grid
        /// with a small margin.
        /// </summary>
        private void FitCameraToMap(int size, float step)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float halfMapW = (size * step) / 2f;
            float halfMapH = (size * step) / 2f;
            float margin   = step;

            float neededVertical   = halfMapH + margin;
            float neededHorizontal = (halfMapW + margin) / cam.aspect;

            cam.orthographicSize   = Mathf.Max(neededVertical, neededHorizontal);
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
