using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;

namespace BaaroForce.Map
{
    /// <summary>
    /// Procedurally generates a 3D isometric cube-tile grid whose terrain distribution
    /// is driven by the chosen Realm and MapSize.
    ///
    /// Scene setup:
    ///   1. Attach this script to a GameObject named "MapGenerator".
    ///   2. Press Play — the grid and isometric camera are configured automatically.
    ///   3. Right-click this component → "Regenerate Map" to rebuild without entering Play.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        public MapSize mapSize   = MapSize.SMALL;
        public Realm   realmType = Realm.EARTH;

        [Header("Tile Appearance")]
        [Tooltip("World-unit side length of each cube tile.")]
        public float tileSize   = 1f;
        [Tooltip("World-unit height (Y) of each cube tile.")]
        public float tileHeight = 0.25f;
        [Tooltip("Gap between tiles in world units.")]
        public float tileGap    = 0.05f;

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

            int size = (int)mapSize;
            tiles = new MapTile[size, size];

            List<TerrainTile.TerrainType> pool = BuildWeightedPool(
                RealmTerrainWeights.GetWeights(realmType));

            float step    = tileSize + tileGap;
            float originX = -(size * step) / 2f + step / 2f;
            float originZ = -(size * step) / 2f + step / 2f;

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    TerrainTile.TerrainType terrain = pool[Random.Range(0, pool.Count)];
                    tiles[x, z] = SpawnTile(x, z, terrain, originX + x * step, originZ + z * step);
                }
            }

            FitCameraToMap(size, step);

            // Set up the deployment phase.
            if (deploymentManager == null)
                deploymentManager = gameObject.AddComponent<DeploymentManager>();
            deploymentManager.Initialize(tiles, size, step, originX, originZ);
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                              //
        // ------------------------------------------------------------------ //

        private MapTile SpawnTile(int x, int z, TerrainTile.TerrainType terrain,
                                  float worldX, float worldZ)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = $"Tile_{x}_{z}";
            obj.transform.SetParent(transform, false);
            // Grid lies on the XZ plane; Y=0 centres the cube so its top face is at Y = tileHeight/2.
            obj.transform.localPosition = new Vector3(worldX, 0f, worldZ);
            obj.transform.localScale    = new Vector3(tileSize, tileHeight, tileSize);
            Destroy(obj.GetComponent<Collider>());

            MapTile tile = obj.AddComponent<MapTile>();
            tile.Initialize(terrain);
            return tile;
        }

        private static List<TerrainTile.TerrainType> BuildWeightedPool(
            Dictionary<TerrainTile.TerrainType, int> weights)
        {
            var pool = new List<TerrainTile.TerrainType>();
            foreach (var pair in weights)
                for (int i = 0; i < pair.Value; i++)
                    pool.Add(pair.Key);
            return pool;
        }

        private void ClearExistingTiles()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            tiles             = null;
            deploymentManager = null;
            DeploymentManager existing = GetComponent<DeploymentManager>();
            if (existing != null) DestroyImmediate(existing);
        }

        /// <summary>
        /// Positions the main camera for a classic isometric view centred on the grid.
        /// Placing the camera at (d, d, d) and looking at the origin gives the standard
        /// 45° azimuth / 35.26° elevation isometric projection.
        /// </summary>
        private void FitCameraToMap(int size, float step)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float gridWorldSize = size * step;
            float dist          = gridWorldSize * 1.2f;

            cam.transform.position = new Vector3(dist, dist, dist);
            cam.transform.LookAt(Vector3.zero, Vector3.up);

            cam.orthographic     = true;
            cam.orthographicSize = gridWorldSize * 0.75f;
        }
    }
}
