using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.ActMap;

namespace BaaroForce.UI
{
    /// <summary>
    /// Renders the Act Map's node-spine (badges + connecting lines) into a container
    /// VisualElement. Shared by <see cref="ActMapController"/> (the interactive Act Map
    /// screen) and <see cref="CombatCornerMenu"/>'s read-only glance-at-the-map overlay
    /// during combat — pass <paramref name="onNodeClicked"/> as null for the latter so no
    /// node is clickable and no hover feedback shows, since it's "just there for a
    /// visual" per the read-only overlay's whole purpose.
    /// </summary>
    public class ActMapView
    {
        private readonly VisualElement _container;
        private readonly ActRunState _run;
        private readonly Action<int, int> _onNodeClicked;
        private bool Interactive => _onNodeClicked != null;

        /// <summary>The column for the current slot, set after <see cref="Render"/> — used
        /// by the owner to scroll it into view.</summary>
        public VisualElement CurrentSlotElement { get; private set; }

        /// <summary>Per slot, the node badges built for it (1 entry, or 2 for a fork) plus
        /// whether each is currently shown "locked" — used after layout settles to draw
        /// the connecting lines between them.</summary>
        private readonly List<(VisualElement badge, bool locked)[]> _slotBadges =
            new List<(VisualElement badge, bool locked)[]>();

        private ActMapConnectorField _connectorField;

        private static readonly Color LineColorLocked = new Color(0.55f, 0.53f, 0.55f, 0.35f);
        private static readonly Color LineColorTraveled = new Color(0.91f, 0.76f, 0.37f, 0.8f);
        private static readonly Color LineColorAhead = new Color(0.79f, 0.64f, 0.35f, 0.4f);
        private const float LineWidth = 4f;

        public ActMapView(VisualElement container, ActRunState run, Action<int, int> onNodeClicked)
        {
            _container = container;
            _run = run;
            _onNodeClicked = onNodeClicked;
        }

        public void Render()
        {
            _container.Clear();
            CurrentSlotElement = null;
            _slotBadges.Clear();

            _connectorField = new ActMapConnectorField();
            _container.Add(_connectorField);

            for (int i = 0; i < _run.Map.Slots.Count; i++)
                _container.Add(BuildSlotColumn(i));

            // Every badge just built starts out with a zero-size layout rect until the next
            // real layout pass resolves it — a fixed-delay timer here was a guess at how long
            // that takes, and guessed wrong under a slow first frame (map load, right after a
            // fight), leaving every line computed from all-zero positions. GeometryChangedEvent
            // fires the moment a badge's rect actually changes from that zero-size default, so
            // hook the very first one instead of a clock: since layout resolves the whole tree
            // in one pass, by the time any single badge reports its real size, they all have one.
            foreach (var badges in _slotBadges)
                foreach (var (badge, _) in badges)
                    badge.RegisterCallback<GeometryChangedEvent>(OnBadgeGeometryReady);
        }

        private void OnBadgeGeometryReady(GeometryChangedEvent evt)
        {
            var badge = (VisualElement)evt.target;
            badge.UnregisterCallback<GeometryChangedEvent>(OnBadgeGeometryReady);
            RecomputeConnectors();
        }

        /// <summary>Scrolls <paramref name="scrollView"/> to <see cref="CurrentSlotElement"/> as
        /// soon as it has a real layout rect, rather than guessing at a fixed delay the same
        /// way <see cref="Render"/> used to for the connector lines.</summary>
        public void ScrollToCurrentWhenReady(ScrollView scrollView)
        {
            VisualElement target = CurrentSlotElement;
            if (target == null) return;

            void OnReady(GeometryChangedEvent evt)
            {
                target.UnregisterCallback<GeometryChangedEvent>(OnReady);
                scrollView.ScrollTo(target);
            }
            target.RegisterCallback<GeometryChangedEvent>(OnReady);
        }

        private VisualElement BuildSlotColumn(int slotIndex)
        {
            ActMapSlot slot = _run.Map.Slots[slotIndex];
            var column = new VisualElement();
            column.AddToClassList("act-map-slot");

            var badges = new (VisualElement badge, bool locked)[slot.Options.Count];

            // A fork always shows both branch options side by side — the map keeps its
            // shape even after a choice is made; BuildNodeWrap is what stops the unchosen
            // option from being clickable once the fork is decided.
            if (slot.IsFork)
            {
                var (wrapA, badgeA, lockedA) = BuildNodeWrap(slot, slotIndex, 0);
                column.Add(wrapA);
                badges[0] = (badgeA, lockedA);

                var gap = new Label("OR");
                gap.AddToClassList("act-fork-gap");
                column.Add(gap);

                var (wrapB, badgeB, lockedB) = BuildNodeWrap(slot, slotIndex, 1);
                column.Add(wrapB);
                badges[1] = (badgeB, lockedB);
            }
            else
            {
                var (wrap, badge, locked) = BuildNodeWrap(slot, slotIndex, 0);
                column.Add(wrap);
                badges[0] = (badge, locked);
            }

            _slotBadges.Add(badges);

            if (slotIndex == _run.CurrentSlotIndex) CurrentSlotElement = column;
            return column;
        }

        private (VisualElement wrap, VisualElement badge, bool locked) BuildNodeWrap(
            ActMapSlot slot, int slotIndex, int optionIndex)
        {
            ActMapNode node = slot.Options[optionIndex];

            var wrap = new VisualElement();
            wrap.AddToClassList("act-node-wrap");

            var badge = new VisualElement();
            badge.AddToClassList("act-node");
            if (node.Type == ActNodeType.Elite) badge.AddToClassList("act-node-elite");
            if (node.Type == ActNodeType.Boss) badge.AddToClassList("act-node-boss");

            Sprite icon = Resources.Load<Sprite>(IconFor(node.Type));
            if (icon != null) badge.style.backgroundImage = new StyleBackground(icon);

            // isTheChosenOption: true for a non-fork slot's only option, or the specific
            // fork option that was (or already auto-was) chosen. False for -1 (undecided).
            bool isTheChosenOption = !slot.IsFork || slot.ChosenOptionIndex == optionIndex;
            // isOpenForkChoice: a genuine still-undecided fork — every option is live.
            bool isOpenForkChoice = slot.IsFork && slot.ChosenOptionIndex == -1;
            bool isPast = slotIndex < _run.CurrentSlotIndex;
            bool isCurrentSlot = slotIndex == _run.CurrentSlotIndex;
            bool locked = false;

            if (isPast)
            {
                if (isTheChosenOption)
                {
                    badge.AddToClassList("act-node-completed");
                    // Crossed off: this node has already been resolved.
                    var check = new VisualElement();
                    check.AddToClassList("act-node-check");
                    var checkMark = new Label("✓");
                    checkMark.AddToClassList("act-node-check-mark");
                    check.Add(checkMark);
                    badge.Add(check);
                }
                else
                {
                    badge.AddToClassList("act-node-locked");
                    AddLockTint(badge);
                    locked = true;
                }
            }
            else if (isCurrentSlot && (isOpenForkChoice || isTheChosenOption))
            {
                // Either a genuine open fork choice (both options are live) or this specific
                // option is the branch already locked in (a fork continuation auto-resolves
                // its branch before this ever renders — see ActRunState.CompleteCurrentNode).
                // Either way this is a node the player can act on right now.
                badge.AddToClassList("act-node-current");
                if (Interactive)
                {
                    int capturedSlot = slotIndex;
                    int capturedOption = optionIndex;
                    badge.RegisterCallback<ClickEvent>(_ => _onNodeClicked(capturedSlot, capturedOption));
                }
            }
            else
            {
                // Current slot, but this option isn't the branch already locked in — show
                // it locked rather than current so the player can't click into the road not taken.
                badge.AddToClassList("act-node-locked");
                AddLockTint(badge);
                locked = true;
            }

            if (!Interactive)
                badge.pickingMode = PickingMode.Ignore;

            wrap.Add(badge);

            var label = new Label(LabelFor(node.Type));
            label.AddToClassList("act-node-label");
            wrap.Add(label);

            return (wrap, badge, locked);
        }

        private static void AddLockTint(VisualElement badge)
        {
            var tint = new VisualElement();
            tint.AddToClassList("act-node-lock-tint");
            badge.Add(tint);
        }

        // ================================================================ //
        // Connector lines                                                     //
        // ================================================================ //

        /// <summary>
        /// Draws a line (or, at a fork boundary, two diverging/converging lines) between
        /// every pair of adjacent slots, running from one node badge's edge to the
        /// next's — rather than a fixed-width filler box that never quite reaches either
        /// node. Whether a boundary diverges, converges, runs straight in parallel, or is
        /// just a single line falls entirely out of how many options each side has, so no
        /// explicit fork bookkeeping is needed here.
        /// </summary>
        private void RecomputeConnectors()
        {
            if (_connectorField == null) return;

            var lines = new List<(Vector2 from, Vector2 to, Color color, float width)>();

            for (int i = 0; i < _slotBadges.Count - 1; i++)
            {
                var prev = _slotBadges[i];
                var next = _slotBadges[i + 1];
                bool prevResolved = _run.Map.Slots[i].Resolved;

                for (int p = 0; p < prev.Length; p++)
                {
                    for (int n = 0; n < next.Length; n++)
                    {
                        // Only connect matching branch indices when both sides have two
                        // options (a fork continuing in parallel) — otherwise every
                        // combination is a real diverge/converge line.
                        if (prev.Length == 2 && next.Length == 2 && p != n) continue;

                        AddLine(lines, prev[p].badge, next[n].badge,
                            prev[p].locked || next[n].locked, prevResolved);
                    }
                }
            }

            _connectorField.SetLines(lines);
        }

        private void AddLine(List<(Vector2, Vector2, Color, float)> lines,
            VisualElement from, VisualElement to, bool locked, bool traveled)
        {
            Vector2 centerA = _connectorField.WorldToLocal(from.worldBound.center);
            Vector2 centerB = _connectorField.WorldToLocal(to.worldBound.center);
            float radiusA = from.resolvedStyle.width * 0.5f;
            float radiusB = to.resolvedStyle.width * 0.5f;

            Vector2 direction = (centerB - centerA).normalized;
            Vector2 start = centerA + direction * radiusA;
            Vector2 end = centerB - direction * radiusB;

            Color color = locked ? LineColorLocked : traveled ? LineColorTraveled : LineColorAhead;
            lines.Add((start, end, color, LineWidth));
        }

        // ================================================================ //
        // Node type -> icon/label                                            //
        // ================================================================ //

        public static string LabelFor(ActNodeType type)
        {
            switch (type)
            {
                case ActNodeType.CharacterSelect: return "Recruit";
                case ActNodeType.RoyalDecree:      return "Royal Decree";
                case ActNodeType.Fight:            return "Fight";
                case ActNodeType.Elite:            return "Elite Fight";
                case ActNodeType.Boss:             return "Boss Fight";
                case ActNodeType.Event:            return "Event";
                case ActNodeType.SideQuest:        return "Side Quest";
                case ActNodeType.Anvil:            return "Anvil";
                case ActNodeType.Treasure:         return "Treasure";
                case ActNodeType.Village:          return "Village";
                default:                           return type.ToString();
            }
        }

        public static string IconFor(ActNodeType type)
        {
            switch (type)
            {
                case ActNodeType.CharacterSelect: return "node_characterselect_128x128";
                case ActNodeType.RoyalDecree:      return "node_decree_128x128";
                case ActNodeType.Fight:            return "node_fight_128x128";
                case ActNodeType.Elite:            return "node_elite_128x128";
                case ActNodeType.Boss:             return "node_boss_128x128";
                case ActNodeType.Event:            return "node_event_128x128";
                case ActNodeType.SideQuest:        return "node_sidequest_128x128";
                case ActNodeType.Anvil:            return "node_anvil_128x128";
                case ActNodeType.Treasure:         return "node_treasure_128x128";
                case ActNodeType.Village:          return "node_village_128x128";
                default:                           return "node_fight_128x128";
            }
        }
    }
}
