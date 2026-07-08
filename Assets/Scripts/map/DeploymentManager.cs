using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;

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

        private MapTile[,] tiles;
        private int         gridSize;
        private float       step;
        private float       originX;
        private float       originY;

        private readonly List<MapTile>   deploymentTiles   = new List<MapTile>();
        private readonly List<Character> charactersToPlace = new List<Character>();
        private int placedCount;

        private bool   deploymentComplete;

        // ------------------------------------------------------------------ //
        // Initialisation (called by MapGenerator after the grid is built)     //
        // ------------------------------------------------------------------ //

        public void Initialize(MapTile[,] grid, int size, float tileStep,
                               float originWorldX, float originWorldY)
        {
            tiles   = grid;
            gridSize = size;
            step    = tileStep;
            originX = originWorldX;
            originY = originWorldY;

            deploymentTiles.Clear();
            charactersToPlace.Clear();
            placedCount       = 0;
            deploymentComplete = false;

            // Collect party members that still need to be placed.
            if (PartyManager.Instance?.Party?.members != null)
                charactersToPlace.AddRange(PartyManager.Instance.Party.members);

            // No one to place — skip the deployment phase entirely.
            if (charactersToPlace.Count == 0)
            {
                deploymentComplete = true;
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
            int startX = gridSize / 2 - DeployWidth / 2;   // e.g. 8/2-2 = 2 for small map
            int startY = 0;

            for (int x = startX; x < startX + DeployWidth; x++)
            {
                for (int y = startY; y < startY + DeployHeight; y++)
                {
                    MapTile tile = tiles[x, y];
                    tile.SetDeploymentZone(true);
                    deploymentTiles.Add(tile);
                }
            }
        }

        private void ClearDeploymentOverlays()
        {
            foreach (MapTile tile in deploymentTiles)
                tile.SetDeploymentZone(false);
        }

        // ------------------------------------------------------------------ //
        // Input                                                               //
        // ------------------------------------------------------------------ //

        private void Update()
        {
            if (deploymentComplete) return;
            if (!Input.GetMouseButtonDown(0)) return;

            HandleClick();
        }

        private void HandleClick()
        {
            // Convert screen position to world position (orthographic camera).
            Vector3 screenPos = new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                -Camera.main.transform.position.z);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            // Derive grid indices from world position.
            int gridX = Mathf.RoundToInt((worldPos.x - originX) / step);
            int gridY = Mathf.RoundToInt((worldPos.y - originY) / step);

            if (gridX < 0 || gridX >= gridSize || gridY < 0 || gridY >= gridSize)
                return;

            MapTile tile = tiles[gridX, gridY];

            if (!tile.IsInDeploymentZone || tile.IsOccupied)
                return;

            // Place the next queued character.
            Character character = charactersToPlace[placedCount];
            tile.PlaceCharacter(character);
            placedCount++;

            if (placedCount >= charactersToPlace.Count)
            {
                deploymentComplete = true;
                ClearDeploymentOverlays();
            }
        }
    }
}
