using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Loot;

namespace BaaroForce.UI
{
    /// <summary>
    /// Full-screen modal shown once a fight ends — "Game Over" when the whole party is
    /// defeated, or "Fight Won!" with a claimable loot list when every enemy is defeated.
    /// Built in code in the same chassis/rivet style as the rest of the Combat HUD (see
    /// ActionPanelUI / CombatHud.uss) — reuses the scene's existing UIDocument.
    /// </summary>
    public class FightResultUI : MonoBehaviour
    {
        /// <summary>Fired when the player clicks the Game Over screen's button.</summary>
        public Action OnReturnToMainMenu;
        /// <summary>Fired when the player clicks the Fight Won screen's "Move on" button.</summary>
        public Action OnMoveOn;
        /// <summary>Fired when the player clicks an unclaimed loot entry.</summary>
        public Action<LootEntry> OnLootClaimed;

        private VisualElement _overlay;
        private Label _title;
        private VisualElement _lootList;
        private Button _primaryButton;
        private Action _primaryAction;

        private void Awake()     => BuildUI();
        private void OnDestroy() => _overlay?.RemoveFromHierarchy();

        private void BuildUI()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[FightResultUI] No UIDocument found in scene.");
                return;
            }

            _overlay = new VisualElement();
            _overlay.AddToClassList("fight-result-overlay");
            _overlay.style.display = DisplayStyle.None;

            var chassis = new VisualElement();
            chassis.AddToClassList("chassis");
            chassis.AddToClassList("fight-result-chassis");
            _overlay.Add(chassis);

            chassis.Add(MakeRivet("rivet-tl"));
            chassis.Add(MakeRivet("rivet-tr"));
            chassis.Add(MakeRivet("rivet-bl"));
            chassis.Add(MakeRivet("rivet-br"));

            _title = new Label();
            _title.AddToClassList("fight-result-title");
            chassis.Add(_title);

            _lootList = new VisualElement();
            _lootList.AddToClassList("loot-list");
            chassis.Add(_lootList);

            _primaryButton = new Button(() => _primaryAction?.Invoke());
            _primaryButton.AddToClassList("action-btn");
            _primaryButton.AddToClassList("fight-result-btn");
            chassis.Add(_primaryButton);

            doc.rootVisualElement.Add(_overlay);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        /// <summary>Shows the "Game Over" screen — all allies defeated.</summary>
        public void ShowGameOver()
        {
            if (_overlay == null) return;

            _title.text = "Game Over";
            _title.RemoveFromClassList("fight-result-title-win");
            _title.AddToClassList("fight-result-title-loss");

            _lootList.Clear();
            _lootList.style.display = DisplayStyle.None;

            _primaryButton.text = "Return to Main Menu";
            _primaryAction = () => OnReturnToMainMenu?.Invoke();

            _overlay.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hides this screen without tearing it down, e.g. to swap in LevelUpUI
        /// before actually proceeding past a won fight.</summary>
        public void Hide()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        /// <summary>Shows the "Fight Won!" screen with a claimable loot list — all enemies defeated.</summary>
        public void ShowFightWon(List<LootEntry> loot)
        {
            if (_overlay == null) return;

            _title.text = "Fight Won!";
            _title.RemoveFromClassList("fight-result-title-loss");
            _title.AddToClassList("fight-result-title-win");

            _lootList.Clear();
            _lootList.style.display = DisplayStyle.Flex;
            if (loot != null)
                foreach (LootEntry entry in loot)
                    _lootList.Add(MakeLootRow(entry));

            _primaryButton.text = "Move on";
            _primaryAction = () => OnMoveOn?.Invoke();

            _overlay.style.display = DisplayStyle.Flex;
        }

        private VisualElement MakeLootRow(LootEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("loot-row");

            var icon = new VisualElement();
            icon.AddToClassList("loot-icon");
            icon.AddToClassList(entry.Type == LootType.Gold ? "loot-icon-gold" : "loot-icon-item");
            row.Add(icon);

            if (entry.Type == LootType.Gold)
            {
                var iconGlyph = new Label("G");
                iconGlyph.AddToClassList("loot-icon-glyph");
                icon.Add(iconGlyph);
            }

            var label = new Label(entry.Type == LootType.Gold
                ? $"{entry.Amount} Gold"
                : $"{entry.Amount} x {entry.DisplayName}");
            label.AddToClassList("loot-label");
            row.Add(label);

            var claimedTag = new Label("Claimed");
            claimedTag.AddToClassList("loot-claimed-tag");
            claimedTag.style.display = DisplayStyle.None;
            row.Add(claimedTag);

            bool claimed = false;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (claimed) return;
                claimed = true;
                OnLootClaimed?.Invoke(entry);
                row.AddToClassList("loot-row-claimed");
                claimedTag.style.display = DisplayStyle.Flex;
            });

            return row;
        }
    }
}
