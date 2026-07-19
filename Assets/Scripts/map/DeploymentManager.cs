using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.GameController;

namespace BaaroForce.Map
{
    /// <summary>
    /// Manages the pre-battle deployment phase.
    /// Shows a 4×2 translucent blue zone at the bottom-centre of the map.
    /// Clicking an unoccupied deployment tile places the next party member.
    /// When all members are placed the overlay disappears.
    /// </summary>
    public class DeploymentManager : MonoBehaviour
    {
        private const int DeployWidth  = 4;
        private const int DeployHeight = 2;

        private MapTile[,] _tiles;
        private int         _gridSize;
        private float       _step;
        private float       _originX;
        private float       _originZ;

        private readonly List<MapTile>   _deploymentTiles   = new List<MapTile>();
        private readonly List<Character> _charactersToPlace = new List<Character>();
        private int _placedCount;

        private bool   _deploymentComplete;

        /// <summary>
        /// Fired when the last party member has been placed (or immediately if the
        /// party is empty).  MapGenerator subscribes TurnManager.StartPlayerTurn here.
        /// </summary>
        public System.Action OnDeploymentComplete;

        // ------------------------------------------------------------------ //
        // Initialisation (called by MapGenerator after the grid is built)     //
        // ------------------------------------------------------------------ //

        public void Initialize(MapTile[,] grid, int size, float tileStep,
                               float originWorldX, float originWorldZ)
        {
            _tiles   = grid;
            _gridSize = size;
            _step    = tileStep;
            _originX = originWorldX;
            _originZ = originWorldZ;

            _deploymentTiles.Clear();
            _charactersToPlace.Clear();
            _placedCount       = 0;
            _deploymentComplete = false;

            // Collect party members that still need to be placed.
            if (PartyManager.Instance?.Party?.Members != null)
                _charactersToPlace.AddRange(PartyManager.Instance.Party.Members);

            // No one to place — skip the deployment phase entirely.
            if (_charactersToPlace.Count == 0)
            {
                _deploymentComplete = true;
                OnDeploymentComplete?.Invoke();
                return;
            }

            MarkDeploymentZone();
        }

        // ------------------------------------------------------------------ //
        // Deployment zone setup                                               //
        // ------------------------------------------------------------------ //

        private void MarkDeploymentZone()
        {
            // Centre 4 columns, bottom 2 rows.
            int startX = _gridSize / 2 - DeployWidth / 2;
            int startZ = 0;

            for (int x = startX; x < startX + DeployWidth; x++)
            {
                for (int z = startZ; z < startZ + DeployHeight; z++)
                {
                    MapTile tile = _tiles[x, z];
                    tile.SetDeploymentZone(true);
                    _deploymentTiles.Add(tile);
                }
            }
        }

        private void ClearDeploymentOverlays()
        {
            foreach (MapTile tile in _deploymentTiles)
                tile.SetDeploymentZone(false);
        }

        // ------------------------------------------------------------------ //
        // Enemy pack placement                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Places each Npc in <paramref name="enemies"/> on a randomly chosen,
        /// unoccupied tile in the far half of the grid (the side opposite the
        /// player deployment zone).
        /// </summary>
        public void PlaceEnemyPack(List<Npc> enemies)
        {
            if (enemies == null || enemies.Count == 0) return;

            // Collect all unoccupied _tiles in the far half of the grid.
            var candidateTiles = new List<MapTile>();
            for (int x = 0; x < _gridSize; x++)
                for (int z = _gridSize / 2; z < _gridSize; z++)
                    if (!_tiles[x, z].IsOccupied)
                        candidateTiles.Add(_tiles[x, z]);

            // Fisher-Yates shuffle so selections are uniformly random.
            for (int i = candidateTiles.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                MapTile temp       = candidateTiles[i];
                candidateTiles[i]  = candidateTiles[j];
                candidateTiles[j]  = temp;
            }

            for (int i = 0; i < enemies.Count && i < candidateTiles.Count; i++)
                candidateTiles[i].PlaceUnit(enemies[i]);
        }

        // ------------------------------------------------------------------ //
        // Input                                                               //
        // ------------------------------------------------------------------ //

        private void Update()
        {
            if (_deploymentComplete) return;
            if (!Input.GetMouseButtonDown(0)) return;

            HandleClick();
        }

        private void HandleClick()
        {
            // Cast a ray from the isometric camera onto the flat XZ grid plane (Y = 0).
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

            if (!gridPlane.Raycast(ray, out float enter))
                return;

            Vector3 hitPoint = ray.GetPoint(enter);

            // Derive grid indices from the XZ hit position.
            int gridX = Mathf.RoundToInt((hitPoint.x - _originX) / _step);
            int gridZ = Mathf.RoundToInt((hitPoint.z - _originZ) / _step);

            if (gridX < 0 || gridX >= _gridSize || gridZ < 0 || gridZ >= _gridSize)
                return;

            MapTile tile = _tiles[gridX, gridZ];

            if (!tile.IsInDeploymentZone || tile.IsOccupied)
                return;

            // Place the next queued character.
            Character character = _charactersToPlace[_placedCount];
            tile.PlaceUnit(character);
            _placedCount++;

            if (_placedCount >= _charactersToPlace.Count)
            {
                _deploymentComplete = true;
                ClearDeploymentOverlays();
                OnDeploymentComplete?.Invoke();
            }
        }
    }
}
