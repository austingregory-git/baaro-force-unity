using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.GameController;
using BaaroForce.Items;

namespace BaaroForce.UI
{
    /// <summary>
    /// Party-wide Inventory modal — a single WoW-style 32-slot bag grid mixing Equipment and
    /// Potions together (they already share one capacity, see <see cref="BaaroForce.Party.Party"/>),
    /// plus a Gold readout. Opened via the small backpack button both ActMapController (Act
    /// Map screen) and CombatCornerMenu (combat scene) add to their bottom-right corner.
    /// Renders into the owner's modal chassis (via the openModal/closeModal delegates and
    /// modalContent element passed into the constructor) instead of building its own overlay,
    /// matching how every other Act Map modal (Decree, Village, Anvil, Treasure, ...) is built.
    ///
    /// Not a MonoBehaviour — a plain UI builder object, same "data classes stay plain C#"
    /// convention as the rest of the model, just split into its own file because the grid
    /// plus Equip/Destroy sub-views is a lot of markup for ActMapController to carry inline.
    /// </summary>
    public class InventoryPanel
    {
        private readonly VisualElement _modalContent;
        private readonly Action<string, bool> _openModal;
        private readonly Action _closeModal;

        public InventoryPanel(VisualElement modalContent, Action<string, bool> openModal, Action closeModal)
        {
            _modalContent = modalContent;
            _openModal = openModal;
            _closeModal = closeModal;
        }

        /// <summary>Opens the Inventory modal on the bag grid. Never touches ActRunState —
        /// closing this modal must never advance the current Act Map node.</summary>
        public void Open()
        {
            _openModal("Inventory", true); // wide: true — sized to fit the 8-wide slot grid.
            RenderGrid();
        }

        // ---------------------------------------------------------------- //
        // Bag grid                                                           //
        // ---------------------------------------------------------------- //

        private void RenderGrid()
        {
            _modalContent.Clear();

            var party = PartyManager.Instance.Party;

            var header = new VisualElement();
            header.AddToClassList("inv-header-row");
            _modalContent.Add(header);

            var gold = new Label($"{party.Gold} Gold");
            gold.AddToClassList("act-choice-label");
            header.Add(gold);

            var capacity = new Label($"Bag: {party.InventoryUsed} / {BaaroForce.Party.Party.InventoryCapacity}");
            capacity.AddToClassList("act-choice-sub");
            header.Add(capacity);

            var grid = new VisualElement();
            grid.AddToClassList("inv-grid");
            _modalContent.Add(grid);

            // Fixed display order (equipment first, then potions) so slots don't visibly
            // reshuffle as items are equipped/destroyed mid-session.
            var items = new List<object>(party.EquipmentBag.Count + party.Potions.Count);
            items.AddRange(party.EquipmentBag);
            items.AddRange(party.Potions);

            for (int i = 0; i < BaaroForce.Party.Party.InventoryCapacity; i++)
                grid.Add(i < items.Count ? BuildFilledSlot(items[i]) : BuildEmptySlot());

            _modalContent.Add(MakeButton("Close", () => _closeModal()));
        }

        private static VisualElement BuildEmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("inv-slot");
            slot.AddToClassList("inv-slot-empty");
            return slot;
        }

        private VisualElement BuildFilledSlot(object item)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inv-slot");

            if (item is Equipment equipment)
            {
                slot.AddToClassList(RarityClass(equipment.Rarity));
                slot.Add(MakeGlyph(SlotGlyph(equipment.SlotType)));
                slot.tooltip = $"{equipment.Name} ({DescribeSlot(equipment)})\n{equipment.Description}\n{DescribeBonuses(equipment)}";
                slot.RegisterCallback<ClickEvent>(_ => ShowEquipmentItemDetail(equipment));
            }
            else if (item is Potion potion)
            {
                slot.AddToClassList(RarityClass(potion.Rarity));
                slot.Add(MakeGlyph("P"));
                slot.tooltip = $"{potion.Name} (Heal {potion.HealAmount})\n{potion.Description}";
                slot.RegisterCallback<ClickEvent>(_ => ShowPotionItemDetail(potion));
            }

            return slot;
        }

        private static Label MakeGlyph(string text)
        {
            var glyph = new Label(text);
            glyph.AddToClassList("inv-slot-glyph");
            return glyph;
        }

        private static string SlotGlyph(EquipmentSlotType slot)
        {
            switch (slot)
            {
                case EquipmentSlotType.Helmet: return "H";
                case EquipmentSlotType.Chest: return "C";
                case EquipmentSlotType.Legs: return "L";
                case EquipmentSlotType.MainHand: return "M";
                case EquipmentSlotType.OffHand: return "O";
                default: return "?";
            }
        }

        private static string RarityClass(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return "inv-slot-rarity-uncommon";
                case Rarity.Rare: return "inv-slot-rarity-rare";
                default: return "inv-slot-rarity-common";
            }
        }

        // ---------------------------------------------------------------- //
        // Equipment detail / Equip / Destroy                                 //
        // ---------------------------------------------------------------- //

        private void ShowEquipmentItemDetail(Equipment item)
        {
            _modalContent.Clear();

            var label = new Label($"{item.Name} ({DescribeSlot(item)})");
            label.AddToClassList("act-choice-label");
            _modalContent.Add(label);
            var desc = new Label(item.Description);
            desc.AddToClassList("act-modal-description");
            _modalContent.Add(desc);
            var stats = new Label(DescribeBonuses(item));
            stats.AddToClassList("act-choice-sub");
            _modalContent.Add(stats);

            _modalContent.Add(MakeButton("Equip", () => ShowEquipMemberPicker(item)));
            _modalContent.Add(MakeButton("Destroy", () => ShowDestroyConfirm(item.Name,
                () => PartyManager.Instance.Party.RemoveEquipment(item))));
            _modalContent.Add(MakeButton("Back", RenderGrid));
        }

        /// <summary>Member picker with a hover-preview side box showing whatever's currently
        /// equipped in the matching slot. Clicking a member equips immediately, auto-swapping
        /// any existing item in that slot back into the party bag in the same click.</summary>
        private void ShowEquipMemberPicker(Equipment item)
        {
            _modalContent.Clear();

            var title = new Label($"Equip {item.Name} to:");
            title.AddToClassList("act-choice-label");
            _modalContent.Add(title);

            var split = new VisualElement();
            split.AddToClassList("inv-picker-split");
            _modalContent.Add(split);

            var memberList = new VisualElement();
            memberList.AddToClassList("inv-picker-list");
            split.Add(memberList);

            var previewBox = new VisualElement();
            previewBox.AddToClassList("inv-picker-preview");
            var previewLabel = new Label("Hover a party member to preview their current gear.");
            previewLabel.AddToClassList("act-modal-description");
            previewBox.Add(previewLabel);
            split.Add(previewBox);

            foreach (Character member in PartyManager.Instance.Party.Members)
            {
                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label($"{member.CharacterName} (Lv {member.Level} {member.CharacterClass?.ClassID})");
                label.AddToClassList("act-choice-label");
                row.Add(label);

                Character capturedMember = member; // fixed per-row capture
                row.RegisterCallback<PointerEnterEvent>(_ =>
                {
                    Equipment current = capturedMember.GetEquipped(item.SlotType);
                    previewLabel.text = current != null
                        ? $"Currently equipped: {current.Name} ({DescribeBonuses(current)})"
                        : "Nothing currently equipped in this slot.";
                });
                row.RegisterCallback<PointerLeaveEvent>(_ =>
                    previewLabel.text = "Hover a party member to preview their current gear.");

                row.RegisterCallback<ClickEvent>(_ =>
                {
                    PartyManager.Instance.Party.RemoveEquipment(item);
                    Equipment previous = capturedMember.Equip(item);
                    if (previous != null) PartyManager.Instance.Party.TryAddEquipment(previous);
                    RenderGrid();
                });

                memberList.Add(row);
            }

            _modalContent.Add(MakeButton("Back", () => ShowEquipmentItemDetail(item)));
        }

        // ---------------------------------------------------------------- //
        // Potion detail / Destroy                                           //
        // ---------------------------------------------------------------- //

        private void ShowPotionItemDetail(Potion potion)
        {
            _modalContent.Clear();
            var label = new Label($"{potion.Name} (Heal {potion.HealAmount})");
            label.AddToClassList("act-choice-label");
            _modalContent.Add(label);
            var desc = new Label(potion.Description);
            desc.AddToClassList("act-modal-description");
            _modalContent.Add(desc);

            // Potions only offer Destroy here — "use" is a combat-only action
            // (TurnManager.OnItemsClicked), out of scope for this panel.
            _modalContent.Add(MakeButton("Destroy", () => ShowDestroyConfirm(potion.Name,
                () => PartyManager.Instance.Party.RemovePotion(potion))));
            _modalContent.Add(MakeButton("Back", RenderGrid));
        }

        // ---------------------------------------------------------------- //
        // Shared Yes/No destroy confirm                                      //
        // ---------------------------------------------------------------- //

        private void ShowDestroyConfirm(string itemName, Action confirmDestroy)
        {
            _modalContent.Clear();
            var label = new Label($"Destroy '{itemName}' permanently?");
            label.AddToClassList("act-modal-description");
            _modalContent.Add(label);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("act-tab-row");
            _modalContent.Add(buttonRow);
            buttonRow.Add(MakeButton("Yes", () => { confirmDestroy(); RenderGrid(); }));
            buttonRow.Add(MakeButton("No", RenderGrid));
        }

        // ---------------------------------------------------------------- //
        // Small helpers                                                      //
        // ---------------------------------------------------------------- //

        private static string DescribeSlot(Equipment e) => e.IsWeapon ? $"{e.SlotType}, Weapon" : e.SlotType.ToString();

        private static string DescribeBonuses(Equipment e)
        {
            var parts = new List<string>();
            if (e.AttackBonus != 0) parts.Add($"+{e.AttackBonus} ATK");
            if (e.HealthBonus != 0) parts.Add($"+{e.HealthBonus} HP");
            if (e.SpellPowerBonus != 0) parts.Add($"+{e.SpellPowerBonus} SP");
            if (e.ManaBonus != 0) parts.Add($"+{e.ManaBonus} Mana");
            if (e.MovementBonus != 0) parts.Add($"+{e.MovementBonus} Move");
            return parts.Count > 0 ? string.Join(", ", parts) : "no bonuses";
        }

        private static Button MakeButton(string label, Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("action-btn");
            btn.AddToClassList("fight-result-btn");
            return btn;
        }
    }
}
