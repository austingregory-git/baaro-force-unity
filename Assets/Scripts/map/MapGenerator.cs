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
            SetupSceneLight();

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
        /// Creates (or reuses) a Directional Light that illuminates the whole map.
        /// A Directional Light works like the sun — it has no position, only a direction,
        /// and every surface in the scene receives the same amount of light from that angle.
        /// </summary>
        private void SetupSceneLight()
        {
            // ----------------------------------------------------------------
            // Step 1 — Avoid duplicates.
            // If this method is called more than once (e.g. via "Regenerate Map")
            // we don't want to keep stacking new lights on top of each other.
            // We search the scene for any Light component whose type is Directional.
            // Light.GetAllLights() isn't available in older Unity versions, so we use
            // FindObjectOfType which searches every active GameObject in the scene.
            // ----------------------------------------------------------------
            Light existingLight = FindObjectOfType<Light>();

            // If a Directional Light already exists, there is nothing to do — bail out early.
            if (existingLight != null && existingLight.type == LightType.Directional)
                return;

            // ----------------------------------------------------------------
            // Step 2 — Create the GameObject that will hold our light component.
            // In Unity every component must live on a GameObject, so we make one
            // specifically for the light and give it a descriptive name.
            // ----------------------------------------------------------------
            GameObject lightGO = new GameObject("Map Directional Light");

            // ----------------------------------------------------------------
            // Step 3 — Add the Light component to our new GameObject.
            // AddComponent<T>() attaches a Unity component of type T and returns
            // the newly created instance so we can configure it immediately.
            // ----------------------------------------------------------------
            Light dirLight = lightGO.AddComponent<Light>();

            // ----------------------------------------------------------------
            // Step 4 — Set the light TYPE to Directional.
            // Unity supports several light types (Point, Spot, Area, Directional).
            // Directional is ideal for maps because it illuminates everything
            // equally regardless of distance, just like sunlight.
            // ----------------------------------------------------------------
            dirLight.type = LightType.Directional;

            // ----------------------------------------------------------------
            // Step 5 — Set the colour.
            // Color.white (1,1,1,1) is pure white, which gives neutral lighting
            // that won't tint your tile colours.  You could use a warm colour
            // like new Color(1f, 0.95f, 0.8f) for a sunlit afternoon look.
            // ----------------------------------------------------------------
            dirLight.color = Color.white;

            // ----------------------------------------------------------------
            // Step 6 — Set the brightness (intensity).
            // 1.0f is Unity's default "daylight" brightness.  Raise it above 1
            // if the scene still looks dark, or lower it for a dimmer mood.
            // ----------------------------------------------------------------
            dirLight.intensity = 1.2f;

            // ----------------------------------------------------------------
            // Step 7 — Rotate the light to choose the angle it shines from.
            // Rotation is expressed as Euler angles (pitch, yaw, roll) in degrees.
            // • X = 50  tilts it ~50° downward (straight down would be 90°)
            //           so tiles AND their side faces both get some light.
            // • Y = -30 rotates it slightly to the left, creating angled shadows
            //           that give the isometric grid a sense of depth.
            // • Z = 0   no roll needed.
            // Quaternion.Euler() converts those three angles into the internal
            // quaternion format Unity uses to store rotations.
            // ----------------------------------------------------------------
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ----------------------------------------------------------------
            // Step 8 — Enable shadows (optional but recommended for depth).
            // ShadowType.SoftShadows produces smooth-edged shadows which look
            // nicer on a stylised map than the harder ShadowType.HardShadows.
            // Set to ShadowType.None if you want maximum performance.
            // ----------------------------------------------------------------
            dirLight.shadows = LightShadows.Soft;
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
