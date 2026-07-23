using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BaaroForce.ActMap;
using BaaroForce.ActMap.Encounters;
using BaaroForce.Animations;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Spells;
using BaaroForce.Statuses;
using BaaroForce.UI;
using System;
using UnityEngine.SceneManagement;
using BaaroForce.Passives;
using BaaroForce.GameController;
using BaaroForce.Relics;
using BaaroForce.Loot;
using BaaroForce.Items;

namespace BaaroForce.Map
{
    public enum TurnPhase { Deployment, PlayerTurn, EnemyTurn }

    /// <summary>
    /// Controls the battle turn loop.
    ///
    /// Flow:  DeploymentManager fires OnDeploymentComplete
    ///        → StartPlayerTurn()
    ///        → player selects a unit by clicking it
    ///        → W = move mode, A = attack, S = spells, D/I = items
    ///        (M is reserved for CombatCornerMenu's read-only map overlay)
    ///
    /// Movement:
    ///   • W highlights reachable _tiles (blue, near-opaque) via BFS.
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

        private GridPathfinder _pathfinder;
        private ZoneOfControlOutlines _zocOutlines;

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
        private ActionPanelUI   _actionPanel;
        private CharacterInspectUI _inspectPanel;
        private SpellPanelUI    _spellPanel;
        private WarningToastUI  _warningToast;
        private FightResultUI   _fightResultUI;
        private LevelUpUI       _levelUpUI;
        private TileInfoPanelUI _tileInfoPanel;
        private CombatCornerMenu _cornerMenu;
        private EndTurnConfirmUI _endTurnConfirm;
        private bool           _isMoving;
        private bool           _fightEnded;
        private MapTile _hoveredTile;
        private Npc     _hoveredTarget;
        private MapTile _hoverHighlightTile;

        /// <summary>The tile currently selected for inspection (see SetInspectedTile) —
        /// distinct from _selectedTile, which is the acting character's tile for movement.</summary>
        private MapTile _inspectedTile;


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
            _pathfinder  = new GridPathfinder(_tiles, _gridSize);
            _zocOutlines = new ZoneOfControlOutlines(transform, _gridSize, _step, _originX, _originZ);

            EnsureMapUI();
            CreateActionAndSpellPanels();
            CreateToastAndInfoUI();
            CreateFightResultUI();
        }

        private void CreateActionAndSpellPanels()
        {
            _actionPanel = gameObject.AddComponent<ActionPanelUI>();
            _actionPanel.OnMoveClicked   = ToggleMoveMode;
            _actionPanel.OnAttackClicked = ToggleAttackMode;
            _actionPanel.OnSpellsClicked = ShowSpellPanel;
            _actionPanel.OnItemsClicked  = () => Debug.Log("[TurnManager] Items — not yet implemented.");
            _actionPanel.OnInspectClicked = () => _inspectPanel.Show(_selectedCharacter);
            _actionPanel.OnWaitClicked   = () => EndCharacterTurn(_selectedCharacter);

            _inspectPanel = gameObject.AddComponent<CharacterInspectUI>();

            _spellPanel = gameObject.AddComponent<SpellPanelUI>();
            _spellPanel.OnSpellSelected = ActivateSpell;
            _spellPanel.OnBackClicked   = ShowActionPanel;
            _spellPanel.GetCooldownRemaining = GetCooldownRemaining;
        }

        private void CreateToastAndInfoUI()
        {
            _warningToast   = gameObject.AddComponent<WarningToastUI>();
            _endTurnConfirm = gameObject.AddComponent<EndTurnConfirmUI>();
            _tileInfoPanel  = gameObject.AddComponent<TileInfoPanelUI>();

            gameObject.AddComponent<CombatLogUI>();
            _cornerMenu = gameObject.AddComponent<CombatCornerMenu>();
        }

        private void CreateFightResultUI()
        {
            _fightResultUI = gameObject.AddComponent<FightResultUI>();
            _fightResultUI.GoldFlightTarget = _cornerMenu.GoldTarget;
            _fightResultUI.ItemFlightTarget = _cornerMenu.InventoryTarget;
            _levelUpUI = gameObject.AddComponent<LevelUpUI>();

            _fightResultUI.OnReturnToMainMenu = ReturnToMainMenu;
            _fightResultUI.OnMoveOn           = ShowLevelUpThenAdvance;
            _fightResultUI.OnLootClaimed      = ClaimLoot;
        }

        private void ReturnToMainMenu()
        {
            PartyManager.Instance.ResetForNewRun();
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Restores everyone to their pre-combat baseline (full HP/mana, no leftover shield or
        /// status effects) before the level-up reveal builds its cards, so a character who
        /// simply took damage or was buffed mid-fight doesn't show a half-empty bar or stale
        /// bonus for reasons unrelated to leveling up. Swaps to the level-up reveal screen first
        /// if anyone leveled up this fight — it no-ops straight through to the Act Map when
        /// nobody did.
        /// </summary>
        private void ShowLevelUpThenAdvance()
        {
            foreach (Character member in PartyManager.Instance.Party.Members)
                member.ResetPostCombatState();

            _fightResultUI.Hide();
            _levelUpUI.Show(PartyManager.Instance.Party.Members, AdvanceToActMap);
        }

        private void AdvanceToActMap()
        {
            PartyManager.Instance.ActRun.PendingEncounter = null;
            PartyManager.Instance.ActRun.CompleteCurrentNode();
            SceneManager.LoadScene("ActMapScene");
        }

        private void ClaimLoot(LootEntry entry)
        {
            if (entry.Type == LootType.Gold)
            {
                PartyManager.Instance.Party.AddGold(entry.Amount);
                _cornerMenu?.AnimateGoldGain();
                return;
            }

            if (entry.Equipment != null) ClaimEquipment(entry.Equipment);
            else if (entry.Potion != null) ClaimPotion(entry.Potion);
        }

        private void ClaimEquipment(Equipment equipment)
        {
            if (!PartyManager.Instance.Party.TryAddEquipment(equipment))
                _warningToast?.Show($"Inventory full — '{equipment.Name}' was lost.");
        }

        private void ClaimPotion(Potion potion)
        {
            if (!PartyManager.Instance.Party.TryAddPotion(potion))
                _warningToast?.Show($"Inventory full — '{potion.Name}' was lost.");
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
            ResetTurnResourcesFor(members);
            CheckAndHandlePlayerTurnStart(members, PartyManager.Instance?.Relics);

            Debug.Log("[TurnManager] Player turn started.");
        }

        private void ResetTurnResourcesFor(List<Character> members)
        {
            if (members == null) return;
            foreach (Character c in members)
            {
                _remainingMovement[c] = c.CharacterStats.Movement;
                _remainingActions[c]  = c.CharacterStats.MaxActionPoints;
            }
        }

        private void CheckAndHandlePlayerTurnStart(List<Character> members, List<Relic> relics)
        {
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
                foreach (var passive in c.CharacterPassiveAbilities)
                    ExecuteStartOfCombatPassive(passive, c);
        }

        /// <summary>Resets per-combat state for every passive, regardless of its trigger — e.g.
        /// Spiritual Protector's "already healed this fight" flag, even though its own
        /// AbilityType is OnAllyDamaged, not StartOfCombat — then fires it if it is one.</summary>
        private void ExecuteStartOfCombatPassive(PassiveAbility passive, Character c)
        {
            if (passive == null) return;
            passive.OnCombatStart();

            if (passive.AbilityType != PassiveAbility.PassiveAbilityType.StartOfCombat) return;
            Debug.Log($"[TurnManager] Executing start-of-combat passive '{passive.Name}' for character '{c.CharacterName}'.");
            passive.Execute(new PassiveOnTurnContext(character: c, characterLevel: c.Level, characterTile: c.CharacterCurrentTile, allTiles: _tiles, gridSize: _gridSize));
        }

        private void CheckAndHandlePlayerTurnStartPassives(List<Character> members)
        {
            foreach (Character c in members)
            {
                ApplyTerrainEffects(c, c.CharacterCurrentTile);
                foreach (var passive in c.CharacterPassiveAbilities)
                    ExecuteStartOfTurnPassive(passive, c);
            }
        }

        private void ExecuteStartOfTurnPassive(PassiveAbility passive, Character c)
        {
            Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{c.CharacterName}' at start of turn.");
            if (passive == null || passive.AbilityType != PassiveAbility.PassiveAbilityType.StartOfTurn) return;

            Debug.Log($"[TurnManager] Executing passive ability '{passive.Name}' for character '{c.CharacterName}'.");
            passive.Execute(new PassiveOnTurnContext(character: c, characterLevel: c.Level, characterTile: c.CharacterCurrentTile, allTiles: _tiles, gridSize: _gridSize));
        }

        // ------------------------------------------------------------------ //
        // Unity update                                                        //
        // ------------------------------------------------------------------ //

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Dev-only cheat, stripped from release builds by the compiler directive above.
            // Works in any phase (even mid-enemy-turn) so it can bail out a fight instantly.
            if (!_fightEnded && Input.GetKeyDown(KeyCode.F1))
                DevDamageAllEnemies(100);
#endif

            if (IsPlayerInputBlocked()) return;

            UpdateHoveredTile();
            if (IsAimingAreaSpell()) UpdateSpellPreview();

            HandleClick();
            HandleKeys();
        }

        private bool IsPlayerInputBlocked() =>
            CombatCornerMenu.IsBlockingCombatInput || _fightEnded ||
            CurrentPhase != TurnPhase.PlayerTurn || _isMoving ||
            (_inspectPanel != null && _inspectPanel.IsVisible);

        private bool IsAimingAreaSpell() =>
            _currentMode == InputMode.Spell &&
            _selectedSpell != null &&
            _selectedSpell.TargetType == SpellTargetType.Area;

        // ------------------------------------------------------------------ //
        // Hovered tile handling                                               //
        // ------------------------------------------------------------------ //

        private void UpdateHoveredTile()
        {
            // While the pointer is over a UI element (e.g. a party-frame status bar),
            // leave the hover target alone rather than immediately re-raycasting into
            // the 3D scene and clobbering it — PartyFrameHudController drives the same
            // hover state directly via SetHoveredTarget in that case (see PointerEnter/
            // PointerLeaveEvent handlers there).
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!TryGetTileUnderMouse(out MapTile tile))
            {
                _hoveredTile = null;
                UpdateHoveredTarget(null);
                return;
            }

            _hoveredTile = tile;
            UpdateHoveredTarget(tile.OccupyingNpc);
        }

        /// <summary>
        /// Public alternate entry point for UI-driven hover input (party-frame status
        /// bar hover) — drives the exact same OnTargetHighlighted/OnTargetCleared
        /// pipeline a 3D-tile hover already does, so CharacterHudController's right-side
        /// panel and preview logic need no changes to support it.
        /// </summary>
        public void SetHoveredTarget(Npc npc) => UpdateHoveredTarget(npc);

        /// <summary>Raises OnTargetHighlighted/OnTargetCleared only when the hovered Npc actually changes.</summary>
        private void UpdateHoveredTarget(Npc npc)
        {
            if (npc == _hoveredTarget) return;

            _hoveredTarget = npc;
            SetHoverHighlightTile(npc?.CharacterCurrentTile);
            if (npc != null) OnTargetHighlighted?.Invoke(npc);
            else              OnTargetCleared?.Invoke();
        }

        /// <summary>
        /// Moves the gold hover-highlight overlay (see MapTile.SetHoverHighlight) onto
        /// whichever tile the hovered/targeted unit currently occupies, clearing it off
        /// the previous one. Single choke point for both hover-state mutation sites
        /// (UpdateHoveredTarget, and CommitAttack's post-kill clear) so the overlay can
        /// never go stale on a tile whose occupant is no longer the hovered target.
        /// </summary>
        private void SetHoverHighlightTile(MapTile tile)
        {
            if (_hoverHighlightTile == tile) return;
            if (_hoverHighlightTile != null) _hoverHighlightTile.SetHoverHighlight(false);
            _hoverHighlightTile = tile;
            if (_hoverHighlightTile != null) _hoverHighlightTile.SetHoverHighlight(true);
        }

        // ------------------------------------------------------------------ //
        // Tile inspection (selection outline + info panel)                    //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Marks <paramref name="tile"/> as the tile currently selected for inspection —
        /// moves the cream selection outline (see MapTile.SetSelectionOutline) onto it and
        /// shows its terrain/unit info in the TileInfoPanelUI. Clicking the already-inspected
        /// tile again toggles inspection off instead. Independent of _selectedTile (the
        /// acting character's tile for movement) — inspecting a tile never affects, and is
        /// never affected by, character selection or the current input mode.
        /// </summary>
        private void SetInspectedTile(MapTile tile)
        {
            if (_inspectedTile == tile)
            {
                ClearInspectedTile();
                return;
            }

            if (_inspectedTile != null) _inspectedTile.SetSelectionOutline(false);
            _inspectedTile = tile;
            _inspectedTile.SetSelectionOutline(true);
            _tileInfoPanel?.Show(tile);
        }

        /// <summary>Clears the current tile inspection, if any (outline + info panel).</summary>
        private void ClearInspectedTile()
        {
            if (_inspectedTile == null) return;
            _inspectedTile.SetSelectionOutline(false);
            _inspectedTile = null;
            _tileInfoPanel?.Hide();
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

            HandleTileActivated(clicked);
        }

        /// <summary>
        /// Everything that happens once a MapTile has been "activated" — via an actual
        /// 3D-tile mouse click (HandleClick) or an equivalent alternate input, e.g.
        /// clicking a party-frame status bar (see ActivateCharacterFrame). Single entry
        /// point so both input paths get identical validation with zero duplicated logic.
        /// </summary>
        private void HandleTileActivated(MapTile clicked)
        {
            switch (_currentMode)
            {
                case InputMode.Move:   HandleMoveModeClick(clicked);       break;
                case InputMode.Attack: HandleAttackModeClick(clicked);     break;
                case InputMode.Spell:  HandleSpellModeClick(clicked);      break;
                default:               HandleInspectOrSelectClick(clicked); break;
            }
        }

        private void HandleMoveModeClick(MapTile clicked)
        {
            if (_highlightedMoveTiles.Contains(clicked))
                CommitMove(clicked);
            else
                SetMode(InputMode.None);
        }

        private void HandleAttackModeClick(MapTile clicked)
        {
            if (_highlightedAttackTiles.Contains(clicked) && clicked.OccupyingNpc != null)
                CommitAttack(clicked);
            else if (clicked.OccupyingNpc != null)
                // Real target, just beyond the attacker's reach — warn instead of
                // silently cancelling so the player doesn't lose their targeting mode.
                _warningToast?.Show("Target is out of range.");
            else
                SetMode(InputMode.None);
        }

        private void HandleSpellModeClick(MapTile clicked)
        {
            if (_spellTargetTiles.Contains(clicked))
                CommitSpell(clicked);
            else if (_selectedSpell != null &&
                     _selectedSpell.TargetType != SpellTargetType.Self &&
                     IsValidSpellTarget(_selectedSpell, clicked))
                // Same idea as the attack case above: a legal target that's simply
                // out of the spell's range shouldn't silently drop out of targeting.
                _warningToast?.Show($"'{_selectedSpell.Name}' is out of range.");
            else
                SetMode(InputMode.None);
        }

        private void HandleInspectOrSelectClick(MapTile clicked)
        {
            SetInspectedTile(clicked);
            if (clicked.OccupyingCharacter != null)
                SelectCharacter(clicked.OccupyingCharacter, clicked);
            else
                Deselect();
        }

        /// <summary>
        /// Public alternate entry point for UI-driven target/select input (party-frame
        /// status bar clicks). Resolves the character's current tile and runs it through
        /// the exact same validation as a real tile click. No-ops outside the player's
        /// turn, after the fight has ended, or if the character has no current tile.
        /// </summary>
        public void ActivateCharacterFrame(Character character)
        {
            if (character == null) return;
            if (_fightEnded || CurrentPhase != TurnPhase.PlayerTurn) return;

            MapTile tile = character.CharacterCurrentTile;
            if (tile == null) return;

            HandleTileActivated(tile);
        }

        /// <summary>
        /// Scans the grid for every Npc still alive. Read-only alternative to exposing
        /// the tile grid itself; mirrors the scan/HealthPoints filter CheckFightOutcome
        /// and RunEnemyTurns already do internally (left independent here to keep this
        /// addition purely additive).
        /// </summary>
        public IEnumerable<Npc> GetLivingEnemies()
        {
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                {
                    MapTile tile = _tiles[x, z];
                    if (tile == null) continue;
                    Npc npc = tile.OccupyingNpc;
                    if (npc != null && npc.CharacterStats.HealthPoints > 0)
                        yield return npc;
                }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>F1 dev cheat: deals <paramref name="amount"/> damage to every living
        /// enemy on the grid — a quick way to finish or skip past a fight while testing.
        /// Mirrors the damage/defeat handling in ResolveBasicAttack (TakeDamage, floating
        /// text, RemoveUnit on death) before checking whether that just won the fight.</summary>
        private void DevDamageAllEnemies(int amount)
        {
            for (int x = 0; x < _gridSize; x++)
            {
                for (int z = 0; z < _gridSize; z++)
                {
                    MapTile tile = _tiles[x, z];
                    if (tile == null) continue;
                    Npc npc = tile.OccupyingNpc;
                    if (npc == null || npc.CharacterStats.HealthPoints <= 0) continue;

                    int dealt = npc.TakeDamage(amount);
                    FloatingCombatTextSystem.Instance?.ShowDamage(npc, dealt, SpellType.Physical);
                    Debug.Log($"[TurnManager] [DEV] Dealt {dealt} damage to '{npc.CharacterName}'.");

                    if (npc.CharacterStats.HealthPoints <= 0)
                    {
                        Debug.Log($"[TurnManager] [DEV] '{npc.CharacterName}' has been defeated!");
                        tile.RemoveUnit();
                    }
                }
            }

            CheckFightOutcome();
        }
#endif

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
            if (Input.GetKeyDown(KeyCode.Escape)) { HandleEscapeKey(); return; }
            if (Input.GetKeyDown(KeyCode.E))      { HandleEndTurnKey(); return; }
            if (_selectedCharacter == null) return;

            HandleActionModeKeys();
        }

        private void HandleEscapeKey()
        {
            SetMode(InputMode.None);
            ShowActionPanel();
            ClearInspectedTile();
        }

        private void HandleEndTurnKey()
        {
            if (_selectedCharacter != null) EndCharacterTurn(_selectedCharacter);
        }

        private void HandleActionModeKeys()
        {
            if (Input.GetKeyDown(KeyCode.W))
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
            _zocOutlines.Clear();
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
            HashSet<MapTile> reachable = _pathfinder.BfsReachable(origin, range, _selectedCharacter);
            HighlightMoveTiles(reachable, origin);
            _zocOutlines.DrawForEnemiesOf(_selectedCharacter, _tiles);
        }

        private void HighlightMoveTiles(HashSet<MapTile> reachable, MapTile origin)
        {
            foreach (MapTile tile in reachable)
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
            List<MapTile> path = _pathfinder.FindShortestPath(_selectedTile, destination, _selectedCharacter);
            int manaCost = _pathfinder.PathCost(path, _selectedCharacter);
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

            UpdateTargetHudAfterAttack(target);
            FinishPlayerAction();
        }

        private void UpdateTargetHudAfterAttack(Npc target)
        {
            if (target.CharacterStats.HealthPoints <= 0)
            {
                OnTargetCleared?.Invoke();
                _hoveredTarget = null;
                SetHoverHighlightTile(null);
            }
            else
            {
                // Push the post-damage stats to the right-side HUD panel.
                OnTargetHighlighted?.Invoke(target);
            }
        }

        /// <summary>
        /// Ends the current player action: checks whether the fight just ended, then — if
        /// not — auto-ends the acting character's turn once their resources are spent, or
        /// else refreshes the HUD with their updated AP/HP/mana. Shared tail of every
        /// player-initiated action (attack, spell) that doesn't reposition the caster first.
        /// </summary>
        private void FinishPlayerAction()
        {
            CheckFightOutcome();
            if (_fightEnded) return;

            CheckAndHandleTurnEnd(_selectedCharacter);
            if (_selectedCharacter != null)
            {
                ShowActionPanel();
                OnCharacterSelected?.Invoke(_selectedCharacter);
            }
        }

        /// <summary>Attack range in Manhattan-distance tiles based on class specialty,
        /// plus any range bonus from the character's passives (e.g. Hans's Long Bow).</summary>
        private int GetAttackRange(Character character)
        {
            int baseRange = character.CharacterClass == null
                ? 1
                : BaseAttackRangeFor(character.CharacterClass.Specialty);
            return baseRange + character.RangeBonus;
        }

        private static int BaseAttackRangeFor(CharacterClass.ClassSpecialty specialty)
        {
            switch (specialty)
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
                ShowSelfSpellRange(origin, spell);
            else
                ShowAimedSpellRange(origin, spell);
        }

        /// <summary>No tile is aimed, but the spell still affects a fixed set of tiles around
        /// the caster (just the caster's own tile for a plain self-buff like Grit, or a wider
        /// CircleAround area for something like Rally) — highlight exactly that area so the
        /// player can see who/what will be affected before confirming.</summary>
        private void ShowSelfSpellRange(MapTile origin, Spell spell)
        {
            List<MapTile> affectedTiles = SpellAreaUtils.GetAreaTiles(spell, origin, origin, _tiles, _gridSize);
            Color color = GetSpellHighlightColor(spell);
            foreach (MapTile tile in affectedTiles)
            {
                tile.SetSpellHighlight(true, color);
                _spellTargetTiles.Add(tile);
            }
        }

        private void ShowAimedSpellRange(MapTile origin, Spell spell)
        {
            Color color = GetSpellHighlightColor(spell);
            int ox = origin.GridX, oz = origin.GridZ;

            // Self counts as an Ally, so Ally/Both spells (e.g. Heal) may target the
            // caster's own tile (dist 0); only Enemy spells require dist >= 1, since a
            // caster can never be its own enemy target.
            int minDist = spell.TargetType == SpellTargetType.Enemy ? 1 : 0;
            // Include the caster's passive range bonus (e.g. Hans's Long Bow) alongside
            // the spell's own range.
            int effectiveRange = spell.Range + (_selectedCharacter?.RangeBonus ?? 0);

            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                    HighlightIfInSpellRange(x, z, ox, oz, minDist, effectiveRange, color);
        }

        private void HighlightIfInSpellRange(int x, int z, int ox, int oz, int minDist, int effectiveRange, Color color)
        {
            int dist = Mathf.Abs(x - ox) + Mathf.Abs(z - oz);
            if (dist < minDist || dist > effectiveRange) return;

            MapTile tile = _tiles[x, z];
            tile.SetSpellHighlight(true, color);
            _spellTargetTiles.Add(tile);
        }

        private void UpdateSpellPreview()
        {
            ClearPreviewTiles();
            if (_hoveredTile == null) return;
            if (!IsHoveredTileInSpellRange()) return;

            HighlightSpellPreviewArea();
        }

        private bool IsHoveredTileInSpellRange()
        {
            int distance =
                Mathf.Abs(_hoveredTile.GridX - _selectedTile.GridX) +
                Mathf.Abs(_hoveredTile.GridZ - _selectedTile.GridZ);

            // Include the caster's passive range bonus (e.g. Hans's Long Bow), same as
            // ShowSpellRange, so the preview never rejects a tile the highlight allowed.
            int effectiveRange = _selectedSpell.Range + (_selectedCharacter?.RangeBonus ?? 0);
            return distance != 0 && distance <= effectiveRange;
        }

        /// <summary>
        /// Dispatches on the spell's own AreaType (HorizontalLine, Cone, ...) so the preview
        /// always matches the shape Execute will actually resolve against. Higher alpha than
        /// the selectable-range highlight (0.9 vs 0.75) so the exact tiles about to be hit
        /// stand out from the wider "you could aim here" range.
        /// </summary>
        private void HighlightSpellPreviewArea()
        {
            List<MapTile> areaTiles = SpellAreaUtils.GetAreaTiles(
                _selectedSpell, _selectedTile, _hoveredTile, _tiles, _gridSize);

            Color previewColor = GetSpellHighlightColor(_selectedSpell, alpha: 0.9f);
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

            if (!IsValidSpellTarget(spell, targetTile)) { WarnInvalidSpellTarget(spell); return; }
            SetMode(InputMode.None);   // clears highlights and nulls _selectedSpell
            if (!HasEnoughMana(_selectedCharacter, spell)) { WarnNotEnoughMana(_selectedCharacter, spell); return; }

            var context = BuildSpellContext(_selectedCharacter, _selectedTile, targetTile);
            if (TryStartSpellWithMovement(_selectedCharacter, spell, context)) return;

            FaceTowardTile(_selectedCharacter, targetTile);
            ResolveSpellCast(_selectedCharacter, spell, context);
            FinishPlayerAction();
        }

        /// <summary>Rejects tiles that don't hold a valid target for this spell's TargetType
        /// (e.g. an Enemy-targeted spell aimed at an empty or ally-occupied tile). Bail out
        /// *before* touching AP/mana/cooldown and stay in targeting mode so the player can
        /// simply pick a real target instead of losing the turn.</summary>
        private void WarnInvalidSpellTarget(Spell spell)
        {
            Debug.Log($"[TurnManager] '{spell.Name}' has no valid target on that tile.");
            _warningToast?.Show(GetNoTargetMessage(spell));
        }

        private bool HasEnoughMana(Character caster, Spell spell) =>
            spell.ManaCost <= 0 || caster.CharacterStats.Mana >= spell.ManaCost;

        private void WarnNotEnoughMana(Character caster, Spell spell)
        {
            Debug.Log($"[TurnManager] '{caster.CharacterName}' does not have enough mana to cast this spell.");
            _warningToast?.Show($"Not enough mana to cast '{spell.Name}'.");
        }

        private SpellContext BuildSpellContext(Character caster, MapTile casterTile, MapTile targetTile) =>
            new SpellContext(
                caster:      caster,
                casterLevel: caster.Level,
                casterTile:  casterTile,
                targetTile:  targetTile,
                allTiles:    _tiles,
                gridSize:    _gridSize);

        /// <summary>If the spell physically repositions the caster first (e.g. Charge), starts
        /// the movement+resolve coroutine and reports true so the caller stops here instead of
        /// resolving the spell immediately in place.</summary>
        private bool TryStartSpellWithMovement(Character caster, Spell spell, SpellContext context)
        {
            MapTile landingTile = spell.GetCasterLandingTile(context);
            if (landingTile == null) return false;

            StartCoroutine(SpellWithMovement(caster, _selectedTile, landingTile, spell, context));
            return true;
        }

        /// <summary>
        /// Executes <paramref name="spell"/> for <paramref name="caster"/> against an
        /// already-built <paramref name="context"/>, spending the spell's action-point cost
        /// regardless of outcome (the attempt was made — free spells like Evasion/Throwing
        /// Knife have ActionPointCost 0), and only deducting mana / starting its cooldown /
        /// breaking invisibility on a successful cast.
        /// </summary>
        private void ResolveSpellCast(Character caster, Spell spell, SpellContext context)
        {
            bool resolved = spell.Execute(context);
            _remainingActions[caster] =
                Mathf.Max(0, RemainingActions(caster) - spell.ActionPointCost);

            if (resolved) ApplySuccessfulCastEffects(caster, spell);
        }

        /// <summary>Deducts mana and starts the cooldown for a spell that just resolved
        /// successfully, then breaks the caster's invisibility (same as any other offensive
        /// action).</summary>
        private void ApplySuccessfulCastEffects(Character caster, Spell spell)
        {
            caster.CharacterStats.Mana = Mathf.Max(0, caster.CharacterStats.Mana - spell.ManaCost);
            StartSpellCooldown(caster, spell);
            caster.BreakInvisibility();
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
            yield return StartCoroutine(MoveCasterToLandingTile(caster, fromTile, landingTile));
            _isMoving = false;

            // Caster is now in position — resolve the spell's damage / effect.
            FaceTowardTile(caster, context.TargetTile);
            ResolveSpellCast(caster, spell, context);

            CheckFightOutcome();
            if (_fightEnded) yield break;

            CheckAndHandleTurnEnd(caster);
            if (_selectedCharacter != null)
                ShowActionPanel();
        }

        private IEnumerator MoveCasterToLandingTile(Character caster, MapTile fromTile, MapTile landingTile)
        {
            List<MapTile> path = _pathfinder.FindShortestPath(fromTile, landingTile, caster);
            yield return StartCoroutine(MoveUnitAlongPath(caster, fromTile, path, finalTile =>
            {
                _selectedTile = finalTile;
            }));
        }

        /// <summary>Highlight tint for <paramref name="spell"/>, at <paramref name="alpha"/>.
        /// Prefers <see cref="Spell.GetHighlightType"/> (the caster-aware hook, so a
        /// randomised-type spell like Magic Dart can highlight its resolved type) — reusing
        /// <see cref="CombatTextColors.ForDamageType"/> so a spell's highlight always matches
        /// the colour its damage numbers use (Physical = orange, Magical = light purple,
        /// Buff = green, ...). Falls back to a TargetType-based colour for spells with no
        /// established type at all.</summary>
        private Color GetSpellHighlightColor(Spell spell, float alpha = 0.75f)
        {
            SpellType? type = spell.GetHighlightType(_selectedCharacter);
            if (type.HasValue)
            {
                Color c = CombatTextColors.ForDamageType(type.Value);
                return new Color(c.r, c.g, c.b, alpha);
            }

            return FallbackHighlightColor(spell.TargetType, alpha);
        }

        private static Color FallbackHighlightColor(SpellTargetType targetType, float alpha)
        {
            switch (targetType)
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

            if (!CanCastSpell(spell)) return;
            EnterSpellTargetingMode(spell);
        }

        /// <summary>Runs every affordability/legality check for casting <paramref name="spell"/>
        /// — silence, action points, mana, cooldown — warning the player and stopping at the
        /// first one that fails.</summary>
        private bool CanCastSpell(Spell spell)
        {
            if (_selectedCharacter.IsSilenced) { WarnSilenced(); return false; }
            if (RemainingActions(_selectedCharacter) < spell.ActionPointCost) { WarnNoActionPointsForSpell(); return false; }
            if (!HasEnoughMana(_selectedCharacter, spell)) { WarnNotEnoughManaToActivate(spell); return false; }
            if (!IsSpellAvailable(_selectedCharacter, spell)) { WarnSpellOnCooldown(spell); return false; }
            return true;
        }

        private void WarnSilenced()
        {
            Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' is silenced and cannot cast spells.");
            _warningToast?.Show($"'{_selectedCharacter.CharacterName}' is silenced and cannot cast spells.");
        }

        private void WarnNoActionPointsForSpell()
        {
            Debug.Log($"[TurnManager] '{_selectedCharacter.CharacterName}' has no actions remaining.");
            _warningToast?.Show($"'{_selectedCharacter.CharacterName}' has no action points left.");
        }

        private void WarnNotEnoughManaToActivate(Spell spell)
        {
            Debug.Log($"[TurnManager] Not enough mana to cast '{spell.Name}'.");
            _warningToast?.Show($"Not enough mana to cast '{spell.Name}'.");
        }

        private void WarnSpellOnCooldown(Spell spell)
        {
            int cooldownRemaining = GetCooldownRemaining(_selectedCharacter, spell);
            string message = cooldownRemaining == int.MaxValue
                ? $"'{spell.Name}' has already been used this fight."
                : $"'{spell.Name}' is on cooldown ({cooldownRemaining} round(s) left).";

            Debug.Log($"[TurnManager] {message}");
            _warningToast?.Show(message);
        }

        /// <summary>Enters targeting mode — hides panels and shows the spell's range/area
        /// highlights. Self-targeting spells with no aim still get a highlight (see
        /// <see cref="ShowSpellRange"/>); the click that confirms them lands on CommitSpell
        /// exactly like any other spell.</summary>
        private void EnterSpellTargetingMode(Spell spell)
        {
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
                foreach (var passive in c.CharacterPassiveAbilities)
                    ExecuteEndOfTurnPassive(passive, c);
        }

        private void ExecuteEndOfTurnPassive(PassiveAbility passive, Character c)
        {
            Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{c.CharacterName}' at end of turn.");
            if (passive == null || passive.AbilityType != PassiveAbility.PassiveAbilityType.EndOfTurn) return;

            Debug.Log($"[TurnManager] Executing passive ability '{passive.Name}' for character '{c.CharacterName}'.");
            passive.Execute(new PassiveOnTurnContext(character: c, characterLevel: c.Level, characterTile: c.CharacterCurrentTile, allTiles: _tiles, gridSize: _gridSize));
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

        /// <summary>Called by the party frame HUD's End Turn button. Ends the player's whole
        /// turn immediately if no living ally has an action point left; otherwise shows a
        /// Yes/No confirm toast naming who still does, only ending the turn if the player
        /// confirms. No-ops outside the player's own turn.</summary>
        public void RequestEndPlayerTurn()
        {
            Debug.Log($"[TurnManager] RequestEndPlayerTurn called. CurrentPhase={CurrentPhase}");
            if (CurrentPhase != TurnPhase.PlayerTurn) return;

            List<string> stillActing = AlliesWithActionPointsRemaining();
            if (stillActing.Count == 0)
            {
                ForceEndPlayerTurn();
                return;
            }

            string names = string.Join(", ", stillActing);
            string verb  = stillActing.Count == 1 ? "has" : "have";
            string message = $"{names} still {verb} an action point remaining. " +
                              "Do you still want to end your turn?";
            Debug.Log($"[TurnManager] Showing End Turn confirm: {message}");
            _endTurnConfirm?.Show(message, ForceEndPlayerTurn);
        }

        /// <summary>Names of living allies who still have an unspent action point this turn —
        /// i.e. haven't clicked Wait and haven't otherwise exhausted their actions.</summary>
        private List<string> AlliesWithActionPointsRemaining()
        {
            var names = new List<string>();
            List<Character> members = PartyManager.Instance?.Party?.Members;
            if (members == null) return names;

            foreach (Character c in members)
                if (c.CharacterStats.HealthPoints > 0 && RemainingActions(c) > 0)
                    names.Add(c.CharacterName);
            return names;
        }

        /// <summary>Immediately ends the whole player turn — marks every living ally finished
        /// (same bookkeeping EndCharacterTurn applies per-character) regardless of remaining
        /// movement/action points, then hands off to the enemy turn.</summary>
        private void ForceEndPlayerTurn()
        {
            FinishAllLivingAllies(PartyManager.Instance?.Party?.Members);

            Debug.Log("[TurnManager] Player turn ended early via End Turn button.");
            Deselect();
            CheckAllCharactersDone();
        }

        private void FinishAllLivingAllies(List<Character> members)
        {
            if (members == null) return;
            foreach (Character c in members)
            {
                if (c.CharacterStats.HealthPoints <= 0) continue;
                _finishedCharacters.Add(c);
                _remainingMovement[c] = 0;
                _remainingActions[c]  = 0;
            }
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
            bool anyAllyAlive = AnyAllyAlive(members);
            bool anyEnemyAlive = AnyEnemyAlive();
            if (anyAllyAlive && anyEnemyAlive) return;

            _fightEnded = true;
            Deselect();

            if (!anyAllyAlive)
                HandleGameOver();
            else
                HandleFightWon(members);
        }

        private static bool AnyAllyAlive(List<Character> members)
        {
            if (members == null) return false;
            foreach (Character c in members)
                if (c.CharacterStats.HealthPoints > 0) return true;
            return false;
        }

        private bool AnyEnemyAlive()
        {
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                {
                    Npc npc = _tiles[x, z]?.OccupyingNpc;
                    if (npc != null && npc.CharacterStats.HealthPoints > 0) return true;
                }
            return false;
        }

        private void HandleGameOver()
        {
            Debug.Log("[TurnManager] All allies defeated. Game over.");
            _fightResultUI?.ShowGameOver();
        }

        private void HandleFightWon(List<Character> members)
        {
            Debug.Log("[TurnManager] All enemies defeated. Fight won!");
            int depth = PartyManager.Instance.Depth;
            // Falls back to a Normal-tier reward shape when testing MapScene standalone,
            // outside the Act Map flow (no PendingEncounter set) — mirrors the RealmType
            // override pattern in MapGenerator.Start().
            EncounterPoolTier tier = PartyManager.Instance.ActRun?.PendingEncounter?.Tier ?? EncounterPoolTier.Normal2;

            List<LootEntry> loot = FightRewardGenerator.Generate(depth, tier);
            GrantVictoryExperience(members, FightRewardGenerator.GetExperience(tier));

            PartyManager.Instance.AdvanceDepth();
            _fightResultUI?.ShowFightWon(loot);
        }

        private static void GrantVictoryExperience(List<Character> members, int xp)
        {
            foreach (Character member in members)
                if (member.CharacterStats.HealthPoints > 0)
                    member.GrantExperience(xp);
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
            var npcTiles = CollectLivingNpcTiles();
            if (npcTiles.Count == 0)
            {
                Debug.Log("[TurnManager] No Npcs remain.  Starting player turn.");
                StartPlayerTurn();
                yield break;
            }

            foreach (var (npc, _) in npcTiles)
            {
                yield return StartCoroutine(RunSingleNpcTurn(npc));
                if (_fightEnded) yield break;
            }

            Debug.Log("[TurnManager] Enemy turn complete.  Starting player turn.");
            TickAllNpcStatusEffects(npcTiles);

            CheckFightOutcome();
            if (_fightEnded) yield break;

            StartPlayerTurn();
        }

        /// <summary>Snapshot of every occupied Npc tile before acting — Npcs that die
        /// mid-turn are simply skipped when their turn comes up.</summary>
        private List<(Npc npc, MapTile tile)> CollectLivingNpcTiles()
        {
            var npcTiles = new List<(Npc npc, MapTile tile)>();
            for (int x = 0; x < _gridSize; x++)
                for (int z = 0; z < _gridSize; z++)
                {
                    MapTile t = _tiles[x, z];
                    if (t.OccupyingNpc != null)
                        npcTiles.Add((t.OccupyingNpc, t));
                }
            return npcTiles;
        }

        private IEnumerator RunSingleNpcTurn(Npc npc)
        {
            if (npc.CharacterStats.HealthPoints <= 0) yield break;
            if (npc.AI == null) yield break;

            // Re-locate the Npc's current tile (it may have moved earlier this round).
            MapTile currentTile = FindNpcTile(npc);
            if (currentTile == null) yield break;

            ApplyTerrainEffects(npc, currentTile);
            NpcTurnContext context = BuildNpcTurnContext(npc, currentTile);

            yield return StartCoroutine(npc.AI.ExecuteTurn(context));

            CheckFightOutcome();
            if (_fightEnded) yield break;

            // Small pause between Npc turns so the player can follow the action.
            yield return new WaitForSeconds(0.25f);
        }

        private NpcTurnContext BuildNpcTurnContext(Npc npc, MapTile currentTile)
        {
            var context = new NpcTurnContext(
                npc:               npc,
                currentTile:       currentTile,
                allTiles:          _tiles,
                gridSize:          _gridSize,
                remainingMovement: npc.IsRooted ? 0 : npc.CharacterStats.Movement,
                remainingActions:  npc.CharacterStats.MaxActionPoints);

            context.BfsReachable       = (origin, range) => _pathfinder.BfsReachable(origin, range, npc);
            context.FindPath           = (origin, dest)  => _pathfinder.FindShortestPath(origin, dest, npc);
            context.PathCost           = path            => _pathfinder.PathCost(path, npc);
            context.TrimPathToMovement = (path, budget)  => _pathfinder.TrimPathToMovement(path, budget, npc);
            context.AnimateNpcMove = path => AnimateNpcMoveCoroutine(npc, context, path);
            context.ExecuteAttack  = targetTile => NpcExecuteAttack(npc, targetTile);
            context.ExecuteSpell   = (spell, targetTile)
                => NpcExecuteSpell(npc, context.CurrentTile, spell, targetTile);
            return context;
        }

        private void TickAllNpcStatusEffects(List<(Npc npc, MapTile tile)> npcTiles)
        {
            foreach (var (npc, _) in npcTiles)
            {
                if (npc.CharacterStats.HealthPoints <= 0) continue;
                npc.TickStatusEffects();
            }
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

            Vector2Int dir;
            if (Mathf.Abs(dx) >= Mathf.Abs(dz))
                dir = new Vector2Int(Math.Sign(dx), 0);
            else
                dir = new Vector2Int(0, Math.Sign(dz));

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

            var (damage, damageType, bypassesDodge) = ComputeAttackDamage(attacker);
            int dealt = bypassesDodge ? target.TakeDamage(damage) : target.TakePhysicalDamage(damage);
            FloatingCombatTextSystem.Instance?.ShowDamage(target, dealt, damageType);
            LogAttackResult(attacker, target, damage, dealt);

            CheckAndHandleAllyDamagedPassives(target, targetTile);
            HandleDefeatIfDead(target, targetTile);
        }

        /// <summary>Computes the attacker's basic-attack damage/type and whether the hit
        /// bypasses Dodge (a physical-evasion mechanic) — true for magic-specialty attacks
        /// and empowered hits, both of which are routed through TakeDamage instead of
        /// TakePhysicalDamage, same as every other magic-typed spell in the game.</summary>
        private static (int damage, SpellType damageType, bool bypassesDodge) ComputeAttackDamage(Character attacker)
        {
            int damage = Mathf.Max(0, attacker.CharacterStats.TotalAttack);
            damage = ApplyAimMultiplier(attacker, damage);

            bool isMagic = IsMagicSpecialty(attacker);
            SpellType damageType = isMagic ? SpellType.Magical : SpellType.Physical;
            bool empowered = ApplyEmpower(attacker, ref damage, ref damageType);

            return (damage, damageType, isMagic || empowered);
        }

        private static bool IsMagicSpecialty(Character attacker) =>
            attacker.CharacterClass != null &&
            attacker.CharacterClass.Specialty == CharacterClass.ClassSpecialty.Magic;

        /// <summary>Aim (Archer) multiplies the damage of the attacker's next basic attack —
        /// consumed here the instant it's used, same pattern as Dodge/Bubble Shield.</summary>
        private static int ApplyAimMultiplier(Character attacker, int damage)
        {
            int aimMultiplier = attacker.TryConsumeAim();
            if (aimMultiplier == 1) return damage;

            Debug.Log($"[TurnManager] '{attacker.CharacterName}' consumes Aim, multiplying attack damage by {aimMultiplier}.");
            return damage * aimMultiplier;
        }

        /// <summary>Empower (Mystic) multiplies the attacker's next basic attack and swaps its
        /// damage type for a pre-rolled element — consumed here the instant it's used, same
        /// pattern as Aim. Returns true if an Empower was consumed.</summary>
        private static bool ApplyEmpower(Character attacker, ref int damage, ref SpellType damageType)
        {
            EmpowerStatus empower = attacker.TryConsumeEmpower();
            if (empower == null) return false;

            damage *= empower.Multiplier;
            damageType = empower.DamageType;
            Debug.Log($"[TurnManager] '{attacker.CharacterName}' consumes Empower, multiplying attack damage by {empower.Multiplier} and dealing {empower.DamageType} damage.");
            return true;
        }

        private static void LogAttackResult(Character attacker, Character target, int damage, int dealt)
        {
            Debug.Log($"[TurnManager] '{attacker.CharacterName}' attacks '{target.CharacterName}' "
                    + $"for {damage} damage ({dealt} after shield).  "
                    + $"HP: {Mathf.Max(0, target.CharacterStats.HealthPoints)}"
                    + $"/{target.CharacterStats.MaxHealthPoints}");
        }

        private static void HandleDefeatIfDead(Character target, MapTile targetTile)
        {
            if (target.CharacterStats.HealthPoints > 0) return;
            Debug.Log($"[TurnManager] '{target.CharacterName}' has been defeated!");
            targetTile.RemoveUnit();
        }

        private void CheckAndHandleReceivingBasicAttackDamage(Character attacker, Character target)
        {
            foreach (var passive in target.CharacterPassiveAbilities)
                ExecuteOnReceivingAttackPassive(passive, attacker, target);
        }

        private void ExecuteOnReceivingAttackPassive(PassiveAbility passive, Character attacker, Character target)
        {
            if (passive == null || passive.AbilityType != PassiveAbility.PassiveAbilityType.OnReceivingAttack) return;

            Debug.Log($"[TurnManager] Checking passive ability '{passive.Name}' for character '{target.CharacterName}' on receiving damage.");
            passive.Execute(new PassiveOnReceivingAttackContext(
                attacker: attacker,
                receivingCharacter: target,
                attackerTile: attacker.CharacterCurrentTile,
                receivingCharacterTile: target.CharacterCurrentTile,
                allTiles: _tiles,
                gridSize: _gridSize));
        }

        /// <summary>
        /// After basic-attack damage lands on <paramref name="damagedAlly"/>, lets every
        /// unit on their side (including themselves — Self is an Ally) react via an
        /// OnAllyDamaged passive (e.g. Buggles' Spiritual Protector) — each passive is
        /// responsible for its own range/health-threshold/once-per-combat checks, same as
        /// every other passive owning its own effect logic. Runs even if the hit was
        /// lethal, since this is called before the defeat check below, giving a reactive
        /// heal a chance to save them first.
        /// </summary>
        private void CheckAndHandleAllyDamagedPassives(Character damagedAlly, MapTile damagedAllyTile)
        {
            if (damagedAlly == null || damagedAllyTile == null) return;

            // No early-return on damagedAlly's HP here, even if this hit was lethal (<= 0) —
            // this runs before the defeat check/removal below, so a reactive heal (e.g.
            // Spiritual Protector saving a unit one-shot from over 50%) still has a chance
            // to bring them back above 0 before they're finalised as defeated.
            foreach (Character owner in CollectAlliesOf(damagedAlly))
                ExecuteAllyDamagedPassivesFor(owner, damagedAlly, damagedAllyTile);
        }

        private List<Character> CollectAlliesOf(Character damagedAlly)
        {
            var allies = new List<Character>();
            if (damagedAlly is Npc)
            {
                for (int x = 0; x < _gridSize; x++)
                    for (int z = 0; z < _gridSize; z++)
                    {
                        Npc npc = _tiles[x, z]?.OccupyingNpc;
                        if (npc != null) allies.Add(npc);
                    }
            }
            else
            {
                var members = PartyManager.Instance?.Party?.Members;
                if (members != null) allies.AddRange(members);
            }
            return allies;
        }

        /// <summary>Self counts as an Ally — a character can trigger its own OnAllyDamaged
        /// passive (e.g. Buggles' Spiritual Protector healing himself, potentially saving
        /// himself from a lethal hit). Self is exempt from the "must be alive" check for the
        /// same reason damagedAlly is above: when owner is the one who was just one-shot,
        /// their HP is already &lt;= 0 pending the passive's chance to heal them back. Other
        /// allies must still be alive to react.</summary>
        private void ExecuteAllyDamagedPassivesFor(Character owner, Character damagedAlly, MapTile damagedAllyTile)
        {
            bool isSelf = owner == damagedAlly;
            if ((!isSelf && owner.CharacterStats.HealthPoints <= 0) || owner.CharacterCurrentTile == null)
                return;

            foreach (var passive in owner.CharacterPassiveAbilities)
                ExecuteOnAllyDamagedPassive(passive, owner, damagedAlly, damagedAllyTile);
        }

        private void ExecuteOnAllyDamagedPassive(PassiveAbility passive, Character owner,
            Character damagedAlly, MapTile damagedAllyTile)
        {
            if (passive == null || passive.AbilityType != PassiveAbility.PassiveAbilityType.OnAllyDamaged) return;

            passive.Execute(new PassiveOnAllyDamagedContext(
                owner: owner,
                ownerTile: owner.CharacterCurrentTile,
                damagedAlly: damagedAlly,
                damagedAllyTile: damagedAllyTile,
                allTiles: _tiles,
                gridSize: _gridSize));
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
            if (!CanNpcCastSpell(caster, spell, targetTile)) return false;

            var context = BuildSpellContext(caster, casterTile, targetTile);
            FaceTowardTile(caster, targetTile);
            bool resolved = spell.Execute(context);
            if (resolved) ApplySuccessfulCastEffects(caster, spell);

            CheckFightOutcome();
            return resolved;
        }

        /// <summary>Defense-in-depth: AggressiveNpcAI already excludes silenced casters,
        /// insufficient mana, spells on cooldown, and Invisible Enemy targets when choosing a
        /// spell/target, but enforcing it here too covers any other AI type that doesn't.</summary>
        private bool CanNpcCastSpell(Npc caster, Spell spell, MapTile targetTile)
        {
            if (caster.IsSilenced) return false;
            if (spell.ManaCost > 0 && caster.CharacterStats.Mana < spell.ManaCost) return false;
            if (!IsSpellAvailable(caster, spell)) return false;

            Character targetCharacter = targetTile?.OccupyingCharacter;
            if (spell.TargetType == SpellTargetType.Enemy && targetCharacter != null &&
                targetCharacter.IsInvisible && !caster.IgnoresInvisibility)
                return false;

            return true;
        }

        /// <summary>Applies <paramref name="tile"/>'s terrain effects (see TerrainInfoRegistry)
        /// to <paramref name="c"/> at the start of its turn — currently just RegenPerTurn, a
        /// direct heal rather than a RegenStatus (re-applying that status every turn via
        /// ApplyStatus would stack its heal-per-turn amount forever, see RegenStatus.Stack,
        /// which is wrong for an ambient per-tile effect that shouldn't compound the longer
        /// you stand still). Single entry point for whatever other per-tile effects
        /// TerrainInfo grows later (damage, buffs, etc.) so callers don't need updating.</summary>
        private void ApplyTerrainEffects(Character c, MapTile tile)
        {
            if (tile == null) return;
            int regen = TerrainInfoRegistry.Get(tile.TerrainType).RegenPerTurn;
            if (regen <= 0) return;
            c.CharacterStats.HealthPoints = Mathf.Min(
                c.CharacterStats.HealthPoints + regen, c.CharacterStats.MaxHealthPoints);
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
                yield return StartCoroutine(MoveOneStep(unit, model, spriteView, currentTile, path[i]));
                currentTile = path[i];
            }

            onArrived?.Invoke(currentTile);
        }

        /// <summary>Faces <paramref name="unit"/> toward <paramref name="to"/>, lerps its
        /// model there over one tile-step's worth of time, then transfers tile occupancy.</summary>
        private IEnumerator MoveOneStep(Character unit, GameObject model, SpriteCharacterView spriteView,
                                         MapTile from, MapTile to)
        {
            FaceStepDirection(unit, spriteView, from, to);

            Vector3 startPos = model.transform.position;
            Vector3 endPos = StepEndPosition(to);
            yield return LerpPosition(model, startPos, endPos, _step / MoveSpeed);

            from.ReleaseUnit();
            to.AssignUnit(unit, model);
        }

        private static void FaceStepDirection(Character unit, SpriteCharacterView spriteView, MapTile from, MapTile to)
        {
            Vector2Int dir = new Vector2Int(to.GridX - from.GridX, to.GridZ - from.GridZ);
            spriteView?.FaceGridDirection(dir.x, dir.y);
            unit.FacingDirection = dir;
        }

        private static Vector3 StepEndPosition(MapTile to)
        {
            float halfH = to.transform.lossyScale.y * 0.5f;
            return to.transform.position + Vector3.up * (halfH + 0.05f);
        }

        private static IEnumerator LerpPosition(GameObject model, Vector3 startPos, Vector3 endPos, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                model.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            model.transform.position = endPos; // Snap to exact position.
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