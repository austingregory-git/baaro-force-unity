using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaaroForce.Characters;
using BaaroForce.Classes;

namespace BaaroForce.Map
{
    public enum TurnPhase { Deployment, PlayerTurn, EnemyTurn }

    /// <summary>
    /// Controls the battle turn loop.
    ///
    /// Flow:  DeploymentManager fires OnDeploymentComplete
    ///        → StartPlayerTurn()
    ///        → player selects a unit by clicking it
    ///        → W/M = move mode, A = attack, S = spells, D/I = items
    ///
    /// Movement:
    ///   • W/M highlights reachable tiles (blue, near-opaque) via BFS.
    ///   • Clicking a highlighted tile moves the character along the grid
    ///     at a steady speed, tile by tile, with no diagonal movement.
    ///   • Movement points are deducted by steps taken.
    ///   • The character may still act after moving.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        // Grid reference (set by MapGenerator.Initialize)                    //
        // ------------------------------------------------------------------ //

        private MapTile[,] tiles;
        private int         gridSize;
        private float       step;
        private float       originX;
        private float       originZ;

        // ------------------------------------------------------------------ //
        // Turn state                                                          //
        // ------------------------------------------------------------------ //

        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Deployment;

        private Character selectedCharacter;
        private MapTile   selectedTile;

        /// <summary>Movement points remaining this turn, keyed by character.</summary>
        private readonly Dictionary<Character, int> remainingMovement =
            new Dictionary<Character, int>();

        /// <summary>Action points remaining this turn, keyed by character.</summary>
        private readonly Dictionary<Character, int> remainingActions =
            new Dictionary<Character, int>();

        /// <summary>Characters who have already used all their resources this turn.</summary>
        private readonly HashSet<Character> finishedCharacters = new HashSet<Character>();

        // ------------------------------------------------------------------ //
        // Input mode and highlights                                           //
        // ------------------------------------------------------------------ //

        private enum InputMode { None, Move, Attack }
        private InputMode currentMode = InputMode.None;

        private readonly List<MapTile> highlightedMoveTiles   = new List<MapTile>();
        private readonly List<MapTile> highlightedAttackTiles = new List<MapTile>();

        private bool isMoving;

        private const float MoveSpeed = 5f;   // world-units per second

        // ------------------------------------------------------------------ //
        // Initialisation                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Called by MapGenerator after building the grid.</summary>
        public void Initialize(MapTile[,] grid, int size, float tileStep,
                               float originWorldX, float originWorldZ)
        {
            tiles    = grid;
            gridSize = size;
            step     = tileStep;
            originX  = originWorldX;
            originZ  = originWorldZ;
        }

        // ------------------------------------------------------------------ //
        // Turn lifecycle                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called by DeploymentManager.OnDeploymentComplete.
        /// Resets movement budgets for all party members and hands control to the player.
        /// </summary>
        public void StartPlayerTurn()
        {
            CurrentPhase = TurnPhase.PlayerTurn;
            remainingMovement.Clear();
            remainingActions.Clear();
            finishedCharacters.Clear();

            var members = PartyManager.Instance?.Party?.members;
            if (members != null)
                foreach (Character c in members)
                {
                    remainingMovement[c] = c.characterStats.movement;
                    remainingActions[c]  = c.characterStats.maxActionPoints;
                }

            Debug.Log("[TurnManager] Player turn started.");
        }

        // ------------------------------------------------------------------ //
        // Unity update                                                        //
        // ------------------------------------------------------------------ //

        private void Update()
        {
            if (CurrentPhase != TurnPhase.PlayerTurn) return;
            if (isMoving) return;

            HandleClick();
            HandleKeys();
        }

        // ------------------------------------------------------------------ //
        // Click handling                                                      //
        // ------------------------------------------------------------------ //

        private void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (!TryGetClickedTile(out MapTile clicked)) return;

            switch (currentMode)
            {
                case InputMode.Move:
                    if (highlightedMoveTiles.Contains(clicked))
                        CommitMove(clicked);
                    else
                        SetMode(InputMode.None);
                    break;

                case InputMode.Attack:
                    if (highlightedAttackTiles.Contains(clicked) && clicked.OccupyingNpc != null)
                        CommitAttack(clicked);
                    else
                        SetMode(InputMode.None);
                    break;

                default:
                    if (clicked.OccupyingCharacter != null)
                        SelectCharacter(clicked.OccupyingCharacter, clicked);
                    else
                        Deselect();
                    break;
            }
        }

        private bool TryGetClickedTile(out MapTile tile)
        {
            tile = null;
            Ray   ray       = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
            if (!gridPlane.Raycast(ray, out float enter)) return false;

            Vector3 hit   = ray.GetPoint(enter);
            int     gridX = Mathf.RoundToInt((hit.x - originX) / step);
            int     gridZ = Mathf.RoundToInt((hit.z - originZ) / step);

            if (gridX < 0 || gridX >= gridSize || gridZ < 0 || gridZ >= gridSize) return false;
            tile = tiles[gridX, gridZ];
            return true;
        }

        // ------------------------------------------------------------------ //
        // Key handling                                                        //
        // ------------------------------------------------------------------ //

        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { SetMode(InputMode.None); return; }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (selectedCharacter != null) EndCharacterTurn(selectedCharacter);
                return;
            }

            if (selectedCharacter == null) return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.M))
                ToggleMoveMode();
            else if (Input.GetKeyDown(KeyCode.A))
                ToggleAttackMode();
            else if (Input.GetKeyDown(KeyCode.S))
                Debug.Log("[TurnManager] Spells — not yet implemented.");
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.I))
                Debug.Log("[TurnManager] Items — not yet implemented.");
        }

        // ------------------------------------------------------------------ //
        // Selection                                                           //
        // ------------------------------------------------------------------ //

        private void SelectCharacter(Character character, MapTile tile)
        {
            if (finishedCharacters.Contains(character))
            {
                Debug.Log($"[TurnManager] '{character.characterName}' has already acted this turn.");
                return;
            }
            SetMode(InputMode.None);
            selectedCharacter = character;
            selectedTile      = tile;
            Debug.Log($"[TurnManager] Selected '{character.characterName}'  " +
                      $"MP: {RemainingMove(character)}  AP: {RemainingActions(character)}");
        }

        private void Deselect()
        {
            SetMode(InputMode.None);
            selectedCharacter = null;
            selectedTile      = null;
        }

        // ------------------------------------------------------------------ //
        // Mode management                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>Switches input mode, clearing all highlights from the previous mode.</summary>
        private void SetMode(InputMode mode)
        {
            ClearMoveHighlights();
            ClearAttackHighlights();
            currentMode = mode;
        }

        // ------------------------------------------------------------------ //
        // Move mode                                                           //
        // ------------------------------------------------------------------ //

        private void ToggleMoveMode()
        {
            if (currentMode == InputMode.Move) { SetMode(InputMode.None); return; }

            int mp = RemainingMove(selectedCharacter);
            if (mp <= 0)
            {
                Debug.Log($"[TurnManager] '{selectedCharacter.characterName}' has no movement remaining.");
                return;
            }

            SetMode(InputMode.Move);
            ShowMovableRange(selectedTile, mp);
        }

        private void ShowMovableRange(MapTile origin, int range)
        {
            foreach (MapTile tile in BfsReachable(origin, range))
            {
                if (tile == origin) continue;
                tile.SetMoveHighlight(true);
                highlightedMoveTiles.Add(tile);
            }
        }

        private void ClearMoveHighlights()
        {
            foreach (MapTile t in highlightedMoveTiles)
                t.SetMoveHighlight(false);
            highlightedMoveTiles.Clear();
        }

        private void CommitMove(MapTile destination)
        {
            SetMode(InputMode.None);
            List<MapTile> path = FindShortestPath(selectedTile, destination);
            int cost = path.Count - 1;
            remainingMovement[selectedCharacter] =
                Mathf.Max(0, RemainingMove(selectedCharacter) - cost);
            StartCoroutine(AnimateMove(selectedCharacter, selectedTile, path));
        }

        // ------------------------------------------------------------------ //
        // Attack mode                                                         //
        // ------------------------------------------------------------------ //

        private void ToggleAttackMode()
        {
            if (currentMode == InputMode.Attack) { SetMode(InputMode.None); return; }

            int ap = RemainingActions(selectedCharacter);
            if (ap <= 0)
            {
                Debug.Log($"[TurnManager] '{selectedCharacter.characterName}' has no actions remaining.");
                return;
            }

            SetMode(InputMode.Attack);
            ShowAttackRange(selectedTile, GetAttackRange(selectedCharacter));
        }

        private void ShowAttackRange(MapTile origin, int range)
        {
            int ox = origin.GridX, oz = origin.GridZ;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist <= 0 || dist > range) continue;
                    MapTile tile = tiles[x, z];
                    tile.SetAttackHighlight(true);
                    highlightedAttackTiles.Add(tile);
                }
            }
        }

        private void ClearAttackHighlights()
        {
            foreach (MapTile t in highlightedAttackTiles)
                t.SetAttackHighlight(false);
            highlightedAttackTiles.Clear();
        }

        private void CommitAttack(MapTile targetTile)
        {
            SetMode(InputMode.None);
            NPC target = targetTile.OccupyingNpc;
            if (target == null) return;

            int damage = selectedCharacter.characterStats.TotalAttack;
            target.characterStats.healthPoints -= damage;

            Debug.Log($"[TurnManager] '{selectedCharacter.characterName}' attacks " +
                      $"'{target.characterName}' for {damage} damage.  " +
                      $"HP: {Mathf.Max(0, target.characterStats.healthPoints)}" +
                      $"/{target.characterStats.maxHealthPoints}");

            remainingActions[selectedCharacter] =
                Mathf.Max(0, RemainingActions(selectedCharacter) - 1);

            if (target.characterStats.healthPoints <= 0)
            {
                Debug.Log($"[TurnManager] '{target.characterName}' has been defeated!");
                targetTile.RemoveNpc();
            }

            CheckAndHandleTurnEnd(selectedCharacter);
        }

        /// <summary>Attack range in Manhattan-distance tiles based on class specialty.</summary>
        private int GetAttackRange(Character character)
        {
            if (character.characterClass == null) return 1;
            switch (character.characterClass.classSpecialty)
            {
                case CharacterClass.ClassSpecialty.MELEE:  return 1;
                case CharacterClass.ClassSpecialty.MAGIC:  return 2;
                case CharacterClass.ClassSpecialty.RANGED: return 3;
                default:                                   return 1;
            }
        }

        // ------------------------------------------------------------------ //
        // Turn management                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Checks whether the character has exhausted both MP and AP;
        /// automatically ends their turn if so.
        /// </summary>
        private void CheckAndHandleTurnEnd(Character character)
        {
            if (RemainingMove(character) <= 0 && RemainingActions(character) <= 0)
                EndCharacterTurn(character);
        }

        /// <summary>
        /// Marks a character as having finished their turn (called manually via E or
        /// automatically when resources are depleted).  Transitions to the enemy phase
        /// once every living party member has finished.
        /// </summary>
        private void EndCharacterTurn(Character character)
        {
            if (character != null && !finishedCharacters.Contains(character))
            {
                finishedCharacters.Add(character);
                remainingMovement[character] = 0;
                remainingActions[character]  = 0;
                Debug.Log($"[TurnManager] '{character.characterName}' has finished their turn.");
            }
            Deselect();
            CheckAllCharactersDone();
        }

        private void CheckAllCharactersDone()
        {
            var members = PartyManager.Instance?.Party?.members;
            if (members == null || members.Count == 0) { StartEnemyTurn(); return; }

            foreach (Character c in members)
                if (!finishedCharacters.Contains(c) && c.characterStats.healthPoints > 0)
                    return;

            StartEnemyTurn();
        }

        private void StartEnemyTurn()
        {
            CurrentPhase = TurnPhase.EnemyTurn;
            Deselect();
            Debug.Log("[TurnManager] All player characters have acted.  Enemy turn — AI not yet implemented.");
        }

        /// <summary>
        /// BFS: returns all tiles reachable within <paramref name="range"/> cardinal steps,
        /// excluding occupied tiles (characters may not pass through or land on them).
        /// </summary>
        private HashSet<MapTile> BfsReachable(MapTile origin, int range)
        {
            var dist  = new Dictionary<MapTile, int>();
            var queue = new Queue<MapTile>();
            dist[origin] = 0;
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                MapTile cur = queue.Dequeue();
                int     d   = dist[cur];
                if (d >= range) continue;

                foreach (MapTile nb in Neighbors(cur))
                {
                    if (dist.ContainsKey(nb)) continue;
                    if (nb.IsOccupied) continue;
                    dist[nb] = d + 1;
                    queue.Enqueue(nb);
                }
            }

            return new HashSet<MapTile>(dist.Keys);
        }

        /// <summary>
        /// BFS shortest path from <paramref name="origin"/> to <paramref name="dest"/>.
        /// Returns an ordered list that starts with origin and ends with dest.
        /// </summary>
        private List<MapTile> FindShortestPath(MapTile origin, MapTile dest)
        {
            var prev  = new Dictionary<MapTile, MapTile>();
            var queue = new Queue<MapTile>();
            prev[origin] = null;
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                MapTile cur = queue.Dequeue();
                if (cur == dest) break;

                foreach (MapTile nb in Neighbors(cur))
                {
                    if (prev.ContainsKey(nb)) continue;
                    if (nb.IsOccupied && nb != dest) continue;
                    prev[nb] = cur;
                    queue.Enqueue(nb);
                }
            }

            // Reconstruct path back from dest → origin, then reverse.
            var path = new List<MapTile>();
            MapTile node = dest;
            while (node != null)
            {
                path.Insert(0, node);
                MapTile parent;
                if (!prev.TryGetValue(node, out parent)) break;
                node = parent;
            }
            return path;
        }

        private List<MapTile> Neighbors(MapTile tile)
        {
            var list = new List<MapTile>(4);
            int x = tile.GridX;
            int z = tile.GridZ;
            if (x > 0)            list.Add(tiles[x - 1, z]);
            if (x < gridSize - 1) list.Add(tiles[x + 1, z]);
            if (z > 0)            list.Add(tiles[x, z - 1]);
            if (z < gridSize - 1) list.Add(tiles[x, z + 1]);
            return list;
        }

        // ------------------------------------------------------------------ //
        // Movement animation                                                  //
        // ------------------------------------------------------------------ //

        private IEnumerator AnimateMove(Character character, MapTile fromTile,
                                        List<MapTile> path)
        {
            isMoving = true;

            GameObject model = fromTile.CharacterObject;
            if (model == null)
            {
                Debug.LogWarning("[TurnManager] Character model not found during move animation.");
                isMoving = false;
                yield break;
            }

            MapTile currentTile = fromTile;

            for (int i = 1; i < path.Count; i++)
            {
                MapTile next     = path[i];
                Vector3 startPos = model.transform.position;
                float   halfH    = next.transform.lossyScale.y * 0.5f;
                Vector3 endPos   = next.transform.position + Vector3.up * (halfH + 0.05f);
                float   elapsed  = 0f;
                float   duration = step / MoveSpeed;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    model.transform.position = Vector3.Lerp(startPos, endPos,
                                                            elapsed / duration);
                    yield return null;
                }

                // Snap to exact position and transfer occupancy.
                model.transform.position = endPos;
                currentTile.ReleaseCharacter();
                next.AssignCharacter(character, model);
                currentTile = next;
            }

            selectedTile = currentTile;
            Debug.Log($"[TurnManager] '{character.characterName}' arrived at " +
                      $"({currentTile.GridX}, {currentTile.GridZ}).  " +
                      $"MP: {RemainingMove(character)}  AP: {RemainingActions(character)}");

            isMoving = false;

            if (selectedCharacter == character)
                CheckAndHandleTurnEnd(character);
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                             //
        // ------------------------------------------------------------------ //

        private int RemainingMove(Character character)
        {
            int mp;
            return remainingMovement.TryGetValue(character, out mp) ? mp : 0;
        }

        private int RemainingActions(Character character)
        {
            int ap;
            return remainingActions.TryGetValue(character, out ap) ? ap : 0;
        }
    }
}
