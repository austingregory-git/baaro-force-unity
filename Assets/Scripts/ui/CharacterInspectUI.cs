using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.Items;
using BaaroForce.Spells;
using BaaroForce.Passives;

namespace BaaroForce.UI
{
    /// <summary>
    /// Full character-sheet modal opened via ActionPanelUI's "Inspect" button. Built in code,
    /// same chassis/rivet/overlay convention as ActionPanelUI and CombatCornerMenu's modal
    /// shell. Reuses CharacterSelectionManager's ability-chip/stat-row builders,
    /// CharacterHudController's segmented-bar/pip/status-effect/zone-theming helpers, and
    /// InventoryPanel's equipment glyph/rarity/description helpers so every section matches
    /// an existing screen instead of reimplementing the same look a third or fourth time.
    /// </summary>
    public class CharacterInspectUI : MonoBehaviour
    {
        private static readonly (EquipmentSlotType slot, string label)[] SlotRows =
        {
            (EquipmentSlotType.Helmet,   "Head"),
            (EquipmentSlotType.Chest,    "Chest"),
            (EquipmentSlotType.Legs,     "Legs"),
            (EquipmentSlotType.MainHand, "Main-Hand"),
            (EquipmentSlotType.OffHand,  "Off-Hand"),
            (EquipmentSlotType.Trinket,  "Trinket"),
        };

        private VisualElement _overlay;
        private VisualElement _chassis;
        private VisualElement _equipCol;
        private VisualElement _statsCol;
        private VisualElement _statusRow;
        private VisualElement _abilitiesRow;
        private VisualElement _portrait;
        private Label _nameLabel;
        private Label _subtitleLabel;

        /// <summary>True while the panel is actively displayed.</summary>
        public bool IsVisible => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()     => BuildPanel();
        private void OnDestroy() => _overlay?.RemoveFromHierarchy();

        // Escape closes Inspect even though TurnManager's own Update() (and its own Escape
        // handling) is skipped entirely while this panel is open — see IsPlayerInputBlocked()
        // in TurnManager, which this panel's visibility feeds into.
        private void Update()
        {
            if (IsVisible && Input.GetKeyDown(KeyCode.Escape)) Hide();
        }

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildPanel()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[CharacterInspectUI] No UIDocument found in scene.");
                return;
            }

            // The ability chips reused from CharacterSelectionManager (.ability-chip etc.) are
            // styled in CharacterSelect.uss, which — unlike CombatHud.uss — isn't already wired
            // to this scene's UIDocument (it's normally only loaded on the separate character-
            // select screen). Add it at runtime the same way CombatCornerMenu adds ActMap.uss,
            // otherwise those chips render with no styling at all (unreadable default text).
            StyleSheet selectStyles = Resources.Load<StyleSheet>("CharacterSelect");
            if (selectStyles != null) doc.rootVisualElement.styleSheets.Add(selectStyles);

            _overlay = new VisualElement();
            _overlay.AddToClassList("inspect-overlay");
            _overlay.style.display = DisplayStyle.None;
            doc.rootVisualElement.Add(_overlay);

            _chassis = new VisualElement();
            _chassis.AddToClassList("chassis");
            _chassis.AddToClassList("inspect-chassis");
            _overlay.Add(_chassis);

            _chassis.Add(MakeRivet("rivet-tl"));
            // No rivet-tr — the close button below sits in that exact corner spot instead.
            _chassis.Add(MakeRivet("rivet-bl"));
            _chassis.Add(MakeRivet("rivet-br"));

            var closeBtn = new Button(Hide) { text = "X" };
            closeBtn.AddToClassList("inspect-close-btn");
            _chassis.Add(closeBtn);

            var header = new VisualElement();
            header.AddToClassList("inspect-header");
            _chassis.Add(header);

            _nameLabel = new Label();
            _nameLabel.AddToClassList("unit-name");
            _nameLabel.AddToClassList("inspect-name");
            header.Add(_nameLabel);

            _subtitleLabel = new Label();
            _subtitleLabel.AddToClassList("inspect-subtitle");
            header.Add(_subtitleLabel);

            var body = new VisualElement();
            body.AddToClassList("inspect-body");
            _chassis.Add(body);

            _equipCol = new VisualElement();
            _equipCol.AddToClassList("inspect-equip-col");
            body.Add(_equipCol);

            var portraitCol = new VisualElement();
            portraitCol.AddToClassList("inspect-portrait-col");
            body.Add(portraitCol);

            _portrait = new VisualElement();
            _portrait.AddToClassList("portrait");
            _portrait.AddToClassList("inspect-portrait");
            portraitCol.Add(_portrait);

            _statusRow = new VisualElement();
            _statusRow.AddToClassList("status-effects");
            _statusRow.AddToClassList("inspect-status-effects");
            portraitCol.Add(_statusRow);

            _statsCol = new VisualElement();
            _statsCol.AddToClassList("stat-list");
            _statsCol.AddToClassList("inspect-stats-col");
            body.Add(_statsCol);

            _abilitiesRow = new VisualElement();
            _abilitiesRow.AddToClassList("inspect-abilities");
            _chassis.Add(_abilitiesRow);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Rebuilds and shows the full character sheet for <paramref name="character"/>.</summary>
        public void Show(Character character)
        {
            if (_overlay == null || character == null) return;

            CharacterHudController.ApplyExclusiveClass(_chassis, CharacterHudController.ZoneClasses,
                $"zone-{CharacterHudController.ResolveZoneId(character)}");

            _nameLabel.text = character.CharacterName;
            _subtitleLabel.text = $"Lv {character.Level} {character.CharacterClass?.ClassID ?? ""}";

            Sprite portraitSprite = !string.IsNullOrEmpty(character.CharacterProfilePicPath)
                ? Resources.Load<Sprite>(character.CharacterProfilePicPath)
                : null;
            _portrait.style.backgroundImage = portraitSprite != null
                ? new StyleBackground(portraitSprite)
                : new StyleBackground(StyleKeyword.None);

            BuildEquipmentColumn(character);
            BuildStatsColumn(character);
            CharacterHudController.PopulateStatusEffects(_statusRow, character);
            BuildAbilitiesRow(character);

            _overlay.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hides the panel. Does not end the turn or change selection — the character
        /// stays selected and it's still their turn underneath.</summary>
        public void Hide()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        // ---------------------------------------------------------------- //
        // Equipment column                                                   //
        // ---------------------------------------------------------------- //

        private void BuildEquipmentColumn(Character character)
        {
            _equipCol.Clear();
            foreach ((EquipmentSlotType slot, string label) in SlotRows)
                _equipCol.Add(BuildEquipSlotRow(character, slot, label));
        }

        private static VisualElement BuildEquipSlotRow(Character character, EquipmentSlotType slotType, string label)
        {
            var row = new VisualElement();
            row.AddToClassList("inspect-equip-row");

            Equipment equipped = character.GetEquipped(slotType);

            var slot = new VisualElement();
            slot.AddToClassList("inv-slot");
            slot.AddToClassList("inspect-equip-slot");

            if (equipped != null)
            {
                slot.AddToClassList(InventoryPanel.RarityClass(equipped.Rarity));

                var glyph = new Label(InventoryPanel.SlotGlyph(slotType));
                glyph.AddToClassList("inv-slot-glyph");
                slot.Add(glyph);

                string title = $"{equipped.Name} ({InventoryPanel.DescribeSlot(equipped)})";
                string description = equipped.Description;
                string bonuses = InventoryPanel.DescribeBonuses(equipped);
                slot.RegisterCallback<PointerEnterEvent>(_ =>
                    TooltipSystem.Instance?.Show(title, description, bonuses));
                slot.RegisterCallback<PointerLeaveEvent>(_ => TooltipSystem.Instance?.Hide());
            }
            else
            {
                slot.AddToClassList("inv-slot-empty");
                slot.tooltip = $"Empty — {label}";
            }

            row.Add(slot);

            var name = new Label(label);
            name.AddToClassList("inspect-equip-label");
            row.Add(name);

            return row;
        }

        // ---------------------------------------------------------------- //
        // Stats column — current/max, unlike CharacterSelectionManager's    //
        // always-full pre-battle cards, matching CharacterHudController's   //
        // live in-combat readout instead.                                   //
        // ---------------------------------------------------------------- //

        private void BuildStatsColumn(Character character)
        {
            _statsCol.Clear();
            CharacterStats stats = character.CharacterStats;

            _statsCol.Add(BuildBarStat("stat-badge-hp", Mathf.Max(0, stats.HealthPoints),
                Mathf.Max(1, stats.MaxHealthPoints), "seg-fill-hp"));
            _statsCol.Add(BuildBarStat("stat-badge-mana", Mathf.Max(0, stats.Mana),
                Mathf.Max(1, stats.MaxMana), "seg-fill-mana"));
            _statsCol.Add(BuildPipStat(stats.Movement));
            _statsCol.Add(CharacterSelectionManager.BuildAttackStat(character.CharacterClass, stats.TotalAttack));

            if (stats.ShieldPoints > 0)
                _statsCol.Add(BuildShieldStat(stats.ShieldPoints));

            // Attack (above) is already Base+Bonus combined; Spell Power has no separate base
            // to add (CharacterStats only tracks SpellPowerBonus) — showing Attack Bonus here
            // too keeps the two equipment-driven bonus stats consistent with each other.
            _statsCol.Add(BuildTextStat("Attack Bonus", stats.AttackBonus));
            _statsCol.Add(BuildTextStat("Spell Power", stats.SpellPowerBonus));
        }

        private static VisualElement BuildBarStat(string badgeClass, int filled, int total, string fillClass)
        {
            VisualElement row = CharacterSelectionManager.BuildStatRow(badgeClass);
            var bar = new VisualElement();
            bar.AddToClassList("seg-bar");
            row.Add(bar);
            CharacterHudController.SetSegmentedBar(bar, filled, total, fillClass);
            var num = new Label($"{filled}/{total}");
            num.AddToClassList("stat-num");
            row.Add(num);

            if (badgeClass == "stat-badge-hp") StatTooltips.AttachHp(row);
            else if (badgeClass == "stat-badge-mana") StatTooltips.AttachMana(row);

            return row;
        }

        private static VisualElement BuildPipStat(int movement)
        {
            VisualElement row = CharacterSelectionManager.BuildStatRow("stat-badge-move");
            var pips = new VisualElement();
            pips.AddToClassList("pips");
            row.Add(pips);
            CharacterHudController.SetPips(pips, movement, movement);
            var num = new Label(movement.ToString());
            num.AddToClassList("stat-num");
            row.Add(num);
            StatTooltips.AttachMovement(row);
            return row;
        }

        // Shield has no bar in the existing HUD (see CombatHudPanel.uxml's shield-row) — just
        // a badge and a number, so this mirrors that instead of CharacterSelectionManager's
        // bar-based BuildBarStat.
        private static VisualElement BuildShieldStat(int shield)
        {
            VisualElement row = CharacterSelectionManager.BuildStatRow("stat-badge-shield");
            var num = new Label(shield.ToString());
            num.AddToClassList("stat-num");
            num.AddToClassList("stat-num-big");
            row.Add(num);
            StatTooltips.AttachShield(row);
            return row;
        }

        // No icon asset exists for Attack Bonus / Spell Power anywhere yet, so these are plain
        // text rows rather than new badges.
        private static VisualElement BuildTextStat(string label, int value)
        {
            var row = new VisualElement();
            row.AddToClassList("stat-row");

            var text = new Label(label);
            text.AddToClassList("inspect-stat-text-label");
            row.Add(text);

            var num = new Label(value.ToString());
            num.AddToClassList("stat-num");
            row.Add(num);

            if (label == "Attack Bonus") StatTooltips.AttachAttackBonus(row);
            else if (label == "Spell Power") StatTooltips.AttachSpellPower(row);

            return row;
        }

        // ---------------------------------------------------------------- //
        // Spells / passives — one flat wrapped row of chips (no Passive/     //
        // Character Spell/Class Spell category headings) — the chips'       //
        // own tint colors already distinguish spells from passives.         //
        // ---------------------------------------------------------------- //

        private void BuildAbilitiesRow(Character character)
        {
            _abilitiesRow.Clear();

            var row = new VisualElement();
            row.AddToClassList("select-card-abilities");
            _abilitiesRow.Add(row);

            foreach (PassiveAbility passive in character.CharacterPassiveAbilities)
                row.Add(CharacterSelectionManager.BuildAbilityChip(passive.Name,
                    passive.GetSummary(character), passive.GetDetailedDescription(character), null));

            foreach (Spell spell in character.CharacterSpells)
            {
                string tintClass = spell is ClassSpell ? "ability-chip-class-spell" : "ability-chip-spell";
                row.Add(CharacterSelectionManager.BuildAbilityChip(spell.Name,
                    spell.GetSummary(character), spell.GetDetailedDescription(character), tintClass));
            }
        }
    }
}
