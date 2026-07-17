using UnityEngine;
using UnityEngine.UIElements;
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
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private VisualTreeAsset panelTemplate; // CombatHudPanel.uxml

        private UIDocument document;
        private VisualElement selectedPanel;   // left
        private VisualElement targetPanel;      // right

        // All possible zone / weapon-type USS classes, so we can strip them cleanly
        // before applying the new one each time a panel is repopulated.
        private static readonly string[] ZoneClasses =
        {
            "zone-fire", "zone-water", "zone-earth", "zone-wind", "zone-light", "zone-dark"
        };
        private static readonly string[] WeaponClasses = { "weapon-melee", "weapon-ranged", "weapon-magic" };

        private void Awake()
        {
            document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            BuildPanels();

            if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
            if (turnManager == null)
            {
                Debug.LogWarning("[CharacterHudController] No TurnManager found in scene.");
                return;
            }

            turnManager.OnCharacterSelected   += ShowSelected;
            turnManager.OnCharacterDeselected += HideSelected;
            turnManager.OnTargetHighlighted   += ShowTarget;
            turnManager.OnTargetCleared       += HideTarget;
        }

        private void OnDisable()
        {
            if (turnManager == null) return;
            turnManager.OnCharacterSelected   -= ShowSelected;
            turnManager.OnCharacterDeselected -= HideSelected;
            turnManager.OnTargetHighlighted   -= ShowTarget;
            turnManager.OnTargetCleared       -= HideTarget;
        }

        // ------------------------------------------------------------------ //
        // Panel construction — one instance of CombatHudPanel.uxml per side.  //
        // ------------------------------------------------------------------ //

        private void BuildPanels()
        {
            VisualElement root = document.rootVisualElement;

            selectedPanel = panelTemplate.Instantiate();
            selectedPanel.AddToClassList("hud-panel");
            selectedPanel.AddToClassList("hud-panel--left");
            selectedPanel.style.display = DisplayStyle.None;
            root.Add(selectedPanel);

            targetPanel = panelTemplate.Instantiate();
            targetPanel.AddToClassList("hud-panel");
            targetPanel.AddToClassList("hud-panel--right");
            targetPanel.style.display = DisplayStyle.None;
            root.Add(targetPanel);
        }

        // ------------------------------------------------------------------ //
        // Left panel — current-turn character                                 //
        // ------------------------------------------------------------------ //

        private void ShowSelected(Character character) => Populate(selectedPanel, character);
        private void HideSelected() => selectedPanel.style.display = DisplayStyle.None;

        // ------------------------------------------------------------------ //
        // Right panel — hovered / attacked target                             //
        // ------------------------------------------------------------------ //

        private void ShowTarget(NPC npc) => Populate(targetPanel, npc);
        private void HideTarget() => targetPanel.style.display = DisplayStyle.None;

        private void Populate(VisualElement panel, Character character)
        {
            if (character == null) { panel.style.display = DisplayStyle.None; return; }

            panel.style.display = DisplayStyle.Flex;

            var stats = character.characterStats;
            var cls   = character.characterClass;

            // --- name / level -------------------------------------------------
            panel.Q<Label>("unit-name").text = character.characterName;
            panel.Q<Label>("unit-level").text = $"Lv {character.Level}";

            // --- zone theme -----------------------------------------------------
            // TODO: point this at whatever field actually stores elemental zone on
            // CharacterClass (e.g. cls.element). Left as a safe fallback so this
            // compiles even before that field is wired up.
            string zoneId = ResolveZoneId(cls);
            ApplyExclusiveClass(panel, ZoneClasses, $"zone-{zoneId}");

            // --- HP ---------------------------------------------------------
            int hp = Mathf.Max(0, stats.healthPoints);
            int maxHp = Mathf.Max(1, stats.maxHealthPoints);
            SetSegmentedBar(panel.Q<VisualElement>("hp-bar"), hp, maxHp, "seg-fill-hp");
            panel.Q<Label>("hp-num").text = $"{hp}/{maxHp}";

            // --- Mana ---------------------------------------------------------
            int mana = Mathf.Max(0, stats.mana);
            int maxMana = Mathf.Max(1, stats.maxMana); // adjust field name if different
            SetSegmentedBar(panel.Q<VisualElement>("mana-bar"), mana, maxMana, "seg-fill-mana");
            panel.Q<Label>("mana-num").text = $"{mana}/{maxMana}";

            // --- Movement (pips) ------------------------------------------------
            int move = stats.movement;
            SetPips(panel.Q<VisualElement>("move-pips"), move, move);
            panel.Q<Label>("move-num").text = $"{move}";

            // --- Attack (icon swaps by weapon type) ------------------------------
            panel.Q<Label>("atk-num").text = stats.TotalAttack.ToString();
            string weaponClass = cls?.classSpecialty switch
            {
                CharacterClass.ClassSpecialty.MELEE  => "weapon-melee",
                CharacterClass.ClassSpecialty.RANGED => "weapon-ranged",
                CharacterClass.ClassSpecialty.MAGIC  => "weapon-magic",
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