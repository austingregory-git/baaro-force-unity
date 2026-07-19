using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Serialization;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Map;

namespace BaaroForce.UI
{
    /// <summary>
    /// Drives the two Combat HUD panels (the "cute-but-mysterious" unit readout —
    /// portrait, name/level, HP, mana, movement, attack) that sit above the map:
    ///   • Left panel  = the currently selected / current-turn character.
    ///   • Right panel = whatever enemy is currently being hovered or attacked.
    ///
    /// This script is intentionally the ONLY thing that knows about TurnManager's
    /// events — TurnManager itself stays UI-agnostic (see OnCharacterSelected /
    /// OnTargetHighlighted in TurnManager.cs).
    ///
    /// Attach this to the same GameObject as TurnManager (or anywhere in the scene)
    /// and assign a UIDocument that points at CombatHud.uxml / CombatHud.uss.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CharacterHudController : MonoBehaviour
    {
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private VisualTreeAsset _panelTemplate; // CombatHudPanel.uxml

        private UIDocument _document;
        private VisualElement _selectedPanel;   // left
        private VisualElement _targetPanel;      // right

        // All possible zone / weapon-type USS classes, so we can strip them cleanly
        // before applying the new one each time a panel is repopulated.
        private static readonly string[] ZoneClasses =
        {
            "zone-fire", "zone-water", "zone-earth", "zone-wind", "zone-light", "zone-dark"
        };
        private static readonly string[] WeaponClasses = { "weapon-melee", "weapon-ranged", "weapon-magic" };

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            BuildPanels();

            if (_turnManager == null) _turnManager = FindAnyObjectByType<TurnManager>();
            if (_turnManager == null)
            {
                Debug.LogWarning("[CharacterHudController] No TurnManager found in scene.");
                return;
            }

            _turnManager.OnCharacterSelected   += ShowSelected;
            _turnManager.OnCharacterDeselected += HideSelected;
            _turnManager.OnTargetHighlighted   += ShowTarget;
            _turnManager.OnTargetCleared       += HideTarget;
        }

        private void OnDisable()
        {
            if (_turnManager == null) return;
            _turnManager.OnCharacterSelected   -= ShowSelected;
            _turnManager.OnCharacterDeselected -= HideSelected;
            _turnManager.OnTargetHighlighted   -= ShowTarget;
            _turnManager.OnTargetCleared       -= HideTarget;
        }

        // ------------------------------------------------------------------ //
        // Panel construction — one instance of CombatHudPanel.uxml per side.  //
        // ------------------------------------------------------------------ //

        private void BuildPanels()
        {
            VisualElement root = _document.rootVisualElement;

            _selectedPanel = _panelTemplate.Instantiate();
            _selectedPanel.AddToClassList("hud-panel");
            _selectedPanel.AddToClassList("hud-panel--left");
            _selectedPanel.style.display = DisplayStyle.None;
            root.Add(_selectedPanel);

            _targetPanel = _panelTemplate.Instantiate();
            _targetPanel.AddToClassList("hud-panel");
            _targetPanel.AddToClassList("hud-panel--right");
            _targetPanel.style.display = DisplayStyle.None;
            root.Add(_targetPanel);
        }

        // ------------------------------------------------------------------ //
        // Left panel — current-turn character                                 //
        // ------------------------------------------------------------------ //

        private void ShowSelected(Character character) => Populate(_selectedPanel, character);
        private void HideSelected() => _selectedPanel.style.display = DisplayStyle.None;

        // ------------------------------------------------------------------ //
        // Right panel — hovered / attacked target                             //
        // ------------------------------------------------------------------ //

        private void ShowTarget(Npc npc) => Populate(_targetPanel, npc);
        private void HideTarget() => _targetPanel.style.display = DisplayStyle.None;

        private void Populate(VisualElement panel, Character character)
        {
            if (character == null) { panel.style.display = DisplayStyle.None; return; }

            panel.style.display = DisplayStyle.Flex;

            var stats = character.CharacterStats;
            var cls   = character.CharacterClass;

            // --- name / level -------------------------------------------------
            panel.Q<Label>("unit-name").text = character.CharacterName;
            panel.Q<Label>("unit-level").text = $"Lv {character.Level}";

            // --- zone theme -----------------------------------------------------
            // TODO: point this at whatever field actually stores elemental zone on
            // CharacterClass (e.g. cls.element). Left as a safe fallback so this
            // compiles even before that field is wired up.
            string zoneId = ResolveZoneId(cls);
            ApplyExclusiveClass(panel, ZoneClasses, $"zone-{zoneId}");

            // --- HP ---------------------------------------------------------
            int hp = Mathf.Max(0, stats.HealthPoints);
            int maxHp = Mathf.Max(1, stats.MaxHealthPoints);
            SetSegmentedBar(panel.Q<VisualElement>("hp-bar"), hp, maxHp, "seg-fill-hp");
            panel.Q<Label>("hp-num").text = $"{hp}/{maxHp}";

            // --- Mana ---------------------------------------------------------
            int mana = Mathf.Max(0, stats.Mana);
            int maxMana = Mathf.Max(1, stats.MaxMana); // adjust field name if different
            SetSegmentedBar(panel.Q<VisualElement>("mana-bar"), mana, maxMana, "seg-fill-mana");
            panel.Q<Label>("mana-num").text = $"{mana}/{maxMana}";

            // --- Movement (pips) ------------------------------------------------
            int move = stats.Movement;
            SetPips(panel.Q<VisualElement>("move-pips"), move, move);
            panel.Q<Label>("move-num").text = $"{move}";

            // --- Attack (icon swaps by weapon type) ------------------------------
            panel.Q<Label>("atk-num").text = stats.TotalAttack.ToString();
            string weaponClass = cls?.Specialty switch
            {
                CharacterClass.ClassSpecialty.Melee  => "weapon-melee",
                CharacterClass.ClassSpecialty.Ranged => "weapon-ranged",
                CharacterClass.ClassSpecialty.Magic  => "weapon-magic",
                _                                     => "weapon-melee",
            };
            ApplyExclusiveClass(panel.Q<VisualElement>("atk-badge"), WeaponClasses, weaponClass);
        }

        private static string ResolveZoneId(CharacterClass cls)
        {
            // Swap this out for the real field once it exists, e.g.:
            //   return cls.element.ToString().ToLowerInvariant();
            return "earth";
        }

        // ------------------------------------------------------------------ //
        // Small UI-building helpers                                           //
        // ------------------------------------------------------------------ //

        /// <summary>Rebuilds a segmented bar (children = individual segment divs) to show filled/total.</summary>
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

        /// <summary>Rebuilds the diamond movement pips to show filled/total.</summary>
        private static void SetPips(VisualElement pips, int filled, int total)
        {
            if (pips == null) return;
            pips.Clear();
            total = Mathf.Max(total, 1);
            for (int i = 0; i < total; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("pip");
                if (i < filled) pip.AddToClassList("pip-on");
                pips.Add(pip);
            }
        }

        private static void ApplyExclusiveClass(VisualElement el, string[] allClasses, string activeClass)
        {
            if (el == null) return;
            foreach (var c in allClasses) el.RemoveFromClassList(c);
            el.AddToClassList(activeClass);
        }
    }
}