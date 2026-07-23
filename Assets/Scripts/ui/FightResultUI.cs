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

        /// <summary>Landing spot for the claim-flight animation on a Gold entry — set by
        /// TurnManager to CombatCornerMenu's Gold readout once both exist. Null-safe: the
        /// flourish is just skipped if this was never wired up. A property (not a plain field)
        /// so Unity's serializer doesn't try — and fail — to serialize a VisualElement.</summary>
        public VisualElement GoldFlightTarget { get; set; }
        /// <summary>Landing spot for the claim-flight animation on an Equipment/Potion entry —
        /// set by TurnManager to CombatCornerMenu's Inventory button.</summary>
        public VisualElement ItemFlightTarget { get; set; }

        // Collapse (row shrinking away) and flight (icon flying to its corner target) are two
        // separate, independently-timed animations fired together on click — see
        // CollapseRow/FlyToTarget. Keeping them close in length is what reads as "one quick
        // motion" rather than two disjointed effects.
        private const long CollapseMilliseconds = 220;
        private const long FlyMilliseconds      = 280;

        private VisualElement _root;
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
            _root = doc.rootVisualElement;

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

            string tintClass = entry.Type == LootType.Gold ? "loot-icon-gold" : "loot-icon-item";

            var icon = new VisualElement();
            icon.AddToClassList("loot-icon");
            icon.AddToClassList(tintClass);
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

            bool claimed = false;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (claimed) return;
                claimed = true;

                VisualElement target = entry.Type == LootType.Gold ? GoldFlightTarget : ItemFlightTarget;
                FlyToTarget(icon, target, tintClass);
                OnLootClaimed?.Invoke(entry);
                CollapseRow(row);
            });

            return row;
        }

        /// <summary>Shrinks <paramref name="row"/> away (height/opacity/margin all animate to
        /// zero via the .loot-row-collapsed USS transition) and removes it once that finishes —
        /// the remaining rows naturally slide up to fill the gap as part of the same layout
        /// reflow, so the player can keep clicking the same spot to claim everything.</summary>
        private static void CollapseRow(VisualElement row)
        {
            // Lock in the row's current rendered height as an explicit pixel value first — a
            // USS transition can't animate away from "auto" — then flip on the collapsed class
            // a tick later so the layout engine commits the explicit height as a distinct frame
            // before the target (zero) height kicks in.
            row.style.height = row.resolvedStyle.height;
            row.style.overflow = Overflow.Hidden;

            row.schedule.Execute(() => row.AddToClassList("loot-row-collapsed")).StartingIn(0);
            row.schedule.Execute(() => row.RemoveFromHierarchy()).StartingIn(CollapseMilliseconds);
        }

        /// <summary>Spawns a small floating copy of <paramref name="source"/> at its current
        /// screen position and manually tweens it (frame-by-frame, not a USS transition — see
        /// the class comment on why) shrinking into <paramref name="target"/>'s position — the
        /// Gold readout or Inventory button — then removes it. A quick, purely cosmetic
        /// flourish; skipped entirely if <paramref name="target"/> was never wired up (e.g.
        /// CombatCornerMenu failed to build) or either element hasn't been laid out yet.</summary>
        private void FlyToTarget(VisualElement source, VisualElement target, string tintClass)
        {
            if (target == null || _root == null) return;

            Rect from = source.worldBound;
            Rect to = target.worldBound;
            if (float.IsNaN(from.x) || float.IsNaN(to.x)) return;

            var flyer = new VisualElement();
            flyer.AddToClassList("loot-fly-icon");
            flyer.AddToClassList(tintClass);
            flyer.pickingMode = PickingMode.Ignore;
            // Set position explicitly rather than relying solely on the USS class for it — this
            // element is inserted straight into the root, outside any flex flow, so it must be
            // absolutely positioned from the very first frame it exists.
            flyer.style.position = Position.Absolute;
            flyer.style.left = from.x;
            flyer.style.top = from.y;
            flyer.style.width = from.width;
            flyer.style.height = from.height;
            flyer.style.opacity = 1f;
            _root.Add(flyer);
            // Freshly added as the last child of root, so it's already frontmost — but assert
            // it explicitly anyway, same reasoning as WarningToastUI.Show's BringToFront.
            flyer.BringToFront();

            float targetLeft = to.x + to.width / 2f - 4f;
            float targetTop  = to.y + to.height / 2f - 4f;

            const int stepMs = 16; // ~60 steps/sec
            int totalSteps = Mathf.Max(1, (int)(FlyMilliseconds / stepMs));
            int step = 0;
            IVisualElementScheduledItem anim = null;
            anim = flyer.schedule.Execute(() =>
            {
                step++;
                float t = Mathf.Clamp01((float)step / totalSteps);
                float eased = 1f - (1f - t) * (1f - t); // ease-out: fast start, gentle landing

                flyer.style.left   = Mathf.Lerp(from.x, targetLeft, eased);
                flyer.style.top    = Mathf.Lerp(from.y, targetTop, eased);
                flyer.style.width  = Mathf.Lerp(from.width, 8f, eased);
                flyer.style.height = Mathf.Lerp(from.height, 8f, eased);
                flyer.style.opacity = Mathf.Lerp(1f, 0f, eased);

                if (t >= 1f)
                {
                    anim.Pause();
                    flyer.RemoveFromHierarchy();
                }
            }).Every(stepMs);
        }
    }
}
