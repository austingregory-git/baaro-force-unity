using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BaaroForce.Animations;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Spells;
using BaaroForce.UI;
using System;
using UnityEngine.SceneManagement;
using BaaroForce.Passives;
using BaaroForce.GameController;
using BaaroForce.Relics;
using BaaroForce.Loot;

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
    ///   • W/M highlights reachable _tiles (blue, near-opaque) via BFS.
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

        private MapTile[,] _tiles;
        private int         _gridSize;
        private float       _step;
        private float       _originX;
        private float       _originZ;

        // ------------------------------------------------------------------ //
        // Turn state                                                          //
        // ------------------------------------------------------------------ //

        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Deployment;

        private Character _selectedCharacter;
        private MapTile   _selectedTile;

        /// <summary>Movement points remaining this turn, keyed by character.</summary>
        private readonly Dictionary<Character, int> _remainingMovement =
            new Dictionary<Character, int>();

        /// <summary>Action points remaining this turn, keyed by character.</summary>
        private readonly Dictionary<Character, int> _remainingActions =
            new Dictionary<Character, int>();

        /// <summary>Characters who have already used all their resources this turn.</summary>
        private readonly HashSet<Character> _finishedCharacters = new HashSet<Character>();

        /// <summary>
        /// Round counter, incremented once per <see cref="StartPlayerTurn"/> (a "round" is one
        /// full player-turn + enemy-turn cycle). Spell cooldowns are tracked against this rather
        /// than per-character turns, since all party members share one PlayerTurn phase.
        /// </summary>
        private int _roundNumber;

        /// <summary>
        /// Per-character map of Spell → the round number it becomes available again.
        /// int.MaxValue means "used, once-per-fight" — never available again this battle.
        /// Absent entries are always available. Persists across rounds (unlike MP/AP) —
        /// only StartPlayerTurn's _roundNumber increment can clear a cooldown.
        /// </summary>
        private readonly Dictionary<Character, Dictionary<Spell, int>> _spellAvailableAtRound =
            new Dictionary<Character, Dictionary<Spell, int>>();

        // ------------------------------------------------------------------ //
        // Input mode and highlights                                           //
        // ------------------------------------------------------------------ //

        private enum InputMode { None, Move, Attack, Spell }
        private InputMode _currentMode = InputMode.None;

        private readonly List<MapTile> _highlightedMoveTiles   = new List<MapTile>();
        private readonly List<MapTile> _highlightedAttackTiles = new List<MapTile>();
        private readonly List<MapTile> _highlightedZoneTiles   = new List<MapTile>();

        /// <summary>Move-highlight color for a reachable tile that also lies within an
        /// enemy's Zone of Control — same alpha as the normal blue so it reads as a
        /// single opaque tile color rather than a blend.</summary>
        private static readonly Color ZoneMoveHighlightColor = new Color(1f, 0.65f, 0.1f, 0.92f);

        //_spellTargetTiles
        private readonly List<MapTile> _spellTargetTiles = new List<MapTile>();
        private readonly List<MapTile> _spellPreviewTiles = new List<MapTile>();

        private Spell         _selectedSpell;
        private ActionPanelUI  _actionPanel;
        private SpellPanelUI   _spellPanel;
        private WarningToastUI _warningToast;
        private FightResultUI  _fightResultUI;
        private bool           _isMoving;
        private bool           _fightEnded;
        private MapTile _hoveredTile;
        private Npc     _hoveredTarget;


        private const float MoveSpeed = 5f;   // world-units per second

        // ------------------------------------------------------------------ //
        // HUD events — consumed by CharacterHudController.                    //
        // TurnManager stays UI-agnostic; it just reports what happened.       //
        // ------------------------------------------------------------------ //

        /// <summary>Fired when a party member is selected — the "current turn" unit.</summary>
        public event Action<Character> OnCharacterSelected;
        /// <summary>Fired when the current selection is cleared.</summary>
        public event Action OnCharacterDeselected;
        /// <summary>Fired when the mouse hovers an enemy Npc (a potential attack target),
        /// and again with updated stats whenever that target takes damage.</summary>
        public event Action<Npc> OnTargetHighlighted;
        /// <summary>Fired when the hovered/attacked target is no longer relevant (mouse left, or it died).</summary>
        public event Action OnTargetCleared;

        // ------------------------------------------------------------------ //
        // Initialisation                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Called by MapGenerator after building the grid.</summary>
        public void Initialize(MapTile[,] grid, int size, float tileStep,
                               float originWorldX, float originWorldZ)
        {
            _tiles    = grid;
            _gridSize = size;
            _step     = tileStep;
            _originX  = originWorldX;
            _originZ  = originWorldZ;

            EnsureMapUI();

            _actionPanel = gameObject.AddComponent<ActionPanelUI>();
            _actionPanel.OnMoveClicked   = ToggleMoveMode;
            _actionPanel.OnAttackClicked = ToggleAttackMode;
            _actionPanel.OnSpellsClicked = ShowSpellPanel;
            _actionPanel.OnItemsClicked  = () => Debug.Log("[TurnManager] Items — not yet implemented.");
            _actionPanel.OnWaitClicked   = () => EndCharacterTurn(_selectedCharacter);

            _spellPanel = gameObject.AddComponent<SpellPanelUI>();
            _spellPanel.OnSpellSelected = ActivateSpell;
            _spellPanel.OnBackClicked   = ShowActionPanel;
            _spellPanel.GetCooldownRemaining = GetCooldownRemaining;

            _warningToast = gameObject.AddComponent<WarningToastUI>();

            _fightResultUI = gameObject.AddComponent<FightResultUI>();
            _fightResultUI.OnReturnToMainMenu = () =>
            {
                PartyManager.Instance.ResetForNewRun();
                SceneManager.LoadScene("MainMenu");
            };
            _fightResultUI.OnMoveOn = () =>
                Debug.Log("[TurnManager] Move on — not yet implemented.");
            _fightResultUI.OnLootClaimed = ClaimLoot;
        }

        private static void ClaimLoot(LootEntry entry)
        {
            if (entry.Type == LootType.Gold)
                PartyManager.Instance.Party.AddGold(entry.Amount);
            // Item loot has no inventory to add to yet — nothing to do until one exists.
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
            _roundNumber++;
            _remainingMovement.Clear();
            _remainingActions.Clear();
            _finishedCharacters.Clear();

            var members = PartyManager.Instance?.Party?.Members;
            if (members != null)
                foreach (Character c in members)
                {
                    _remainingMovement[c] = c.CharacterStats.Movement;
                    _remainingActions[c]  = c.CharacterStats.MaxActionPoints;
                }

            var relics = PartyManager.Instance?.Relics;
            CheckAndHandlePlayerTurnStart(members, relics);

            Debug.Log("[TurnManager] Player turn started.");
        }

        private void CheckAndHandlePlayerTurnStart(List<Character> members, List<Relic> relics)
        {
            // Implement logic to handle the start of the player's turn.
            // This could include checking if all characters have finished their turns,
            // applying relic effects, or other start-of-turn mechanics.
            Debug.Log("[TurnManager] Checking and handling start of player turn.");

            // _roundNumber was just incremented in StartPlayerTurn, so 1 means this is the
            // very first player turn of the battle — fire once-per-battle passives here.
            if (_roundNumber == 1)
                CheckAndHandleStartOfCombatPassives(members);

            CheckAndHandlePlayerTurnStartPassives(members);

            CheckAndHandleTurnStartRelics(relics);
        }

        private void CheckAndHandleTurnStartRelics(List<Relic> relics)
        {
            return;
            //throw new NotImplementedException();
        }

        private void CheckAndHandleStartOfCombatPassives(List<Character> members)
        {
            if (members == null) return;
            foreach (Character c in members)
            {
                foreach (var passive in c.CharacterPassiveAbilities)
                {
                    if (passive != null && passive.AbilityType == PassiveAbility.PassiveAbilityType.StartOfCombat)
                    {
                        Debug.Log($"[TurnManager] Executing start-of-combat passive '{passive.Name}' for character '{c.CharacterName}'.");
                        passive.Execute(new PassiveOnTurnContext(character: c, characterLevel: c.Level, characterTile: c.CharacterCurrentTile, allTiles: _tiles, gridSize: _gridSize));
                    }
                }
            }
        }

        private void CheckAndHandlePlayerTurnStartPassives(List<Character> members)
        {
            foreach (Character c in members)
            {
                foreach (var passive in c.CharacterPassiveAbilities)
                {
                    Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{c.CharacterName}' at start of turn.");
                    if (passive != null && passive.AbilityType == PassiveAbility.PassiveAbilityType.StartOfTurn)
                    {
                        Debug.Log($"[TurnManager] Executing passive ability '{passive.Name}' for character '{c.CharacterName}'.");
                        passive.Execute(new PassiveOnTurnContext(character: c, characterLevel: c.Level, characterTile: c.CharacterCurrentTile, allTiles: _tiles, gridSize: _gridSize));
                    }
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Unity update                                                        //
        // ------------------------------------------------------------------ //

        private void Update()
        {
            if (_fightEnded) return;
            if (CurrentPhase != TurnPhase.PlayerTurn) return;
            if (_isMoving) return;

            UpdateHoveredTile();

            if (_currentMode == InputMode.Spell &&
                _selectedSpell != null &&
                _selectedSpell.TargetType == SpellTargetType.Area)
            {
                UpdateSpellPreview();
            }

            HandleClick();
            HandleKeys();
        }

        // ------------------------------------------------------------------ //
        // Hovered tile handling                                               //
        // ------------------------------------------------------------------ //

        private void UpdateHoveredTile()
        {
            if (!TryGetTileUnderMouse(out MapTile tile))
            {
                _hoveredTile = null;
                UpdateHoveredTarget(null);
                return;
            }

            _hoveredTile = tile;
            UpdateHoveredTarget(tile.OccupyingNpc);
        }

        /// <summary>Raises OnTargetHighlighted/OnTargetCleared only when the hovered Npc actually changes.</summary>
        private void UpdateHoveredTarget(Npc npc)
        {
            if (npc == _hoveredTarget) return;

            _hoveredTarget = npc;
            if (npc != null) OnTargetHighlighted?.Invoke(npc);
            else              OnTargetCleared?.Invoke();
        }

        private bool TryGetTileUnderMouse(out MapTile tile)
        {
            tile = null;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

            if (!gridPlane.Raycast(ray, out float enter))
                return false;

            Vector3 hit = ray.GetPoint(enter);

            int gridX = Mathf.RoundToInt((hit.x - _originX) / _step);
            int gridZ = Mathf.RoundToInt((hit.z - _originZ) / _step);

            if (gridX < 0 || gridX >= _gridSize ||
                gridZ < 0 || gridZ >= _gridSize)
                return false;

            tile = _tiles[gridX, gridZ];
            return true;
        }

        // ------------------------------------------------------------------ //
        // Click handling                                                      //
        // ------------------------------------------------------------------ //

        private void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            // Prevent grid clicks when the pointer is over a UI element
            // (e.g. clicking a spell button must not also deselect the character).
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            if (!TryGetClickedTile(out MapTile clicked)) return;

            switch (_currentMode)
            {
                case InputMode.Move:
                    if (_highlightedMoveTiles.Contains(clicked))
                        CommitMove(clicked);
                    else
                        SetMode(InputMode.None);
                    break;

                case InputMode.Attack:
                    if (_highlightedAttackTiles.Contains(clicked) && clicked.OccupyingNpc != null)
                        CommitAttack(clicked);
                    else
                        SetMode(InputMode.None);
                    break;

                case InputMode.Spell:
                    if (_spellTargetTiles.Contains(clicked))
                        CommitSpell(clicked);
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
            int     gridX = Mathf.RoundToInt((hit.x - _originX) / _step);
            int     gridZ = Mathf.RoundToInt((hit.z - _originZ) / _step);

            if (gridX < 0 || gridX >= _gridSize || gridZ < 0 || gridZ >= _gridSize) return false;
            tile = _tiles[gridX, gridZ];
            return true;
        }

        // ------------------------------------------------------------------ //
        // Key handling                                                        //
        // ------------------------------------------------------------------ //

        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetMode(InputMode.None);
                ShowActionPanel();
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (_selectedCharacter != null) EndCharacterTurn(_selectedCharacter);
                return;
            }

            if (_selectedCharacter == null) return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.M))
                ToggleMoveMode();
            else if (Input.GetKeyDown(KeyCode.A))
                ToggleAttackMode();
            else if (Input.GetKeyDown(KeyCode.S))
                ToggleSpellMode();
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.I))
                Debug.Log("[TurnManager] Items — not yet implemented.");
            else
                HandleSpellHotkeys();
        }

        /// <summary>Number-row hotkeys 1–9, bound to the selected character's spells in
        /// list order (e.g. their 2nd spell activates on '2') — same entry point as
        /// clicking the row in SpellPanelUI, so affordability/cooldown checks and the
        /// targeting-mode toggle behave identically either way.</summary>
        private static readonly KeyCode[] SpellHotkeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        };

        private void HandleSpellHotkeys()
        {
            List<Spell> spells = _selectedCharacter.CharacterSpells;
            if (spells == null) return;

            for (int i = 0; i < SpellHotkeys.Length && i < spells.Count; i++)
            {
                if (Input.GetKeyDown(SpellHotkeys[i]))
                {
                    ActivateSpell(spells[i]);
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ //
        // Selection                                                           //
        // ------------------------------------------------------------------ //

        private void SelectCharacter(Character character, MapTile tile)
        {
            if (_finishedCharacters.Contains(character))
            {
                Debug.Log($"[TurnManager] '{character.CharacterName}' has already acted this turn.");
                return;
            }
            SetMode(InputMode.None);
            _selectedCharacter = character;
            _selectedTile      = tile;
            Debug.Log($"[TurnManager] Selected '{character.CharacterName}'  " +
                      $"MP: {RemainingMove(character)}  AP: {RemainingActions(character)}");
            ShowActionPanel();
            OnCharacterSelected?.Invoke(character);
        }

        private void Deselect()
        {
            SetMode(InputMode.None);
            _selectedCharacter = null;
            _selectedTile      = null;
            _actionPanel?.Hide();
            _spellPanel?.Hide();
            OnCharacterDeselected?.Invoke();
        }

        // ------------------------------------------------------------------ //
        // Mode management                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>Switches input mode, clearing all highlights from the previous mode.</summary>
        private void SetMode(InputMode mode)
        {
            ClearMoveHighlights();
            ClearZoneOfControlHighlights();
            ClearAttackHighlights();
            ClearSpellHighlights();
            ClearPreviewTiles();

            if (mode != InputMode.Spell)
                _selectedSpell = null;

            _currentMode = mode;
        }

        // ------------------------------------------------------------------ //
        // Move mode                                                           //
        // ------------------------------------------------------------------ //

        private void ToggleMoveMode()
        {
            if (_currentMode == InputMode.Move) { SetMode(InputMode.None); return; }

            if (_selectedCharacter.IsRooted)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' is rooted and cannot move.");
                _warningToast?.Show($"'{_selectedCharacter.CharacterName}' is rooted and cannot move.");
                return;
            }

            int mp = RemainingMove(_selectedCharacter);
            if (mp <= 0)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' has no movement remaining.");
                return;
            }

            SetMode(InputMode.Move);
            ShowMovableRange(_selectedTile, mp);
        }

        private void ShowMovableRange(MapTile origin, int range)
        {
            HashSet<MapTile> zoneTiles = GetEnemyZoneOfControlTiles(_selectedCharacter);
            HashSet<MapTile> reachable = BfsReachable(origin, range, _selectedCharacter);

            foreach (MapTile tile in reachable)
            {
                if (tile == origin) continue;
                bool inZone = zoneTiles.Contains(tile);
                tile.SetMoveHighlight(true, inZone ? ZoneMoveHighlightColor : (Color?)null);
                _highlightedMoveTiles.Add(tile);
            }

            // Tiles inside an enemy's zone that the mover can't currently reach get a
            // separate, lower-alpha warning tint — there's no move-highlight quad there
            // to recolor instead, so this avoids ever stacking two overlays on one tile.
            foreach (MapTile tile in zoneTiles)
            {
                if (tile == origin || reachable.Contains(tile)) continue;
                tile.SetZoneOfControlHighlight(true);
                _highlightedZoneTiles.Add(tile);
            }
        }

        /// <summary>Every tile within an enemy's Zone of Control (the 8 tiles surrounding
        /// it), relative to <paramref name="mover"/>.</summary>
        private HashSet<MapTile> GetEnemyZoneOfControlTiles(Character mover)
        {
            var zone = new HashSet<MapTile>();
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                {
                    MapTile t = _tiles[x, z];
                    Character occupant = t.OccupyingUnit;
                    if (occupant == null || !IsEnemyOf(mover, occupant)) continue;

                    foreach (MapTile zoneTile in SpellAreaUtils.GetCircleAroundTiles(t, 1, _tiles, _gridSize))
                        zone.Add(zoneTile);
                }
            return zone;
        }

        private void ClearMoveHighlights()
        {
            foreach (MapTile t in _highlightedMoveTiles)
                t.SetMoveHighlight(false);
            _highlightedMoveTiles.Clear();
        }

        private void ClearZoneOfControlHighlights()
        {
            foreach (MapTile t in _highlightedZoneTiles)
                t.SetZoneOfControlHighlight(false);
            _highlightedZoneTiles.Clear();
        }

        private void CommitMove(MapTile destination)
        {
            SetMode(InputMode.None);
            List<MapTile> path = FindShortestPath(_selectedTile, destination, _selectedCharacter);
            int manaCost = PathCost(path, _selectedCharacter);
            _remainingMovement[_selectedCharacter] =
                Mathf.Max(0, RemainingMove(_selectedCharacter) - manaCost);
            OnCharacterSelected?.Invoke(_selectedCharacter);
            StartCoroutine(AnimateMove(_selectedCharacter, _selectedTile, path));
        }

        // ------------------------------------------------------------------ //
        // Attack mode                                                         //
        // ------------------------------------------------------------------ //

        private void ToggleAttackMode()
        {
            if (_currentMode == InputMode.Attack) { SetMode(InputMode.None); return; }

            int ap = RemainingActions(_selectedCharacter);
            if (ap <= 0)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' has no actions remaining.");
                return;
            }

            SetMode(InputMode.Attack);
            ShowAttackRange(_selectedTile, GetAttackRange(_selectedCharacter));
        }

        private void ShowAttackRange(MapTile origin, int range)
        {
            int ox = origin.GridX, oz = origin.GridZ;
            for (int x = 0; x < _gridSize; x++)
            {
                for (int z = 0; z < _gridSize; z++)
                {
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist <= 0 || dist > range) continue;
                    MapTile tile = _tiles[x, z];
                    tile.SetAttackHighlight(true);
                    _highlightedAttackTiles.Add(tile);
                }
            }
        }

        private void ClearAttackHighlights()
        {
            foreach (MapTile t in _highlightedAttackTiles)
                t.SetAttackHighlight(false);
            _highlightedAttackTiles.Clear();
        }

        private void CommitAttack(MapTile targetTile)
        {
            SetMode(InputMode.None);
            Npc target = targetTile.OccupyingNpc;
            if (target == null) return;

            FaceTowardTile(_selectedCharacter, targetTile);
            ResolveBasicAttack(_selectedCharacter, target, targetTile);

            _remainingActions[_selectedCharacter] =
                Mathf.Max(0, RemainingActions(_selectedCharacter) - 1);

            if (target.CharacterStats.HealthPoints <= 0)
            {
                OnTargetCleared?.Invoke();
                _hoveredTarget = null;
            }
            else
            {
                // Push the post-damage stats to the right-side HUD panel.
                OnTargetHighlighted?.Invoke(target);
            }

            CheckFightOutcome();
            if (_fightEnded) return;

            CheckAndHandleTurnEnd(_selectedCharacter);
            if (_selectedCharacter != null)
            {
                ShowActionPanel();
                // Push updated AP/HP/mana to the left-side HUD panel.
                OnCharacterSelected?.Invoke(_selectedCharacter);
            }
        }

        /// <summary>Attack range in Manhattan-distance _tiles based on class specialty.</summary>
        private int GetAttackRange(Character character)
        {
            if (character.CharacterClass == null) return 1;
            switch (character.CharacterClass.Specialty)
            {
                case CharacterClass.ClassSpecialty.Melee:  return 1;
                case CharacterClass.ClassSpecialty.Magic:  return 2;
                case CharacterClass.ClassSpecialty.Ranged: return 3;
                default:                                   return 1;
            }
        }

        // ------------------------------------------------------------------ //
        // Spell mode                                                          //
        // ------------------------------------------------------------------ //

        private void ToggleSpellMode()
        {
            // Cancel active tile-targeting and return to the spell panel.
            if (_currentMode == InputMode.Spell)
            {
                SetMode(InputMode.None);
                if (_selectedCharacter != null) ShowSpellPanel();
                return;
            }

            // Toggle the spell panel off if it is already showing.
            if (_spellPanel != null && _spellPanel.IsVisible)
            {
                ShowActionPanel();
                return;
            }

            if (_selectedCharacter == null) return;
            ShowSpellPanel();
        }

        private void ShowSpellRange(MapTile origin, Spell spell)
        {
            if (spell.TargetType == SpellTargetType.Self)
            {
                // No tile is aimed, but the spell still affects a fixed set of tiles around
                // the caster (just the caster's own tile for a plain self-buff like Grit, or
                // a wider CircleAround area for something like Rally) — highlight exactly
                // that area so the player can see who/what will be affected before confirming.
                List<MapTile> affectedTiles = SpellAreaUtils.GetAreaTiles(spell, origin, origin, _tiles, _gridSize);
                Color selfColor = GetSpellHighlightColor(spell);
                foreach (MapTile tile in affectedTiles)
                {
                    tile.SetSpellHighlight(true, selfColor);
                    _spellTargetTiles.Add(tile);
                }
                return;
            }

            Color color = GetSpellHighlightColor(spell);
            int ox = origin.GridX, oz = origin.GridZ;

            for (int x = 0; x < _gridSize; x++)
            {
                for (int z = 0; z < _gridSize; z++)
                {
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist <= 0 || dist > spell.Range) continue;

                    MapTile tile = _tiles[x, z];

                    tile.SetSpellHighlight(true, color);
                    _spellTargetTiles.Add(tile);
                }
            }
        }

        private void UpdateSpellPreview()
        {
            ClearPreviewTiles();

            if (_hoveredTile == null)
                return;

            int distance =
                Mathf.Abs(_hoveredTile.GridX - _selectedTile.GridX) +
                Mathf.Abs(_hoveredTile.GridZ - _selectedTile.GridZ);

            if (distance == 0 || distance > _selectedSpell.Range)
                return;

            // Dispatches on the spell's own AreaType (HorizontalLine, Cone, ...) so the preview
            // always matches the shape Execute will actually resolve against.
            List<MapTile> areaTiles =
                SpellAreaUtils.GetAreaTiles(
                    _selectedSpell,
                    _selectedTile,
                    _hoveredTile,
                    _tiles,
                    _gridSize);

            // Higher alpha than the selectable-range highlight (0.8 vs 0.55) so the exact
            // tiles about to be hit stand out from the wider "you could aim here" range.
            Color previewColor = GetSpellHighlightColor(_selectedSpell, alpha: 0.8f);
            foreach (var tile in areaTiles)
            {
                tile.SetSpellHighlight(true, previewColor);
                _spellPreviewTiles.Add(tile);
            }
        }

        private void ClearPreviewTiles()
        {
            foreach (var tile in _spellPreviewTiles)
                tile.SetSpellHighlight(false, Color.clear);

            _spellPreviewTiles.Clear();
        }

        private void ClearSpellHighlights()
        {
            foreach (MapTile t in _spellTargetTiles)
                t.SetSpellHighlight(false, Color.clear);
            _spellTargetTiles.Clear();
        }

        private void CommitSpell(MapTile targetTile)
        {
            if (_selectedSpell == null) return;

            Spell spell = _selectedSpell;

            // Reject tiles that don't hold a valid target for this spell's TargetType
            // (e.g. an Enemy-targeted spell aimed at an empty or ally-occupied tile).
            // Bail out *before* touching AP/mana/cooldown and stay in targeting mode
            // so the player can simply pick a real target instead of losing the turn.
            if (!IsValidSpellTarget(spell, targetTile))
            {
                Debug.Log($"[TurnManager] '{spell.Name}' has no valid target on that tile.");
                _warningToast?.Show(GetNoTargetMessage(spell));
                return;
            }

            SetMode(InputMode.None);   // clears highlights and nulls _selectedSpell

            if (spell.ManaCost > 0 && _selectedCharacter.CharacterStats.Mana < spell.ManaCost)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' " +
                           "does not have enough mana to cast this spell.");
                _warningToast?.Show($"Not enough mana to cast '{spell.Name}'.");
                return;
            }

            var context = new SpellContext(
                caster:      _selectedCharacter,
                casterLevel: _selectedCharacter.Level,
                casterTile:  _selectedTile,
                targetTile:  targetTile,
                allTiles:    _tiles,
                gridSize:    _gridSize);

            // If the spell physically repositions the caster first (e.g. Charge),
            // animate the movement and resolve the effect at the end of that coroutine.
            MapTile landingTile = spell.GetCasterLandingTile(context);
            if (landingTile != null)
            {
                StartCoroutine(SpellWithMovement(_selectedCharacter, _selectedTile,
                                                 landingTile, spell, context));
                return;
            }

            FaceTowardTile(_selectedCharacter, targetTile);
            bool resolved = spell.Execute(context);

            // Spend the spell's own action-point cost — the attempt was made.
            // Free spells (ActionPointCost == 0, e.g. Evasion, Throwing Knife) spend nothing.
            _remainingActions[_selectedCharacter] =
                Mathf.Max(0, RemainingActions(_selectedCharacter) - spell.ActionPointCost);

            // Only deduct mana and start the cooldown on a successful cast.
            if (resolved)
            {
                _selectedCharacter.CharacterStats.Mana =
                    Mathf.Max(0, _selectedCharacter.CharacterStats.Mana - spell.ManaCost);
                StartSpellCooldown(_selectedCharacter, spell);
                _selectedCharacter.BreakInvisibility();
            }

            CheckFightOutcome();
            if (_fightEnded) return;

            CheckAndHandleTurnEnd(_selectedCharacter);
            if (_selectedCharacter != null)
            {
                ShowActionPanel();
                OnCharacterSelected?.Invoke(_selectedCharacter);
            }
        }

        /// <summary>
        /// Animates the caster moving to <paramref name="landingTile"/> along the grid,
        /// then resolves the spell's Execute.  Input is blocked while moving.
        /// Movement does NOT consume movement points — it is part of the spell effect.
        /// </summary>
        private IEnumerator SpellWithMovement(Character caster, MapTile fromTile,
                                               MapTile landingTile, Spell spell,
                                               SpellContext context)
        {
            _isMoving = true;

            List<MapTile> path = FindShortestPath(fromTile, landingTile, caster);
            yield return StartCoroutine(MoveUnitAlongPath(caster, fromTile, path, finalTile =>
            {
                _selectedTile = finalTile;
            }));

            _isMoving = false;

            // Caster is now in position — resolve the spell's damage / effect.
            FaceTowardTile(caster, context.TargetTile);
            bool resolved = spell.Execute(context);

            _remainingActions[caster] =
                Mathf.Max(0, RemainingActions(caster) - spell.ActionPointCost);

            if (resolved)
            {
                caster.CharacterStats.Mana =
                    Mathf.Max(0, caster.CharacterStats.Mana - spell.ManaCost);
                StartSpellCooldown(caster, spell);
                caster.BreakInvisibility();
            }

            CheckFightOutcome();
            if (_fightEnded) yield break;

            CheckAndHandleTurnEnd(caster);
            if (_selectedCharacter != null)
                ShowActionPanel();
        }

        /// <summary>Highlight tint for <paramref name="spell"/>'s selectable-range/self area.
        /// Prefers the spell's own elemental/physical <see cref="Spell.Type"/> — reusing
        /// <see cref="CombatTextColors.ForDamageType"/> so a spell's highlight always matches
        /// the colour its damage numbers use (Physical = orange, Magical = light purple, ...).
        /// Falls back to a TargetType-based colour for spells with no established type
        /// (buffs like Grit/Meditate, whose effect isn't a damage type at all).</summary>
        /// <summary>Highlight tint for <paramref name="spell"/>, at <paramref name="alpha"/>.
        /// Prefers <see cref="Spell.GetHighlightType"/> (the caster-aware hook, so a
        /// randomised-type spell like Magic Dart can highlight its resolved type) — reusing
        /// <see cref="CombatTextColors.ForDamageType"/> so a spell's highlight always matches
        /// the colour its damage numbers use (Physical = orange, Magical = light purple,
        /// Buff = green, ...). Falls back to a TargetType-based colour for spells with no
        /// established type at all.</summary>
        private Color GetSpellHighlightColor(Spell spell, float alpha = 0.55f)
        {
            SpellType? type = spell.GetHighlightType(_selectedCharacter);
            if (type.HasValue)
            {
                Color c = CombatTextColors.ForDamageType(type.Value);
                return new Color(c.r, c.g, c.b, alpha);
            }

            switch (spell.TargetType)
            {
                case SpellTargetType.Enemy: return new Color(0.9f, 0.15f, 0.10f, alpha); // red
                case SpellTargetType.Ally:  return new Color(0.1f, 0.80f, 0.20f, alpha); // green
                case SpellTargetType.Self:  return new Color(0.1f, 0.80f, 0.20f, alpha); // green, same as Ally
                case SpellTargetType.Both:  return new Color(0.6f, 0.10f, 0.85f, alpha); // purple
                default:                   return new Color(0.9f, 0.15f, 0.10f, alpha);
            }
        }

        /// <summary>
        /// True if <paramref name="tile"/> holds an occupant this spell's TargetType
        /// can legally affect. Area/Self spells have no occupant requirement — Area
        /// spells resolve against a shape of tiles (some of which may be empty) and
        /// Self spells never reach this check (no tile is targeted).
        /// </summary>
        private bool IsValidSpellTarget(Spell spell, MapTile tile)
        {
            if (tile == null) return false;

            switch (spell.TargetType)
            {
                case SpellTargetType.Enemy: return tile.OccupyingNpc != null;
                case SpellTargetType.Ally:  return tile.OccupyingCharacter != null;
                case SpellTargetType.Both:  return tile.IsOccupied;
                default:                    return true;
            }
        }

        private string GetNoTargetMessage(Spell spell)
        {
            switch (spell.TargetType)
            {
                case SpellTargetType.Enemy: return $"'{spell.Name}' needs an enemy on the targeted tile.";
                case SpellTargetType.Ally:  return $"'{spell.Name}' needs an ally on the targeted tile.";
                case SpellTargetType.Both:  return $"'{spell.Name}' needs a unit on the targeted tile.";
                default:                    return $"'{spell.Name}' has no valid target there.";
            }
        }

        private Spell GetFirstUsableSpell(Character character)
        {
            if (character.CharacterSpells == null) return null;
            foreach (Spell spell in character.CharacterSpells)
                if (character.CharacterStats.Mana >= spell.ManaCost)
                    return spell;
            return null;
        }

        /// <summary>
        /// Activates a spell for the currently selected character and enters targeting mode.
        /// Self-targeting spells have nothing to aim, but still highlight the fixed set of
        /// tiles their effect will actually reach (see <see cref="ShowSpellRange"/>) — the
        /// player confirms by clicking any highlighted tile, same as any other spell.
        /// Called by both the S key shortcut and the spell panel buttons.
        /// </summary>
        public void ActivateSpell(Spell spell)
        {
            if (_selectedCharacter == null) return;

            // Toggle off when the same spell is already queued.
            if (_currentMode == InputMode.Spell && _selectedSpell == spell)
            {
                SetMode(InputMode.None);
                return;
            }

            if (_selectedCharacter.IsSilenced)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' is silenced and cannot cast spells.");
                _warningToast?.Show($"'{_selectedCharacter.CharacterName}' is silenced and cannot cast spells.");
                return;
            }

            int ap = RemainingActions(_selectedCharacter);
            if (ap < spell.ActionPointCost)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' has no actions remaining.");
                _warningToast?.Show($"'{_selectedCharacter.CharacterName}' has no action points left.");
                return;
            }

            if (spell.ManaCost > 0 && _selectedCharacter.CharacterStats.Mana < spell.ManaCost)
            {
                Debug.Log($"[TurnManager] Not enough mana to cast '{spell.Name}'.");
                _warningToast?.Show($"Not enough mana to cast '{spell.Name}'.");
                return;
            }

            if (!IsSpellAvailable(_selectedCharacter, spell))
            {
                int cooldownRemaining = GetCooldownRemaining(_selectedCharacter, spell);
                string cooldownMessage = cooldownRemaining == int.MaxValue
                    ? $"'{spell.Name}' has already been used this fight."
                    : $"'{spell.Name}' is on cooldown ({cooldownRemaining} round(s) left).";

                Debug.Log($"[TurnManager] {cooldownMessage}");
                _warningToast?.Show(cooldownMessage);
                return;
            }

            // Enter targeting mode — hide panels and show range/area highlights.
            // Self-targeting spells with no aim still get a highlight (see ShowSpellRange);
            // the click that confirms them lands on CommitSpell exactly like any other spell.
            _actionPanel?.Hide();
            _spellPanel?.Hide();
            SetMode(InputMode.Spell);
            _selectedSpell = spell;
            ShowSpellRange(_selectedTile, spell);
            Debug.Log($"[TurnManager] Spell mode: '{spell.Name}'  " +
                      $"(Range: {spell.Range}, manaCost: {spell.ManaCost} mana)");

        }

        // ------------------------------------------------------------------ //
        // Panel management                                                    //
        // ------------------------------------------------------------------ //

        /// <summary>Returns to the character action menu.  Clears any active input mode.</summary>
        private void ShowActionPanel()
        {
            SetMode(InputMode.None);
            _spellPanel?.Hide();
            if (_selectedCharacter != null)
                _actionPanel?.Show(_selectedCharacter);
        }

        /// <summary>Opens the spell selection panel.  Clears any active input mode.</summary>
        private void ShowSpellPanel()
        {
            SetMode(InputMode.None);
            _actionPanel?.Hide();
            if (_selectedCharacter != null)
                _spellPanel?.Show(_selectedCharacter);
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

        private void CheckAndHandlePlayerTurnEnd(List<Character> members, List<Relic> relics)
        {
            // Implement logic to handle the end of the player's turn.
            // This could include checking if all characters have finished their turns,
            // applying relic effects, or other end-of-turn mechanics.
            Debug.Log("[TurnManager] Checking and handling end of player turn.");

            foreach (Character c in members)
            {
                c.TickStatusEffects();
            }

            CheckAndHandlePlayerTurnEndPassives(members);

            CheckAndHandleTurnEndRelics(relics);
        }

        private void CheckAndHandleTurnEndRelics(List<Relic> relics)
        {
            return;
            //throw new NotImplementedException();
        }

        private void CheckAndHandlePlayerTurnEndPassives(List<Character> members)
        {
            foreach (Character c in members)
            {
                foreach (var passive in c.CharacterPassiveAbilities)
                {
                    Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{c.CharacterName}' at end of turn.");
                    if (passive != null && passive.AbilityType == PassiveAbility.PassiveAbilityType.EndOfTurn)
                    {
                        Debug.Log($"[TurnManager] Executing passive ability '{passive.Name}' for character '{c.CharacterName}'.");
                        passive.Execute(new PassiveOnTurnContext(character: c, characterLevel: c.Level, characterTile: c.CharacterCurrentTile, allTiles: _tiles, gridSize: _gridSize));
                    }
                }
            }
        }

        /// <summary>
        /// Marks a character as having finished their turn (called manually via E or
        /// automatically when resources are depleted).  Transitions to the enemy phase
        /// once every living party member has finished.
        /// </summary>
        private void EndCharacterTurn(Character character)
        {
            if (character != null && !_finishedCharacters.Contains(character))
            {
                _finishedCharacters.Add(character);
                _remainingMovement[character] = 0;
                _remainingActions[character]  = 0;
                Debug.Log($"[TurnManager] '{character.CharacterName}' has finished their turn.");
            }
            Deselect();
            CheckAllCharactersDone();
        }

        private void CheckAllCharactersDone()
        {
            CheckFightOutcome();
            if (_fightEnded) return;

            var members = PartyManager.Instance?.Party?.Members;
            if (members == null || members.Count == 0) { StartEnemyTurn(); return; }

            foreach (Character c in members)
                if (!_finishedCharacters.Contains(c) && c.CharacterStats.HealthPoints > 0)
                    return;

            var relics = PartyManager.Instance?.Relics;
            CheckAndHandlePlayerTurnEnd(members, relics);

            CheckFightOutcome();
            if (_fightEnded) return;

            StartEnemyTurn();
        }

        /// <summary>
        /// Scans the party and the grid for remaining living allies/enemies. If either side
        /// has been wiped, ends the fight: hides all HUD panels, blocks further input, and
        /// shows the appropriate Game Over / Fight Won screen. No-ops once already ended,
        /// and no-ops while both sides still have a survivor.
        /// </summary>
        private void CheckFightOutcome()
        {
            if (_fightEnded) return;

            var members = PartyManager.Instance?.Party?.Members;
            bool anyAllyAlive = false;
            if (members != null)
                foreach (Character c in members)
                    if (c.CharacterStats.HealthPoints > 0) { anyAllyAlive = true; break; }

            bool anyEnemyAlive = false;
            for (int x = 0; x < _gridSize && !anyEnemyAlive; x++)
                for (int z = 0; z < _gridSize && !anyEnemyAlive; z++)
                {
                    MapTile tile = _tiles[x, z];
                    if (tile == null) continue;
                    Npc npc = tile.OccupyingNpc;
                    if (npc != null && npc.CharacterStats.HealthPoints > 0)
                        anyEnemyAlive = true;
                }

            if (anyAllyAlive && anyEnemyAlive) return;

            _fightEnded = true;
            Deselect();

            if (!anyAllyAlive)
            {
                Debug.Log("[TurnManager] All allies defeated. Game over.");
                _fightResultUI?.ShowGameOver();
            }
            else
            {
                Debug.Log("[TurnManager] All enemies defeated. Fight won!");
                int depth = PartyManager.Instance.Depth;
                List<LootEntry> loot = FightRewardGenerator.Generate(depth);
                PartyManager.Instance.AdvanceDepth();
                _fightResultUI?.ShowFightWon(loot);
            }
        }

        private void StartEnemyTurn()
        {
            CurrentPhase = TurnPhase.EnemyTurn;
            Deselect();
            Debug.Log("[TurnManager] Enemy turn started.");
            StartCoroutine(RunEnemyTurns());
        }

        /// <summary>
        /// Runs every living Npc's AI turn sequentially, then returns control
        /// to the player by calling StartPlayerTurn.
        /// </summary>
        private IEnumerator RunEnemyTurns()
        {
            // Snapshot Npc list before acting — Npcs that die mid-turn are skipped.
            var npcTiles = new List<(Npc npc, MapTile tile)>();
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                {
                    MapTile t = _tiles[x, z];
                    if (t.OccupyingNpc != null)
                        npcTiles.Add((t.OccupyingNpc, t));
                }

            if (npcTiles.Count == 0)
            {
                Debug.Log("[TurnManager] No Npcs remain.  Starting player turn.");
                StartPlayerTurn();
                yield break;
            }

            foreach (var (npc, _) in npcTiles)
            {
                if (npc.CharacterStats.HealthPoints <= 0) continue;
                if (npc.AI == null) continue;

                // Re-locate the Npc's current tile (it may have moved earlier this round).
                MapTile currentTile = FindNpcTile(npc);
                if (currentTile == null) continue;

                var context = new NpcTurnContext(
                    npc:               npc,
                    currentTile:       currentTile,
                    allTiles:          _tiles,
                    gridSize:          _gridSize,
                    remainingMovement: npc.IsRooted ? 0 : npc.CharacterStats.Movement,
                    remainingActions:  npc.CharacterStats.MaxActionPoints);

                context.BfsReachable       = (origin, range) => BfsReachable(origin, range, npc);
                context.FindPath           = (origin, dest)  => FindShortestPath(origin, dest, npc);
                context.PathCost           = path            => PathCost(path, npc);
                context.TrimPathToMovement = (path, budget)  => TrimPathToMovement(path, budget, npc);
                context.AnimateNpcMove = path => AnimateNpcMoveCoroutine(npc, context, path);
                context.ExecuteAttack  = targetTile => NpcExecuteAttack(npc, targetTile);
                context.ExecuteSpell   = (spell, targetTile)
                    => NpcExecuteSpell(npc, context.CurrentTile, spell, targetTile);

                yield return StartCoroutine(npc.AI.ExecuteTurn(context));

                CheckFightOutcome();
                if (_fightEnded) yield break;

                // Small pause between Npc turns so the player can follow the action.
                yield return new WaitForSeconds(0.25f);
            }

            Debug.Log("[TurnManager] Enemy turn complete.  Starting player turn.");
            // Tick status effects for all Npcs at the end of the enemy turn.
            foreach (var (npc, _) in npcTiles)
            {
                if (npc.CharacterStats.HealthPoints <= 0) continue;
                npc.TickStatusEffects();
            }

            CheckFightOutcome();
            if (_fightEnded) yield break;

            StartPlayerTurn();
        }

        /// <summary>Scans the grid for the tile currently occupied by <paramref name="npc"/>.</summary>
        private MapTile FindNpcTile(Npc npc)
        {
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                    if (_tiles[x, z].OccupyingNpc == npc)
                        return _tiles[x, z];
            return null;
        }

        /// <summary>
        /// Animates an Npc moving along <paramref name="path"/> tile by tile.
        /// Updates <c>context.CurrentTile</c> on completion.
        /// </summary>
        private IEnumerator AnimateNpcMoveCoroutine(Npc npc, NpcTurnContext context,
                                                     List<MapTile> path)
        {
            if (path == null || path.Count == 0) yield break;

            yield return StartCoroutine(MoveUnitAlongPath(npc, path[0], path, finalTile =>
            {
                context.CurrentTile = finalTile;
                Debug.Log($"[TurnManager] '{npc.CharacterName}' moved to "
                        + $"({finalTile.GridX}, {finalTile.GridZ}).");
            }));
        }

        /// <summary>
        /// Turns <paramref name="unit"/> to face <paramref name="targetTile"/> from its
        /// current tile, snapping to whichever grid axis (X or Z) has the larger offset —
        /// unlike step-by-step movement, an attack or spell target can be several _tiles
        /// away in both axes at once. No-ops if the unit has no tile/model yet or is
        /// already standing on the target tile (e.g. a Self-targeted spell).
        /// </summary>
        private static void FaceTowardTile(Character unit, MapTile targetTile)
        {
            MapTile fromTile = unit?.CharacterCurrentTile;
            if (fromTile == null || targetTile == null || fromTile == targetTile) return;

            int dx = targetTile.GridX - fromTile.GridX;
            int dz = targetTile.GridZ - fromTile.GridZ;
            if (dx == 0 && dz == 0) return;

            Vector2Int dir = Mathf.Abs(dx) >= Mathf.Abs(dz)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dz > 0 ? 1 : -1);

            fromTile.UnitObject?.GetComponent<SpriteCharacterView>()?.FaceGridDirection(dir.x, dir.y);
            unit.FacingDirection = dir;
        }

        /// <summary>
        /// Resolves a basic attack: applies damage, checks the target's
        /// OnReceivingAttack passives, logs, and clears the target's tile on defeat.
        /// Shared by player-on-Npc (<see cref="CommitAttack"/>) and Npc-on-player
        /// (<see cref="NpcExecuteAttack"/>) attacks.
        /// </summary>
        private void ResolveBasicAttack(Character attacker, Character target, MapTile targetTile)
        {
            attacker.BreakInvisibility();

            CheckAndHandleReceivingBasicAttackDamage(attacker, target);

            int damage = Mathf.Max(0, attacker.CharacterStats.TotalAttack);
            int dealt  = target.TakePhysicalDamage(damage);
            FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, SpellType.Physical);

            Debug.Log($"[TurnManager] '{attacker.CharacterName}' attacks '{target.CharacterName}' "
                    + $"for {damage} damage ({dealt} after shield).  "
                    + $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}"
                    + $"/{target.CharacterStats.MaxHealthPoints}");

            if (target.CharacterStats.HealthPoints <= 0)
            {
                Debug.Log($"[TurnManager] '{target.CharacterName}' has been defeated!");
                targetTile.RemoveUnit();
            }
        }

        private void CheckAndHandleReceivingBasicAttackDamage(Character attacker, Character target)
        {
            foreach (var passive in target.CharacterPassiveAbilities)
            {
                if (passive != null && passive.AbilityType == PassiveAbility.PassiveAbilityType.OnReceivingAttack)
                {
                    Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{target.CharacterName}' on receiving damage.");
                    passive.Execute(new PassiveOnReceivingAttackContext(
                        attacker: attacker,
                        receivingCharacter: target,
                        attackerTile: attacker.CharacterCurrentTile,
                        receivingCharacterTile: target.CharacterCurrentTile,
                        allTiles: _tiles,
                        gridSize: _gridSize));
                }
            }
        }

        /// <summary>Executes a basic attack from <paramref name="attacker"/> against the
        /// player Character on <paramref name="targetTile"/>.</summary>
        private void NpcExecuteAttack(Npc attacker, MapTile targetTile)
        {
            Character target = targetTile.OccupyingCharacter;
            if (target == null) return;

            // Defense-in-depth: AggressiveNpcAI already excludes Invisible targets when
            // choosing who to attack, but enforce it here too in case another AI type doesn't.
            if (target.IsInvisible && !attacker.IgnoresInvisibility)
            {
                Debug.LogWarning($"[TurnManager] '{attacker.CharacterName}' cannot target invisible '{target.CharacterName}'.");
                return;
            }

            FaceTowardTile(attacker, targetTile);
            ResolveBasicAttack(attacker, target, targetTile);
            CheckFightOutcome();
        }

        /// <summary>
        /// Builds a <see cref="SpellContext"/> for an Npc caster and invokes the spell's Execute.
        /// Deducts mana from <paramref name="caster"/> on success.
        /// Returns true if the spell resolved.
        /// </summary>
        private bool NpcExecuteSpell(Npc caster, MapTile casterTile,
                                     Spell spell, MapTile targetTile)
        {
            if (caster.IsSilenced) return false;
            if (spell.ManaCost > 0 && caster.CharacterStats.Mana < spell.ManaCost) return false;
            if (!IsSpellAvailable(caster, spell)) return false;

            // Defense-in-depth: AggressiveNpcAI already excludes Invisible Enemy targets when
            // choosing a spell target, but enforce it here too in case another AI type doesn't.
            Character targetCharacter = targetTile?.OccupyingCharacter;
            if (spell.TargetType == SpellTargetType.Enemy && targetCharacter != null &&
                targetCharacter.IsInvisible && !caster.IgnoresInvisibility)
                return false;

            var context = new SpellContext(
                caster:      caster,
                casterLevel: caster.Level,
                casterTile:  casterTile,
                targetTile:  targetTile,
                allTiles:    _tiles,
                gridSize:    _gridSize);

            FaceTowardTile(caster, targetTile);
            bool resolved = spell.Execute(context);

            if (resolved)
            {
                caster.CharacterStats.Mana =
                    Mathf.Max(0, caster.CharacterStats.Mana - spell.ManaCost);
                StartSpellCooldown(caster, spell);
                caster.BreakInvisibility();
            }

            CheckFightOutcome();

            return resolved;
        }

        /// <summary>
        /// True if <paramref name="other"/> is an enemy of <paramref name="mover"/> for
        /// Zone-of-Control purposes — factions are simply Npc vs. non-Npc Character.
        /// An Invisible <paramref name="other"/> is not considered an enemy unless
        /// <paramref name="mover"/> is an Npc that ignores invisibility, matching the
        /// perception rule <see cref="AggressiveNpcAI"/> already uses for targeting/pathing
        /// (see AggressiveNpcAI.CanTarget) — otherwise ZoC would reveal an invisible unit's
        /// position via a doubled movement cost the mover shouldn't be able to notice.
        /// </summary>
        private static bool IsEnemyOf(Character mover, Character other)
        {
            if ((mover is Npc) == (other is Npc)) return false;
            if (other.IsInvisible && !(mover is Npc n && n.IgnoresInvisibility)) return false;
            return true;
        }

        /// <summary>
        /// Movement cost of a single cardinal step from <paramref name="from"/> to
        /// <paramref name="to"/> for <paramref name="mover"/>. Costs 2 (instead of 1) if
        /// <paramref name="from"/> lies within an enemy's Zone of Control (the 8 tiles
        /// surrounding that enemy) and <paramref name="to"/> lies outside that same enemy's
        /// zone — i.e. the step leaves the zone. Checking all enemies adjacent to
        /// <paramref name="from"/> with an early-exit means overlapping enemy zones still
        /// only double the cost once, never stack.
        /// </summary>
        private int StepCost(MapTile from, MapTile to, Character mover)
        {
            foreach (MapTile t in SpellAreaUtils.GetCircleAroundTiles(from, 1, _tiles, _gridSize))
            {
                Character occupant = t.OccupyingUnit;
                if (occupant == null || !IsEnemyOf(mover, occupant)) continue;

                int dx = Mathf.Abs(to.GridX - t.GridX);
                int dz = Mathf.Abs(to.GridZ - t.GridZ);
                if (Mathf.Max(dx, dz) > 1) return 2;
            }
            return 1;
        }

        /// <summary>Total Zone-of-Control-aware movement cost of an already-found
        /// <paramref name="path"/> (as returned by <see cref="FindShortestPath"/>).</summary>
        private int PathCost(List<MapTile> path, Character mover)
        {
            int cost = 0;
            for (int i = 1; i < path.Count; i++)
                cost += StepCost(path[i - 1], path[i], mover);
            return cost;
        }

        /// <summary>Returns the longest affordable prefix of <paramref name="path"/> (which
        /// must start at the mover's current tile) given a movement point <paramref name="budget"/>,
        /// accounting for Zone-of-Control step costs. May return just the origin tile if even
        /// the first step is unaffordable.</summary>
        private List<MapTile> TrimPathToMovement(List<MapTile> path, int budget, Character mover)
        {
            var trimmed = new List<MapTile> { path[0] };
            int spent = 0;
            for (int i = 1; i < path.Count; i++)
            {
                int stepCost = StepCost(path[i - 1], path[i], mover);
                if (spent + stepCost > budget) break;
                spent += stepCost;
                trimmed.Add(path[i]);
            }
            return trimmed;
        }

        /// <summary>
        /// Dijkstra: returns all _tiles reachable within <paramref name="range"/> movement
        /// points of <paramref name="mover"/>, excluding occupied _tiles (characters may not
        /// pass through or land on them). Step costs vary (see <see cref="StepCost"/>), so a
        /// genuine relax-then-settle loop is used rather than plain BFS.
        /// </summary>
        private HashSet<MapTile> BfsReachable(MapTile origin, int range, Character mover)
        {
            var dist     = new Dictionary<MapTile, int> { [origin] = 0 };
            var settled  = new HashSet<MapTile>();
            var frontier = new List<MapTile> { origin };

            while (frontier.Count > 0)
            {
                MapTile cur  = null;
                int     best = int.MaxValue;
                foreach (MapTile t in frontier)
                    if (dist[t] < best) { best = dist[t]; cur = t; }

                frontier.Remove(cur);
                settled.Add(cur);
                if (best > range) break;

                foreach (MapTile nb in Neighbors(cur))
                {
                    if (nb.IsOccupied || settled.Contains(nb)) continue;

                    int nd = best + StepCost(cur, nb, mover);
                    if (nd > range) continue;

                    if (!dist.TryGetValue(nb, out int old) || nd < old)
                    {
                        dist[nb] = nd;
                        if (!frontier.Contains(nb)) frontier.Add(nb);
                    }
                }
            }

            return new HashSet<MapTile>(settled);
        }

        /// <summary>
        /// Dijkstra shortest (cheapest) path from <paramref name="origin"/> to
        /// <paramref name="dest"/> for <paramref name="mover"/>, accounting for
        /// Zone-of-Control step costs (see <see cref="StepCost"/>).
        /// Returns an ordered list that starts with origin and ends with dest.
        /// </summary>
        private List<MapTile> FindShortestPath(MapTile origin, MapTile dest, Character mover)
        {
            var dist     = new Dictionary<MapTile, int> { [origin] = 0 };
            var prev     = new Dictionary<MapTile, MapTile>();
            var settled  = new HashSet<MapTile>();
            var frontier = new List<MapTile> { origin };

            while (frontier.Count > 0)
            {
                MapTile cur  = null;
                int     best = int.MaxValue;
                foreach (MapTile t in frontier)
                    if (dist[t] < best) { best = dist[t]; cur = t; }

                frontier.Remove(cur);
                settled.Add(cur);
                if (cur == dest) break;

                foreach (MapTile nb in Neighbors(cur))
                {
                    if (nb.IsOccupied && nb != dest) continue;
                    if (settled.Contains(nb)) continue;

                    int nd = best + StepCost(cur, nb, mover);
                    if (!dist.TryGetValue(nb, out int old) || nd < old)
                    {
                        dist[nb] = nd;
                        prev[nb] = cur;
                        if (!frontier.Contains(nb)) frontier.Add(nb);
                    }
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
            if (x > 0)            list.Add(_tiles[x - 1, z]);
            if (x < _gridSize - 1) list.Add(_tiles[x + 1, z]);
            if (z > 0)            list.Add(_tiles[x, z - 1]);
            if (z < _gridSize - 1) list.Add(_tiles[x, z + 1]);
            return list;
        }

        // ------------------------------------------------------------------ //
        // Movement animation                                                  //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Walks <paramref name="unit"/> along <paramref name="path"/> tile-by-tile at
        /// <see cref="MoveSpeed"/>, handing tile occupancy off at each _step. Invokes
        /// <paramref name="onArrived"/> with the final tile once movement completes.
        /// No-ops (without invoking <paramref name="onArrived"/>) if the path is trivial
        /// or the unit has no model on <paramref name="fromTile"/>.
        /// Shared by player movement, Npc movement, and caster repositioning (e.g. Charge).
        /// </summary>
        private IEnumerator MoveUnitAlongPath(Character unit, MapTile fromTile,
                                               List<MapTile> path, Action<MapTile> onArrived)
        {
            if (path == null || path.Count <= 1) yield break;

            GameObject model = fromTile.UnitObject;
            if (model == null)
            {
                Debug.LogWarning($"[TurnManager] '{unit.CharacterName}' model not found during move.");
                yield break;
            }

            MapTile currentTile = fromTile;
            var spriteView = model.GetComponent<SpriteCharacterView>();

            for (int i = 1; i < path.Count; i++)
            {
                MapTile next     = path[i];
                spriteView?.FaceGridDirection(next.GridX - currentTile.GridX, next.GridZ - currentTile.GridZ);
                unit.FacingDirection = new Vector2Int(next.GridX - currentTile.GridX, next.GridZ - currentTile.GridZ);
                Vector3 startPos = model.transform.position;
                float   halfH    = next.transform.lossyScale.y * 0.5f;
                Vector3 endPos   = next.transform.position + Vector3.up * (halfH + 0.05f);
                float   elapsed  = 0f;
                float   duration = _step / MoveSpeed;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    model.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                    yield return null;
                }

                // Snap to exact position and transfer occupancy.
                model.transform.position = endPos;
                currentTile.ReleaseUnit();
                next.AssignUnit(unit, model);
                currentTile = next;
            }

            onArrived?.Invoke(currentTile);
        }

        private IEnumerator AnimateMove(Character character, MapTile fromTile,
                                        List<MapTile> path)
        {
            _isMoving = true;

            yield return StartCoroutine(MoveUnitAlongPath(character, fromTile, path, finalTile =>
            {
                _selectedTile = finalTile;
                Debug.Log($"[TurnManager] '{character.CharacterName}' arrived at " +
                          $"({finalTile.GridX}, {finalTile.GridZ}).  " +
                          $"MP: {RemainingMove(character)}  AP: {RemainingActions(character)}");
            }));

            _isMoving = false;

            if (_selectedCharacter == character)
            {
                CheckAndHandleTurnEnd(character);
                if (_selectedCharacter != null)
                    ShowActionPanel();
            }
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Non-null while the player is aiming a basic attack or a targeted spell.
        /// <c>spell == null</c> means a basic attack. Used by CharacterHudController to
        /// preview the pending action's effect on the HUD before the player commits.
        /// </summary>
        public (Character caster, Spell spell)? PendingAction
        {
            get
            {
                if (_selectedCharacter == null) return null;
                if (_currentMode == InputMode.Attack) return (_selectedCharacter, (Spell)null);
                if (_currentMode == InputMode.Spell && _selectedSpell != null) return (_selectedCharacter, _selectedSpell);
                return null;
            }
        }

        /// <summary>Movement points <paramref name="character"/> has left this turn (0 if untracked, e.g. an enemy Npc).</summary>
        public int RemainingMove(Character character)
        {
            int mp;
            return _remainingMovement.TryGetValue(character, out mp) ? mp : 0;
        }

        private int RemainingActions(Character character)
        {
            int ap;
            return _remainingActions.TryGetValue(character, out ap) ? ap : 0;
        }

        // ------------------------------------------------------------------ //
        // Spell cooldowns                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Full rounds still blocking <paramref name="spell"/> for <paramref name="character"/>
        /// (0 = usable now). Returns int.MaxValue for a once-per-fight spell that's been used.
        /// </summary>
        public int GetCooldownRemaining(Character character, Spell spell)
        {
            if (!_spellAvailableAtRound.TryGetValue(character, out var map)) return 0;
            if (!map.TryGetValue(spell, out int availableAtRound)) return 0;
            if (availableAtRound == int.MaxValue) return int.MaxValue;
            return Mathf.Max(0, availableAtRound - _roundNumber);
        }

        private bool IsSpellAvailable(Character character, Spell spell) =>
            GetCooldownRemaining(character, spell) <= 0;

        /// <summary>Call after a spell successfully resolves to start its cooldown (if any).</summary>
        private void StartSpellCooldown(Character character, Spell spell)
        {
            if (!spell.OncePerFight && spell.Cooldown <= 0) return;

            if (!_spellAvailableAtRound.TryGetValue(character, out var map))
            {
                map = new Dictionary<Spell, int>();
                _spellAvailableAtRound[character] = map;
            }

            // +1 because a cooldown of N must still block the *next* N rounds, not N-1 —
            // the round it was cast in doesn't count toward the cooldown itself.
            map[spell] = spell.OncePerFight ? int.MaxValue : _roundNumber + spell.Cooldown + 1;
        }

        /// <summary>Ensures an EventSystem, TooltipSystem, and FloatingCombatTextSystem exist in the scene.</summary>
        private void EnsureMapUI()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            if (TooltipSystem.Instance == null)
                new GameObject("[TooltipSystem]").AddComponent<TooltipSystem>();
            if (FloatingCombatTextSystem.Instance == null)
                new GameObject("[FloatingCombatTextSystem]").AddComponent<FloatingCombatTextSystem>();
        }
    }
}