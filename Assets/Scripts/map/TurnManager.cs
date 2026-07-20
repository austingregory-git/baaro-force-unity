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
using BaaroForce.Passives;
using BaaroForce.GameController;
using BaaroForce.Relics;

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

        //_spellTargetTiles
        private readonly List<MapTile> _spellTargetTiles = new List<MapTile>();
        private readonly List<MapTile> _spellPreviewTiles = new List<MapTile>();

        private Spell         _selectedSpell;
        private ActionPanelUI  _actionPanel;
        private SpellPanelUI   _spellPanel;
        private bool           _isMoving;
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
            CheckAndHandlePlayerTurnStartPassives(members);

            CheckAndHandleTurnStartRelics(relics);
        }

        private void CheckAndHandleTurnStartRelics(List<Relic> relics)
        {
            return;
            //throw new NotImplementedException();
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
            foreach (MapTile tile in BfsReachable(origin, range))
            {
                if (tile == origin) continue;
                tile.SetMoveHighlight(true);
                _highlightedMoveTiles.Add(tile);
            }
        }

        private void ClearMoveHighlights()
        {
            foreach (MapTile t in _highlightedMoveTiles)
                t.SetMoveHighlight(false);
            _highlightedMoveTiles.Clear();
        }

        private void CommitMove(MapTile destination)
        {
            SetMode(InputMode.None);
            List<MapTile> path = FindShortestPath(_selectedTile, destination);
            int manaCost = path.Count - 1;
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
            if (spell.TargetType == SpellTargetType.Self) return;

            Color color = GetSpellHighlightColor(spell.TargetType);
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

            List<MapTile> areaTiles =
                SpellAreaUtils.GetHorizontalLineTiles(
                    _selectedTile,
                    _hoveredTile,
                    _selectedSpell.Range,
                    _selectedSpell.Area,
                    _tiles,
                    _gridSize);

            foreach (var tile in areaTiles)
            {
                tile.SetSpellHighlight(
                    true,
                    new Color(1f, 0.5f, 0f, 0.8f));

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
            SetMode(InputMode.None);   // clears highlights and nulls _selectedSpell

            if (spell.ManaCost > 0 && _selectedCharacter.CharacterStats.Mana < spell.ManaCost)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' " +
                           "does not have enough mana to cast this spell.");
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

            bool resolved = spell.Execute(context);

            // Always spend one action point — the attempt was made.
            _remainingActions[_selectedCharacter] =
                Mathf.Max(0, RemainingActions(_selectedCharacter) - 1);

            // Only deduct mana and start the cooldown on a successful cast.
            if (resolved)
            {
                _selectedCharacter.CharacterStats.Mana =
                    Mathf.Max(0, _selectedCharacter.CharacterStats.Mana - spell.ManaCost);
                StartSpellCooldown(_selectedCharacter, spell);
            }

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

            List<MapTile> path = FindShortestPath(fromTile, landingTile);
            yield return StartCoroutine(MoveUnitAlongPath(caster, fromTile, path, finalTile =>
            {
                _selectedTile = finalTile;
            }));

            _isMoving = false;

            // Caster is now in position — resolve the spell's damage / effect.
            bool resolved = spell.Execute(context);

            _remainingActions[caster] =
                Mathf.Max(0, RemainingActions(caster) - 1);

            if (resolved)
            {
                caster.CharacterStats.Mana =
                    Mathf.Max(0, caster.CharacterStats.Mana - spell.ManaCost);
                StartSpellCooldown(caster, spell);
            }

            CheckAndHandleTurnEnd(caster);
            if (_selectedCharacter != null)
                ShowActionPanel();
        }

        private Color GetSpellHighlightColor(SpellTargetType targetType)
        {
            switch (targetType)
            {
                case SpellTargetType.Enemy: return new Color(0.9f, 0.15f, 0.10f, 0.55f); // red
                case SpellTargetType.Ally:  return new Color(0.1f, 0.80f, 0.20f, 0.55f); // green
                case SpellTargetType.Both:  return new Color(0.6f, 0.10f, 0.85f, 0.55f); // purple
                default:                   return new Color(0.9f, 0.15f, 0.10f, 0.55f);
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
        /// Activates a spell for the currently selected character.
        /// Self-targeting spells execute immediately; all others enter targeting mode.
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

            int ap = RemainingActions(_selectedCharacter);
            if (ap <= 0)
            {
                Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' has no actions remaining.");
                return;
            }

            if (spell.ManaCost > 0 && _selectedCharacter.CharacterStats.Mana < spell.ManaCost)
            {
                Debug.Log($"[TurnManager] Not enough mana to cast '{spell.Name}'.");
                return;
            }

            if (!IsSpellAvailable(_selectedCharacter, spell))
            {
                Debug.Log($"[TurnManager] '{spell.Name}' is still on cooldown " +
                          $"({GetCooldownRemaining(_selectedCharacter, spell)} round(s) left).");
                return;
            }

            if (spell.TargetType == SpellTargetType.Self)
            {
                // Self spells need no target tile — execute immediately.
                SetMode(InputMode.None);
                var selfContext = new SpellContext(
                    caster:      _selectedCharacter,
                    casterLevel: _selectedCharacter.Level,
                    casterTile:  _selectedTile,
                    targetTile:  null,
                    allTiles:    _tiles,
                    gridSize:    _gridSize);

                bool selfResolved = spell.Execute(selfContext);

                _remainingActions[_selectedCharacter] =
                    Mathf.Max(0, RemainingActions(_selectedCharacter) - 1);

                if (selfResolved)
                {
                    _selectedCharacter.CharacterStats.Mana =
                        Mathf.Max(0, _selectedCharacter.CharacterStats.Mana - spell.ManaCost);
                    StartSpellCooldown(_selectedCharacter, spell);
                }

                CheckAndHandleTurnEnd(_selectedCharacter);
                if (_selectedCharacter != null)
                {
                    ShowActionPanel();
                    // Push updated shield/HP/mana/AP to the left-side HUD panel.
                    OnCharacterSelected?.Invoke(_selectedCharacter);
                }
                return;
            }

            // Targeted spell — hide panels and show range highlights.
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
            var members = PartyManager.Instance?.Party?.Members;
            if (members == null || members.Count == 0) { StartEnemyTurn(); return; }

            foreach (Character c in members)
                if (!_finishedCharacters.Contains(c) && c.CharacterStats.HealthPoints > 0)
                    return;

            var relics = PartyManager.Instance?.Relics;
            CheckAndHandlePlayerTurnEnd(members, relics);

            StartEnemyTurn();
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
                    remainingMovement: npc.CharacterStats.Movement,
                    remainingActions:  npc.CharacterStats.MaxActionPoints);

                context.BfsReachable   = BfsReachable;
                context.FindPath       = FindShortestPath;
                context.AnimateNpcMove = path => AnimateNpcMoveCoroutine(npc, context, path);
                context.ExecuteAttack  = targetTile => NpcExecuteAttack(npc, targetTile);
                context.ExecuteSpell   = (spell, targetTile)
                    => NpcExecuteSpell(npc, context.CurrentTile, spell, targetTile);

                yield return StartCoroutine(npc.AI.ExecuteTurn(context));

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
        /// Resolves a basic attack: applies damage, checks the target's
        /// OnReceivingAttack passives, logs, and clears the target's tile on defeat.
        /// Shared by player-on-Npc (<see cref="CommitAttack"/>) and Npc-on-player
        /// (<see cref="NpcExecuteAttack"/>) attacks.
        /// </summary>
        private void ResolveBasicAttack(Character attacker, Character target, MapTile targetTile)
        {
            CheckAndHandleReceivingBasicAttackDamage(attacker, target);

            int damage = Mathf.Max(0, attacker.CharacterStats.TotalAttack);
            int dealt  = target.CharacterStats.TakeDamage(damage);

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

            ResolveBasicAttack(attacker, target, targetTile);
        }

        /// <summary>
        /// Builds a <see cref="SpellContext"/> for an Npc caster and invokes the spell's Execute.
        /// Deducts mana from <paramref name="caster"/> on success.
        /// Returns true if the spell resolved.
        /// </summary>
        private bool NpcExecuteSpell(Npc caster, MapTile casterTile,
                                     Spell spell, MapTile targetTile)
        {
            if (spell.ManaCost > 0 && caster.CharacterStats.Mana < spell.ManaCost) return false;
            if (!IsSpellAvailable(caster, spell)) return false;

            var context = new SpellContext(
                caster:      caster,
                casterLevel: caster.Level,
                casterTile:  casterTile,
                targetTile:  targetTile,
                allTiles:    _tiles,
                gridSize:    _gridSize);

            bool resolved = spell.Execute(context);

            if (resolved)
            {
                caster.CharacterStats.Mana =
                    Mathf.Max(0, caster.CharacterStats.Mana - spell.ManaCost);
                StartSpellCooldown(caster, spell);
            }

            return resolved;
        }

        /// <summary>
        /// BFS: returns all _tiles reachable within <paramref name="range"/> cardinal steps,
        /// excluding occupied _tiles (characters may not pass through or land on them).
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

        /// <summary>Ensures an EventSystem and TooltipSystem exist in the scene.</summary>
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
        }
    }
}