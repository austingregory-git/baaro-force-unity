using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Spells;
using BaaroForce.UI;
using System;
using BaaroForce.Passives;

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

        private enum InputMode { None, Move, Attack, Spell }
        private InputMode currentMode = InputMode.None;

        private readonly List<MapTile> highlightedMoveTiles   = new List<MapTile>();
        private readonly List<MapTile> highlightedAttackTiles = new List<MapTile>();

        //spellTargetTiles
        private readonly List<MapTile> spellTargetTiles = new List<MapTile>();
        private readonly List<MapTile> spellPreviewTiles = new List<MapTile>();

        private Spell         selectedSpell;
        private ActionPanelUI  _actionPanel;
        private SpellPanelUI   _spellPanel;
        private bool           isMoving;
        private MapTile hoveredTile;


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

            EnsureMapUI();

            _actionPanel = gameObject.AddComponent<ActionPanelUI>();
            _actionPanel.OnMoveClicked   = ToggleMoveMode;
            _actionPanel.OnAttackClicked = ToggleAttackMode;
            _actionPanel.OnSpellsClicked = ShowSpellPanel;
            _actionPanel.OnItemsClicked  = () => Debug.Log("[TurnManager] Items — not yet implemented.");

            _spellPanel = gameObject.AddComponent<SpellPanelUI>();
            _spellPanel.OnSpellSelected = ActivateSpell;
            _spellPanel.OnBackClicked   = ShowActionPanel;
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
                foreach (var passive in c.characterPassiveAbilities)
                {
                    Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{c.characterName}' at start of turn.");
                    if (passive != null && passive.AbilityType == PassiveAbility.PassiveAbilityType.START_OF_TURN)
                    {
                        Debug.Log($"[TurnManager] Executing passive ability '{passive.Name}' for character '{c.characterName}'.");
                        passive.Execute(new PassiveAbilityContext(character: c, characterLevel: c.Level, characterTile: c.characterCurrentTile, allTiles: tiles, gridSize: gridSize));
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
            if (isMoving) return;

            UpdateHoveredTile();

            if (currentMode == InputMode.Spell &&
                selectedSpell != null &&
                selectedSpell.targetType == SpellTargetType.Area)
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
                hoveredTile = null;
                return;
            }

            hoveredTile = tile;
        }

        private bool TryGetTileUnderMouse(out MapTile tile)
        {
            tile = null;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

            if (!gridPlane.Raycast(ray, out float enter))
                return false;

            Vector3 hit = ray.GetPoint(enter);

            int gridX = Mathf.RoundToInt((hit.x - originX) / step);
            int gridZ = Mathf.RoundToInt((hit.z - originZ) / step);

            if (gridX < 0 || gridX >= gridSize ||
                gridZ < 0 || gridZ >= gridSize)
                return false;

            tile = tiles[gridX, gridZ];
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

                case InputMode.Spell:
                    if (spellTargetTiles.Contains(clicked))
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetMode(InputMode.None);
                ShowActionPanel();
                return;
            }

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
                ToggleSpellMode();
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
            ShowActionPanel();
        }

        private void Deselect()
        {
            SetMode(InputMode.None);
            selectedCharacter = null;
            selectedTile      = null;
            _actionPanel?.Hide();
            _spellPanel?.Hide();
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
                selectedSpell = null;

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
            int manaCost = path.Count - 1;
            remainingMovement[selectedCharacter] =
                Mathf.Max(0, RemainingMove(selectedCharacter) - manaCost);
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
            if (selectedCharacter != null) ShowActionPanel();
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
        // Spell mode                                                          //
        // ------------------------------------------------------------------ //

        private void ToggleSpellMode()
        {
            // Cancel active tile-targeting and return to the spell panel.
            if (currentMode == InputMode.Spell)
            {
                SetMode(InputMode.None);
                if (selectedCharacter != null) ShowSpellPanel();
                return;
            }

            // Toggle the spell panel off if it is already showing.
            if (_spellPanel != null && _spellPanel.IsVisible)
            {
                ShowActionPanel();
                return;
            }

            if (selectedCharacter == null) return;
            ShowSpellPanel();
        }

        private void ShowSpellRange(MapTile origin, Spell spell)
        {
            if (spell.targetType == SpellTargetType.Self) return;

            Color color = GetSpellHighlightColor(spell.targetType);
            int ox = origin.GridX, oz = origin.GridZ;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
                    if (dist <= 0 || dist > spell.range) continue;

                    MapTile tile = tiles[x, z];

                    tile.SetSpellHighlight(true, color);
                    spellTargetTiles.Add(tile);
                }
            }
        }

        private void UpdateSpellPreview()
        {
            ClearPreviewTiles();

            if (hoveredTile == null)
                return;

            int distance =
                Mathf.Abs(hoveredTile.GridX - selectedTile.GridX) +
                Mathf.Abs(hoveredTile.GridZ - selectedTile.GridZ);

            if (distance == 0 || distance > selectedSpell.range)
                return;

            List<MapTile> areaTiles =
                SpellAreaUtils.GetHorizontalLineTiles(
                    selectedTile,
                    hoveredTile,
                    selectedSpell.range,
                    selectedSpell.area,
                    tiles,
                    gridSize);

            foreach (var tile in areaTiles)
            {
                tile.SetSpellHighlight(
                    true,
                    new Color(1f, 0.5f, 0f, 0.8f));

                spellPreviewTiles.Add(tile);
            }
        }

        private void ClearPreviewTiles()
        {
            foreach (var tile in spellPreviewTiles)
                tile.SetSpellHighlight(false, Color.clear);

            spellPreviewTiles.Clear();
        }

        private void ClearSpellHighlights()
        {
            foreach (MapTile t in spellTargetTiles)
                t.SetSpellHighlight(false, Color.clear);
            spellTargetTiles.Clear();
        }

        private void CommitSpell(MapTile targetTile)
        {
            if (selectedSpell == null) return;

            Spell spell = selectedSpell;
            SetMode(InputMode.None);   // clears highlights and nulls selectedSpell

            if (spell.manaCost > 0 && selectedCharacter.characterStats.mana < spell.manaCost)
            {
                Debug.Log($"[TurnManager] '{selectedCharacter.characterName}' " +
                           "does not have enough mana to cast this spell.");
                return;
            }

            var context = new SpellContext(
                caster:      selectedCharacter,
                casterLevel: selectedCharacter.Level,
                casterTile:  selectedTile,
                targetTile:  targetTile,
                allTiles:    tiles,
                gridSize:    gridSize);

            // If the spell physically repositions the caster first (e.g. Charge),
            // animate the movement and resolve the effect at the end of that coroutine.
            MapTile landingTile = spell.GetCasterLandingTile(context);
            if (landingTile != null)
            {
                StartCoroutine(SpellWithMovement(selectedCharacter, selectedTile,
                                                 landingTile, spell, context));
                return;
            }

            bool resolved = spell.Execute(context);

            // Always spend one action point — the attempt was made.
            remainingActions[selectedCharacter] =
                Mathf.Max(0, RemainingActions(selectedCharacter) - 1);

            // Only deduct mana on a successful cast.
            if (resolved)
                selectedCharacter.characterStats.mana =
                    Mathf.Max(0, selectedCharacter.characterStats.mana - spell.manaCost);

            CheckAndHandleTurnEnd(selectedCharacter);
            if (selectedCharacter != null)
                ShowActionPanel();
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
            isMoving = true;

            List<MapTile> path  = FindShortestPath(fromTile, landingTile);
            GameObject    model = fromTile.CharacterObject;

            if (model != null && path.Count > 1)
            {
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
                        model.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                        yield return null;
                    }

                    model.transform.position = endPos;
                    currentTile.ReleaseCharacter();
                    next.AssignCharacter(caster, model);
                    currentTile = next;
                }

                selectedTile = currentTile;
            }

            isMoving = false;

            // Caster is now in position — resolve the spell's damage / effect.
            bool resolved = spell.Execute(context);

            remainingActions[caster] =
                Mathf.Max(0, RemainingActions(caster) - 1);

            if (resolved)
                caster.characterStats.mana =
                    Mathf.Max(0, caster.characterStats.mana - spell.manaCost);

            CheckAndHandleTurnEnd(caster);
            if (selectedCharacter != null)
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
            if (character.characterSpells == null) return null;
            foreach (Spell spell in character.characterSpells)
                if (character.characterStats.mana >= spell.manaCost)
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
            if (selectedCharacter == null) return;

            // Toggle off when the same spell is already queued.
            if (currentMode == InputMode.Spell && selectedSpell == spell)
            {
                SetMode(InputMode.None);
                return;
            }

            int ap = RemainingActions(selectedCharacter);
            if (ap <= 0)
            {
                Debug.Log($"[TurnManager] '{selectedCharacter.characterName}' has no actions remaining.");
                return;
            }

            if (spell.manaCost > 0 && selectedCharacter.characterStats.mana < spell.manaCost)
            {
                Debug.Log($"[TurnManager] Not enough mana to cast '{spell.name}'.");
                return;
            }

            if (spell.targetType == SpellTargetType.Self)
            {
                // Self spells need no target tile — execute immediately.
                SetMode(InputMode.None);
                var selfContext = new SpellContext(
                    caster:      selectedCharacter,
                    casterLevel: selectedCharacter.Level,
                    casterTile:  selectedTile,
                    targetTile:  null,
                    allTiles:    tiles,
                    gridSize:    gridSize);

                bool selfResolved = spell.Execute(selfContext);

                remainingActions[selectedCharacter] =
                    Mathf.Max(0, RemainingActions(selectedCharacter) - 1);

                if (selfResolved)
                    selectedCharacter.characterStats.mana =
                        Mathf.Max(0, selectedCharacter.characterStats.mana - spell.manaCost);

                CheckAndHandleTurnEnd(selectedCharacter);
                if (selectedCharacter != null)
                    ShowActionPanel();
                return;
            }

            // Targeted spell — hide panels and show range highlights.
            _actionPanel?.Hide();
            _spellPanel?.Hide();
            SetMode(InputMode.Spell);
            selectedSpell = spell;
            ShowSpellRange(selectedTile, spell);
            Debug.Log($"[TurnManager] Spell mode: '{spell.name}'  " +
                      $"(Range: {spell.range}, manaCost: {spell.manaCost} mana)");

        }

        // ------------------------------------------------------------------ //
        // Panel management                                                    //
        // ------------------------------------------------------------------ //

        /// <summary>Returns to the character action menu.  Clears any active input mode.</summary>
        private void ShowActionPanel()
        {
            SetMode(InputMode.None);
            _spellPanel?.Hide();
            if (selectedCharacter != null)
                _actionPanel?.Show(selectedCharacter);
        }

        /// <summary>Opens the spell selection panel.  Clears any active input mode.</summary>
        private void ShowSpellPanel()
        {
            SetMode(InputMode.None);
            _actionPanel?.Hide();
            if (selectedCharacter != null)
                _spellPanel?.Show(selectedCharacter);
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
                foreach (var passive in c.characterPassiveAbilities)
                {
                    Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{c.characterName}' at end of turn.");
                    if (passive != null && passive.AbilityType == PassiveAbility.PassiveAbilityType.END_OF_TURN)
                    {
                        Debug.Log($"[TurnManager] Executing passive ability '{passive.Name}' for character '{c.characterName}'.");
                        passive.Execute(new PassiveAbilityContext(character: c, characterLevel: c.Level, characterTile: c.characterCurrentTile, allTiles: tiles, gridSize: gridSize));
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
        /// Runs every living NPC's AI turn sequentially, then returns control
        /// to the player by calling StartPlayerTurn.
        /// </summary>
        private IEnumerator RunEnemyTurns()
        {
            // Snapshot NPC list before acting — NPCs that die mid-turn are skipped.
            var npcTiles = new List<(NPC npc, MapTile tile)>();
            for (int x = 0; x < gridSize; x++)
                for (int z = 0; z < gridSize; z++)
                {
                    MapTile t = tiles[x, z];
                    if (t.OccupyingNpc != null)
                        npcTiles.Add((t.OccupyingNpc, t));
                }

            if (npcTiles.Count == 0)
            {
                Debug.Log("[TurnManager] No NPCs remain.  Starting player turn.");
                StartPlayerTurn();
                yield break;
            }

            foreach (var (npc, _) in npcTiles)
            {
                if (npc.characterStats.healthPoints <= 0) continue;
                if (npc.AI == null) continue;

                // Re-locate the NPC's current tile (it may have moved earlier this round).
                MapTile currentTile = FindNpcTile(npc);
                if (currentTile == null) continue;

                var context = new NpcTurnContext(
                    npc:               npc,
                    currentTile:       currentTile,
                    allTiles:          tiles,
                    gridSize:          gridSize,
                    remainingMovement: npc.characterStats.movement,
                    remainingActions:  npc.characterStats.maxActionPoints);

                context.BfsReachable   = BfsReachable;
                context.FindPath       = FindShortestPath;
                context.AnimateNpcMove = path => AnimateNpcMoveCoroutine(npc, context, path);
                context.ExecuteAttack  = targetTile => NpcExecuteAttack(npc, targetTile);
                context.ExecuteSpell   = (spell, targetTile)
                    => NpcExecuteSpell(npc, context.CurrentTile, spell, targetTile);

                yield return StartCoroutine(npc.AI.ExecuteTurn(context));

                // Small pause between NPC turns so the player can follow the action.
                yield return new WaitForSeconds(0.25f);
            }

            Debug.Log("[TurnManager] Enemy turn complete.  Starting player turn.");
            // Tick status effects for all NPCs at the end of the enemy turn.
            foreach (var (npc, _) in npcTiles)
            {
                if (npc.characterStats.healthPoints <= 0) continue;
                npc.TickStatusEffects();
            }

            StartPlayerTurn();
        }

        /// <summary>Scans the grid for the tile currently occupied by <paramref name="npc"/>.</summary>
        private MapTile FindNpcTile(NPC npc)
        {
            for (int x = 0; x < gridSize; x++)
                for (int z = 0; z < gridSize; z++)
                    if (tiles[x, z].OccupyingNpc == npc)
                        return tiles[x, z];
            return null;
        }

        /// <summary>
        /// Animates an NPC moving along <paramref name="path"/> tile by tile.
        /// Updates <c>context.CurrentTile</c> on completion.
        /// </summary>
        private IEnumerator AnimateNpcMoveCoroutine(NPC npc, NpcTurnContext context,
                                                     List<MapTile> path)
        {
            if (path == null || path.Count <= 1) yield break;

            GameObject model = path[0].NpcObject;
            if (model == null)
            {
                Debug.LogWarning($"[TurnManager] NPC '{npc.characterName}' model not found during move.");
                yield break;
            }

            MapTile currentTile = path[0];
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
                    model.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                    yield return null;
                }

                model.transform.position = endPos;
                currentTile.ReleaseNpc();
                next.AssignNpc(npc, model);
                currentTile = next;
            }

            context.CurrentTile = currentTile;
            Debug.Log($"[TurnManager] '{npc.characterName}' moved to "
                    + $"({currentTile.GridX}, {currentTile.GridZ}).");
        }

        /// <summary>Executes a basic attack from <paramref name="attacker"/> against the
        /// player Character on <paramref name="targetTile"/>.</summary>
        private void NpcExecuteAttack(NPC attacker, MapTile targetTile)
        {
            Character target = targetTile.OccupyingCharacter;
            if (target == null) return;

            int damage = attacker.characterStats.TotalAttack;
            target.characterStats.healthPoints -= damage;

            Debug.Log($"[TurnManager] '{attacker.characterName}' attacks '{target.characterName}' "
                    + $"for {damage} damage.  "
                    + $"HP: {Mathf.Max(0, target.characterStats.healthPoints)}"
                    + $"/{target.characterStats.maxHealthPoints}");

            if (target.characterStats.healthPoints <= 0)
            {
                Debug.Log($"[TurnManager] '{target.characterName}' has been defeated!");
                targetTile.RemoveCharacter();
            }
        }

        /// <summary>
        /// Builds an <see cref="NpcSpellContext"/> and invokes the spell's NPC Execute overload.
        /// Deducts mana from <paramref name="caster"/> on success.
        /// Returns true if the spell resolved.
        /// </summary>
        private bool NpcExecuteSpell(NPC caster, MapTile casterTile,
                                     Spell spell, MapTile targetTile)
        {
            if (spell.manaCost > 0 && caster.characterStats.mana < spell.manaCost) return false;

            var ctx = new NpcSpellContext(
                caster:      caster,
                casterLevel: caster.Level,
                casterTile:  casterTile,
                targetTile:  targetTile,
                allTiles:    tiles,
                gridSize:    gridSize);

            bool resolved = spell.Execute(ctx);

            if (resolved)
                caster.characterStats.mana =
                    Mathf.Max(0, caster.characterStats.mana - spell.manaCost);

            return resolved;
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
            {
                CheckAndHandleTurnEnd(character);
                if (selectedCharacter != null)
                    ShowActionPanel();
            }
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

        /// <summary>Ensures an EventSystem and TooltipSystem exist in the scene.</summary>
        private void EnsureMapUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
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
