using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.GameController;
using BaaroForce.Map;

namespace BaaroForce.UI
{
    /// <summary>
    /// Persistent WoW-style party frames: one compact row per living ally (center-left)
    /// and one per living enemy (center-right), always visible during a fight — unlike
    /// <see cref="CharacterHudController"/>'s two panels, which only ever show the
    /// currently selected ally / currently hovered enemy.
    ///
    /// Clicking a row calls <see cref="TurnManager.ActivateCharacterFrame"/>, which
    /// resolves the character's current tile and runs it through the exact same
    /// validation a real 3D tile click would — this script never re-implements
    /// selection/targeting rules itself.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PartyFrameHudController : MonoBehaviour
    {
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private VisualTreeAsset _rowTemplate; // PartyFrameRow.uxml
        [SerializeField] private StyleSheet _styleSheet;       // CombatHud.uss

        private UIDocument _document;
        private VisualElement _allyColumn;
        private VisualElement _allyList;
        private VisualElement _enemyList;
        private Button _endTurnButton;

        private readonly Dictionary<Character, VisualElement> _allyRows = new Dictionary<Character, VisualElement>();
        private readonly Dictionary<Character, VisualElement> _enemyRows = new Dictionary<Character, VisualElement>();
        private readonly HashSet<Character> _scratchSet = new HashSet<Character>();
        private readonly List<Character> _scratchRemovals = new List<Character>();

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private Coroutine _hookRoutine;

        private void OnEnable()
        {
            BuildLists();
            _hookRoutine = StartCoroutine(HookTurnManager());
        }

        private void OnDisable()
        {
            if (_hookRoutine != null)
            {
                StopCoroutine(_hookRoutine);
                _hookRoutine = null;
            }
        }

        // MapGenerator creates TurnManager at runtime (via AddComponent in its own
        // Start), which runs after this component's OnEnable — so a one-shot lookup
        // here would race it and silently never find it. Poll a few frames instead,
        // mirroring CharacterHudController.HookTurnManager().
        private IEnumerator HookTurnManager()
        {
            if (_turnManager == null) _turnManager = FindAnyObjectByType<TurnManager>();

            int framesWaited = 0;
            while (_turnManager == null && framesWaited < 300)
            {
                yield return null;
                framesWaited++;
                _turnManager = FindAnyObjectByType<TurnManager>();
            }

            if (_turnManager == null)
                Debug.LogWarning("[PartyFrameHudController] No TurnManager found in scene.");
        }

        private void BuildLists()
        {
            VisualElement root = _document.rootVisualElement;
            if (_styleSheet != null && !root.styleSheets.Contains(_styleSheet))
                root.styleSheets.Add(_styleSheet);

            // Ally rows + the End Turn button share one vertically-centred column (rather than
            // the row list alone being absolutely positioned, like the enemy side) so the
            // button always trails the rows in normal flex flow — "below the party frames"
            // regardless of how many allies are currently alive.
            _allyColumn = new VisualElement();
            _allyColumn.AddToClassList("party-frame-ally-column");
            root.Add(_allyColumn);

            _allyList = new VisualElement();
            _allyList.AddToClassList("party-frame-ally-rows");
            _allyColumn.Add(_allyList);

            _endTurnButton = new Button(OnEndTurnClicked) { text = "End Turn" };
            _endTurnButton.AddToClassList("action-btn");
            _endTurnButton.AddToClassList("action-btn-endturn");
            _endTurnButton.AddToClassList("end-turn-btn");
            _allyColumn.Add(_endTurnButton);

            _enemyList = new VisualElement();
            _enemyList.AddToClassList("party-frame-list");
            _enemyList.AddToClassList("party-frame-list--enemy");
            root.Add(_enemyList);
        }

        private void OnEndTurnClicked()
        {
            Debug.Log("[PartyFrameHudController] End Turn button clicked.");
            _turnManager?.RequestEndPlayerTurn();
        }

        private void Update()
        {
            if (_allyList == null || _enemyList == null) return;

            IEnumerable<Character> allies = PartyManager.Instance?.Party?.Members
                ?.Where(c => c.CharacterStats.HealthPoints > 0) ?? Enumerable.Empty<Character>();
            RefreshSide(_allyList, _allyRows, allies);

            IEnumerable<Character> enemies = _turnManager != null
                ? _turnManager.GetLivingEnemies().Cast<Character>()
                : Enumerable.Empty<Character>();
            RefreshSide(_enemyList, _enemyRows, enemies);

            // Later-added HUD components (ActionPanelUI, CombatCornerMenu, ...) are appended
            // to root AFTER this component builds its lists (they're created by TurnManager,
            // which runs from MapGenerator.Start — after every scene component's OnEnable),
            // so they paint over this column by default UI Toolkit stacking order even where
            // they have no visible content. Re-assert this column on top every frame so the
            // End Turn button stays clickable — except while a real modal is open, so it
            // doesn't punch through something the player is actually supposed to interact with.
            if (_allyColumn != null && !CombatCornerMenu.IsBlockingCombatInput)
                _allyColumn.BringToFront();

            _endTurnButton?.SetEnabled(_turnManager != null && _turnManager.CurrentPhase == TurnPhase.PlayerTurn);
        }

        /// <summary>
        /// Adds/removes rows only when the living roster on this side actually changed,
        /// then refreshes HP/mana bars on every remaining row — same per-frame refresh
        /// cost model CharacterHudController already uses, just applied to N rows instead
        /// of one panel.
        /// </summary>
        private void RefreshSide(VisualElement container, Dictionary<Character, VisualElement> rows,
            IEnumerable<Character> living)
        {
            _scratchSet.Clear();
            foreach (Character c in living) _scratchSet.Add(c);

            _scratchRemovals.Clear();
            foreach (Character c in rows.Keys)
                if (!_scratchSet.Contains(c)) _scratchRemovals.Add(c);

            foreach (Character c in _scratchRemovals)
            {
                rows[c].RemoveFromHierarchy();
                rows.Remove(c);
            }

            foreach (Character c in _scratchSet)
            {
                if (rows.ContainsKey(c)) continue;
                VisualElement row = BuildRow(c);
                container.Add(row);
                rows[c] = row;
            }

            foreach (var kvp in rows)
                RefreshRowStats(kvp.Value, kvp.Key);
        }

        /// <summary>Instantiates a row, sets its one-time-only fields (portrait, name),
        /// and wires its click to <see cref="TurnManager.ActivateCharacterFrame"/>.</summary>
        private VisualElement BuildRow(Character character)
        {
            VisualElement row = _rowTemplate.Instantiate();
            VisualElement clickTarget = row.Q<VisualElement>("frame-root") ?? row;

            Character capturedCharacter = character; // fixed per-row capture
            clickTarget.RegisterCallback<ClickEvent>(_ => _turnManager?.ActivateCharacterFrame(capturedCharacter));

            // Hovering an enemy row previews it exactly like hovering the unit on the
            // tile — same OnTargetHighlighted/OnTargetCleared pipeline CharacterHudController
            // already consumes for the right-side panel, just driven from a UI hover instead.
            if (character is Npc npc)
            {
                clickTarget.RegisterCallback<PointerEnterEvent>(_ => _turnManager?.SetHoveredTarget(npc));
                clickTarget.RegisterCallback<PointerLeaveEvent>(_ => _turnManager?.SetHoveredTarget(null));
            }

            row.Q<Label>("unit-name").text = character.CharacterName;
            row.Q<Label>("level-badge").text = character.Level.ToString();

            VisualElement portrait = row.Q<VisualElement>("portrait");
            if (portrait != null && !string.IsNullOrEmpty(character.CharacterProfilePicPath))
            {
                Sprite sprite = Resources.Load<Sprite>(character.CharacterProfilePicPath);
                if (sprite != null) portrait.style.backgroundImage = new StyleBackground(sprite);
            }

            return row;
        }

        private static void RefreshRowStats(VisualElement row, Character character)
        {
            CharacterStats stats = character.CharacterStats;

            int hp = Mathf.Max(0, stats.HealthPoints);
            int maxHp = Mathf.Max(1, stats.MaxHealthPoints);
            SetSegmentedBar(row.Q<VisualElement>("hp-bar"), hp, maxHp, "seg-fill-hp");

            int mana = Mathf.Max(0, stats.Mana);
            int maxMana = Mathf.Max(1, stats.MaxMana);
            SetSegmentedBar(row.Q<VisualElement>("mana-bar"), mana, maxMana, "seg-fill-mana");
        }

        /// <summary>Rebuilds a segmented bar (children = individual segment divs) to show filled/total.
        /// Mirrors CharacterHudController.SetSegmentedBar.</summary>
        private static void SetSegmentedBar(VisualElement bar, int filled, int total, string fillClass)
        {
            if (bar == null) return;
            bar.Clear();
            total = Mathf.Max(total, 1);
            for (int i = 0; i < total; i++)
            {
                var seg = new VisualElement();
                seg.AddToClassList("seg");
                if (i < filled) seg.AddToClassList(fillClass);
                bar.Add(seg);
            }
        }
    }
}
