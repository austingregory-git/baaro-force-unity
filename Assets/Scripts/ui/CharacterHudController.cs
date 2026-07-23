using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Serialization;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.Formulas;
using BaaroForce.Map;
using BaaroForce.Spells;
using BaaroForce.Statuses;

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
        [SerializeField] private StyleSheet _styleSheet;         // CombatHud.uss

        private UIDocument _document;
        private VisualElement _selectedPanel;   // left
        private VisualElement _targetPanel;      // right
        private ActionArrowIndicator _arrow;

        // Which character each panel currently shows — cached so Update() can preview
        // the pending action against them without needing a fresh event each frame.
        private Character _selectedCharacter;
        private Npc       _targetCharacter;

        // Ghost/preview elements that should pulse this frame — rebuilt every
        // RefreshAllPreviews() call, one shared oscillator drives all of them.
        private readonly List<VisualElement> _pulsingElements = new List<VisualElement>();
        private const float PulseSpeed    = 3f;    // radians/sec
        private const float MinPulseAlpha = 0.35f;

        // All possible zone / weapon-type USS classes, so we can strip them cleanly
        // before applying the new one each time a panel is repopulated.
        // internal, not private — ZoneClasses is reused by CharacterInspectUI's chassis border.
        internal static readonly string[] ZoneClasses =
        {
            "zone-fire", "zone-water", "zone-earth", "zone-wind", "zone-light", "zone-dark"
        };
        private static readonly string[] WeaponClasses = { "weapon-melee", "weapon-ranged", "weapon-magic" };

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        /// <summary>
        /// Continuously previews whatever action TurnManager is currently aiming (a
        /// basic attack or a targeted spell) against the caster's own panel and/or the
        /// hovered target's panel, and drives the shared pulse animation for any ghost
        /// elements the preview created. Runs every frame (like TooltipSystem/
        /// WarningToastUI's own Update polling) rather than trying to track every place
        /// TurnManager's targeting state can change.
        /// </summary>
        private void Update()
        {
            if (_selectedPanel == null || _targetPanel == null) return;

            RefreshAllPreviews();

            if (_pulsingElements.Count == 0) return;
            float alpha = Mathf.Lerp(MinPulseAlpha, 1f, (Mathf.Sin(Time.time * PulseSpeed) + 1f) * 0.5f);
            foreach (VisualElement el in _pulsingElements)
                el.style.opacity = alpha;
        }

        private Coroutine _hookRoutine;

        private void OnEnable()
        {
            BuildPanels();
            _hookRoutine = StartCoroutine(HookTurnManager());
        }

        private void OnDisable()
        {
            if (_hookRoutine != null)
            {
                StopCoroutine(_hookRoutine);
                _hookRoutine = null;
            }

            if (_turnManager == null) return;
            _turnManager.OnCharacterSelected   -= ShowSelected;
            _turnManager.OnCharacterDeselected -= HideSelected;
            _turnManager.OnTargetHighlighted   -= ShowTarget;
            _turnManager.OnTargetCleared       -= HideTarget;
        }

        // MapGenerator creates TurnManager at runtime (via AddComponent in its own
        // Start), which runs after this component's OnEnable — so a one-shot lookup
        // here would race it and silently never subscribe. Poll a few frames instead.
        private System.Collections.IEnumerator HookTurnManager()
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
            {
                Debug.LogWarning("[CharacterHudController] No TurnManager found in scene.");
                yield break;
            }

            _turnManager.OnCharacterSelected   += ShowSelected;
            _turnManager.OnCharacterDeselected += HideSelected;
            _turnManager.OnTargetHighlighted   += ShowTarget;
            _turnManager.OnTargetCleared       += HideTarget;
        }

        // ------------------------------------------------------------------ //
        // Panel construction — one instance of CombatHudPanel.uxml per side.  //
        // ------------------------------------------------------------------ //

        private void BuildPanels()
        {
            VisualElement root = _document.rootVisualElement;
            if (_styleSheet != null) root.styleSheets.Add(_styleSheet);

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

            _arrow = new ActionArrowIndicator(root);
        }

        // ------------------------------------------------------------------ //
        // Left panel — current-turn character                                 //
        // ------------------------------------------------------------------ //

        private void ShowSelected(Character character)
        {
            _selectedCharacter = character;
            Populate(_selectedPanel, character, useRemainingMovement: true);
        }

        private void HideSelected()
        {
            _selectedCharacter = null;
            _selectedPanel.style.display = DisplayStyle.None;
        }

        // ------------------------------------------------------------------ //
        // Right panel — hovered / attacked target                             //
        // ------------------------------------------------------------------ //

        private void ShowTarget(Npc npc)
        {
            _targetCharacter = npc;
            Populate(_targetPanel, npc, useRemainingMovement: false);
        }

        private void HideTarget()
        {
            _targetCharacter = null;
            _targetPanel.style.display = DisplayStyle.None;
        }

        private void Populate(VisualElement panel, Character character, bool useRemainingMovement)
        {
            if (character == null) { panel.style.display = DisplayStyle.None; return; }

            panel.style.display = DisplayStyle.Flex;

            var stats = character.CharacterStats;
            var cls   = character.CharacterClass;

            // --- name / level -------------------------------------------------
            panel.Q<Label>("unit-name").text = character.CharacterName;
            panel.Q<Label>("unit-level").text = $"Lv {character.Level}";

            // --- zone theme -----------------------------------------------------
            string zoneId = ResolveZoneId(character);
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
            int maxMove = stats.Movement;
            int move = useRemainingMovement && _turnManager != null
                ? _turnManager.RemainingMove(character)
                : maxMove;
            SetPips(panel.Q<VisualElement>("move-pips"), move, maxMove);
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

            // --- Shield (only shown while the character actually has any) -------
            int shield = Mathf.Max(0, stats.ShieldPoints);
            panel.Q<VisualElement>("shield-row").style.display =
                shield > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            panel.Q<Label>("shield-num").text = shield.ToString();

            // --- Active status effects (buffs/debuffs) --------------------------
            PopulateStatusEffects(panel.Q<VisualElement>("status-effects"), character);
        }

        // ------------------------------------------------------------------ //
        // Action-outcome preview — "what will happen if this resolves"        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Re-applies the pending action's predicted outcome (if any) to both panels,
        /// and points/hides the arrow between them. Always touches both panels — even
        /// with nothing pending — so bars/labels reliably fall back to their plain
        /// (non-preview) look through the exact same rendering path, rather than a
        /// separate "clear" branch that could drift out of sync with it.
        /// </summary>
        private void RefreshAllPreviews()
        {
            _pulsingElements.Clear();

            var pending = _turnManager != null ? _turnManager.PendingAction : null;
            Character caster = pending?.caster;
            Spell     spell   = pending?.spell; // null spell with non-null pending = basic attack

            if (_selectedCharacter != null)
            {
                bool casterIsAiming = pending != null && caster == _selectedCharacter;
                ActionPreview casterPreview = casterIsAiming
                    ? ComputeCasterPreview(caster, spell)
                    : ActionPreview.None;
                int casterManaCost = casterIsAiming ? (spell?.ManaCost ?? 0) : 0;
                ApplyPreview(_selectedPanel, _selectedCharacter, casterPreview, casterManaCost);
            }

            bool targetIsAimed = pending != null && _targetCharacter != null &&
                                  (spell == null || spell.TargetType != SpellTargetType.Self);
            if (_targetCharacter != null)
            {
                ActionPreview targetPreview = targetIsAimed
                    ? (spell != null
                        ? spell.GetPreview(caster, _targetCharacter)
                        : new ActionPreview { RawDamage = Mathf.Max(0, caster.CharacterStats.TotalAttack)
                            * caster.PeekAimMultiplier() * caster.PeekEmpowerMultiplier() })
                    : ActionPreview.None;
                ApplyPreview(_targetPanel, _targetCharacter, targetPreview, manaCost: 0);
            }

            if (targetIsAimed && _selectedPanel.style.display == DisplayStyle.Flex &&
                _targetPanel.style.display == DisplayStyle.Flex)
                _arrow.PointBetween(_selectedPanel, _targetPanel);
            else
                _arrow.Hide();
        }

        /// <summary>A Self-type spell's own effect on its caster (Grit, Rally); anything
        /// else has nothing to preview on the caster's own panel beyond mana cost.</summary>
        private static ActionPreview ComputeCasterPreview(Character caster, Spell spell) =>
            spell != null && spell.TargetType == SpellTargetType.Self
                ? spell.GetPreview(caster, caster)
                : ActionPreview.None;

        /// <summary>
        /// Overlays a predicted outcome onto an already-Populate()'d panel: dual-state
        /// HP/mana bars, a death overlay on a lethal hit, +/-N labels next to attack and
        /// shield, and a hollow "ghost" chip for a status effect that would be applied.
        /// Safe to call with an all-zero <paramref name="preview"/> and 0 mana cost — it
        /// then just re-renders the plain (non-preview) state via the same code path.
        /// </summary>
        private void ApplyPreview(VisualElement panel, Character character, ActionPreview preview, int manaCost)
        {
            CharacterStats stats = character.CharacterStats;

            // --- HP (+ predicted death) -----------------------------------------
            int currentHp    = Mathf.Max(0, stats.HealthPoints);
            int currentMaxHp = Mathf.Max(1, stats.MaxHealthPoints);
            int predictedMaxHp = currentMaxHp + Mathf.Max(0, preview.MaxHpDelta);

            int predictedHp = currentHp;
            if (preview.RawDamage > 0)
                predictedHp = Mathf.Max(0, stats.PeekDamage(preview.RawDamage).predictedHp);
            else if (preview.RawHeal > 0)
                predictedHp = stats.PeekHeal(preview.RawHeal);
            if (preview.MaxHpDelta > 0)
                predictedHp = Mathf.Min(predictedMaxHp, predictedHp + preview.MaxHpDelta);

            SetSegmentedBarWithPreview(panel.Q<VisualElement>("hp-bar"), currentHp, currentMaxHp,
                predictedHp, predictedMaxHp, "seg-fill-hp", _pulsingElements);

            VisualElement chassis = panel.Q<VisualElement>("chassis");
            VisualElement deathOverlay = chassis?.Q<VisualElement>("death-overlay");
            if (preview.RawDamage > 0 && predictedHp <= 0)
            {
                if (deathOverlay == null)
                {
                    deathOverlay = BuildDeathOverlay();
                    chassis.Add(deathOverlay);
                }
                _pulsingElements.Add(deathOverlay);
            }
            else
            {
                deathOverlay?.RemoveFromHierarchy();
            }

            // --- Mana (caster-only spend preview) -------------------------------
            int currentMana   = Mathf.Max(0, stats.Mana);
            int maxMana       = Mathf.Max(1, stats.MaxMana);
            int predictedMana = manaCost > 0 ? Mathf.Max(0, currentMana - manaCost) : currentMana;
            SetSegmentedBarWithPreview(panel.Q<VisualElement>("mana-bar"), currentMana, maxMana,
                predictedMana, maxMana, "seg-fill-mana", _pulsingElements);

            // --- Attack / Shield deltas ------------------------------------------
            ShowStatDelta(panel, "atk-num", "atk-delta", preview.AttackBonusDelta, _pulsingElements);

            VisualElement shieldRow = panel.Q<VisualElement>("shield-row");
            if (preview.ShieldGain > 0) shieldRow.style.display = DisplayStyle.Flex;
            ShowStatDelta(panel, "shield-num", "shield-delta", preview.ShieldGain, _pulsingElements);

            // --- Ghost status-effect chip ----------------------------------------
            ApplyGhostStatusChip(panel.Q<VisualElement>("status-effects"), character,
                preview.StatusEffectName, preview.StatusEffectKind, _pulsingElements);
        }

        private static VisualElement BuildDeathOverlay()
        {
            var overlay = new VisualElement { name = "death-overlay" };
            overlay.AddToClassList("death-overlay");
            var label = new Label("✖"); // ✖
            label.AddToClassList("death-overlay-label");
            overlay.Add(label);
            return overlay;
        }

        /// <summary>Adds/updates/removes the small +N/-N label next to a stat number,
        /// registering it to pulse while present.</summary>
        private static void ShowStatDelta(VisualElement panel, string numberElementName,
            string deltaElementName, int delta, List<VisualElement> pulseTarget)
        {
            Label numberEl = panel.Q<Label>(numberElementName);
            VisualElement parentRow = numberEl.parent;
            Label deltaLabel = parentRow.Q<Label>(deltaElementName);

            if (delta == 0)
            {
                deltaLabel?.RemoveFromHierarchy();
                return;
            }

            if (deltaLabel == null)
            {
                deltaLabel = new Label { name = deltaElementName };
                deltaLabel.AddToClassList("stat-delta");
                parentRow.Add(deltaLabel);
            }

            deltaLabel.text = (delta > 0 ? "+" : "") + delta;
            deltaLabel.RemoveFromClassList("stat-delta-buff");
            deltaLabel.RemoveFromClassList("stat-delta-debuff");
            deltaLabel.AddToClassList(delta > 0 ? "stat-delta-buff" : "stat-delta-debuff");
            pulseTarget.Add(deltaLabel);
        }

        /// <summary>Adds/updates/removes the hollow "would be applied" status chip,
        /// registering it to pulse while present.</summary>
        private static void ApplyGhostStatusChip(VisualElement statusContainer, Character character,
            string statusEffectName, StatusEffect.StatusEffectType? kind, List<VisualElement> pulseTarget)
        {
            const string ghostName = "status-chip-ghost";
            Label ghost = statusContainer.Q<Label>(ghostName);

            bool alreadyActive = statusEffectName != null &&
                                  character.ActiveEffects.Exists(e => e.Name == statusEffectName);
            if (statusEffectName == null || alreadyActive)
            {
                ghost?.RemoveFromHierarchy();
            }
            else
            {
                if (ghost == null)
                {
                    ghost = new Label { name = ghostName };
                    ghost.AddToClassList("status-chip");
                    ghost.AddToClassList("status-chip--ghost");
                    statusContainer.Add(ghost);
                }

                ghost.text = statusEffectName;
                ghost.RemoveFromClassList("status-chip-buff");
                ghost.RemoveFromClassList("status-chip-debuff");
                ghost.RemoveFromClassList("status-chip-custom");
                ghost.AddToClassList(kind switch
                {
                    StatusEffect.StatusEffectType.Buff   => "status-chip-buff",
                    StatusEffect.StatusEffectType.Debuff => "status-chip-debuff",
                    _                                      => "status-chip-custom",
                });
                pulseTarget.Add(ghost);
            }

            statusContainer.style.display =
                statusContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Rebuilds a segmented bar showing both the current filled count and, via
        /// distinctly-styled "ghost" segments appended to <paramref name="pulseTarget"/>,
        /// any predicted change: segments about to be lost render orange, segments about
        /// to be gained (including new capacity beyond the current max) render pale
        /// green. With <paramref name="predicted"/> == <paramref name="current"/> and
        /// <paramref name="predictedMax"/> == <paramref name="currentMax"/> this renders
        /// identically to the plain <see cref="SetSegmentedBar"/>.
        /// </summary>
        private static void SetSegmentedBarWithPreview(VisualElement bar, int current, int currentMax,
            int predicted, int predictedMax, string fillClass, List<VisualElement> pulseTarget)
        {
            if (bar == null) return;
            bar.Clear();

            int total = Mathf.Max(1, Mathf.Max(currentMax, predictedMax));
            for (int i = 0; i < total; i++)
            {
                var seg = new VisualElement();
                seg.AddToClassList("seg");

                bool filledNow   = i < current;
                bool filledLater = i < predicted;

                if (filledNow && filledLater)
                {
                    seg.AddToClassList(fillClass);
                }
                else if (filledNow)
                {
                    seg.AddToClassList("seg-pending-loss");
                    pulseTarget.Add(seg);
                }
                else if (filledLater)
                {
                    seg.AddToClassList("seg-pending-gain");
                    pulseTarget.Add(seg);
                }

                bar.Add(seg);
            }
        }

        /// <summary>internal, not private — reused by CharacterInspectUI's status-effects row.</summary>
        internal static void PopulateStatusEffects(VisualElement container, Character character)
        {
            container.Clear();

            foreach (StatusEffect effect in character.ActiveEffects)
            {
                string label = effect.Stacks > 1 ? $"{effect.Name} x{effect.Stacks}" : effect.Name;
                var chip = new Label(effect.RemainingTurns >= 0
                    ? $"{label} ({effect.RemainingTurns})"
                    : label);
                chip.AddToClassList("status-chip");
                chip.AddToClassList(effect.EffectType switch
                {
                    StatusEffect.StatusEffectType.Buff   => "status-chip-buff",
                    StatusEffect.StatusEffectType.Debuff => "status-chip-debuff",
                    _                                     => "status-chip-custom",
                });
                chip.tooltip = effect.Description;
                container.Add(chip);
            }

            container.style.display =
                character.ActiveEffects.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>internal, not private — reused by CharacterInspectUI's zone-tinted border.</summary>
        internal static string ResolveZoneId(Character character)
        {
            List<Realm> realms = character.CharacterRealms;
            if (realms != null && realms.Count > 0)
                return realms[0].ToString().ToLowerInvariant();
            return "earth";
        }

        // ------------------------------------------------------------------ //
        // Small UI-building helpers                                           //
        // ------------------------------------------------------------------ //

        /// <summary>Rebuilds a segmented bar (children = individual segment divs) to show
        /// filled/total. internal, not private — reused by CharacterInspectUI's stats column.</summary>
        internal static void SetSegmentedBar(VisualElement bar, int filled, int total, string fillClass)
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

        /// <summary>Rebuilds the diamond movement pips to show filled/total. internal, not
        /// private — reused by CharacterInspectUI's stats column.</summary>
        internal static void SetPips(VisualElement pips, int filled, int total)
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

        /// <summary>internal, not private — reused by CharacterInspectUI's zone-tinted border.</summary>
        internal static void ApplyExclusiveClass(VisualElement el, string[] allClasses, string activeClass)
        {
            if (el == null) return;
            foreach (var c in allClasses) el.RemoveFromClassList(c);
            el.AddToClassList(activeClass);
        }
    }
}