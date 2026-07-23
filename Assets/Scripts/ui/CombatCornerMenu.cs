using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.ActMap;
using BaaroForce.GameController;

namespace BaaroForce.UI
{
    /// <summary>
    /// Bottom-right corner icons for the combat scene: Inventory (previously Act Map
    /// screen only) and a new Map icon that pops a read-only, full-screen glance at the
    /// Act Map — same look as the real Act Map screen, but every node is non-clickable
    /// (see <see cref="ActMapView"/>'s Interactive flag), since mid-fight it's purely a
    /// "where am I in the run" reference. Also bound to the 'M' key (TurnManager no
    /// longer treats M as a Move-mode alias — see TurnManager.HandleKeys — since W
    /// already covers that). Also owns the top-right Gold readout (reusing ActMap.uss's
    /// .act-map-gold look) so the combat scene has one too — both it and the Inventory
    /// button double as landing targets for FightResultUI's claim-flight animation.
    ///
    /// Added by TurnManager.Initialize() alongside the other combat UI components
    /// (ActionPanelUI, WarningToastUI, ...); reuses whichever UIDocument is already in
    /// the scene, same convention as those.
    /// </summary>
    public class CombatCornerMenu : MonoBehaviour
    {
        /// <summary>True while either the read-only map overlay or the Inventory modal is
        /// showing — TurnManager checks this to freeze battle input underneath.</summary>
        public static bool IsBlockingCombatInput { get; private set; }

        private VisualElement _root;

        private VisualElement _mapOverlay;
        private ScrollView _mapScroll;
        private VisualElement _mapPath;
        private ActMapView _mapView;
        private bool _mapOpen;

        private VisualElement _modalOverlay;
        private VisualElement _modalChassis;
        private VisualElement _modalContent;
        private Label _modalTitle;
        private InventoryPanel _inventoryPanel;
        private bool _modalOpen;

        private VisualElement _inventoryButton;
        private Label _goldLabel;
        private int _displayedGold;
        private IVisualElementScheduledItem _goldCountAnim;

        /// <summary>The Inventory button's own element — used as the "fly to" landing spot for
        /// claimed equipment/potions in FightResultUI's claim animation.</summary>
        public VisualElement InventoryTarget => _inventoryButton;

        /// <summary>The top-right Gold readout's element — used as the "fly to" landing spot
        /// for claimed gold in FightResultUI's claim animation.</summary>
        public VisualElement GoldTarget => _goldLabel;

        private void Awake()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[CombatCornerMenu] No UIDocument found in scene.");
                return;
            }
            _root = doc.rootVisualElement;

            StyleSheet actMapStyles = Resources.Load<StyleSheet>("ActMap");
            if (actMapStyles != null) _root.styleSheets.Add(actMapStyles);

            // TurnManager.Initialize() already adds one to this same GameObject before adding
            // CombatCornerMenu; fall back to adding our own only if that's somehow not true
            // (e.g. this component used standalone outside the usual combat scene wiring).
            WarningToastUI warningToast = GetComponent<WarningToastUI>() ?? gameObject.AddComponent<WarningToastUI>();

            BuildMapOverlay();
            BuildModalShell();
            _inventoryPanel = new InventoryPanel(_modalContent, OpenModal, CloseModal, warningToast.Show);

            _goldLabel = new Label();
            _goldLabel.AddToClassList("act-map-gold");
            _root.Add(_goldLabel);
            RefreshGold();

            _inventoryButton = MakeInventoryButton(() => _inventoryPanel.Open());
            _root.Add(MakeMapButton(ToggleMap));
            _root.Add(_inventoryButton);
        }

        /// <summary>Snaps the top-right Gold readout straight to the party's current total, no
        /// animation — for setup (Awake) and anything else that isn't a claimed-gold moment.</summary>
        public void RefreshGold()
        {
            _goldCountAnim?.Pause();
            _displayedGold = PartyManager.Instance.Party.Gold;
            if (_goldLabel != null) _goldLabel.text = $"{_displayedGold} Gold";
        }

        /// <summary>Counts the Gold readout up from whatever it's currently showing to the
        /// party's new total (see TurnManager.ClaimLoot), with a quick scale bounce timed to
        /// the count so the number visibly "lands" rather than just snapping to the new value.
        /// Safe to call again mid-animation — restarts from whatever's currently displayed.</summary>
        public void AnimateGoldGain()
        {
            if (_goldLabel == null) return;

            int from = _displayedGold;
            int to = PartyManager.Instance.Party.Gold;
            if (to <= from) { RefreshGold(); return; }

            _goldCountAnim?.Pause();

            const int stepMs = 25;
            const int durationMs = 550;
            int totalSteps = Mathf.Max(1, durationMs / stepMs);
            int step = 0;

            IVisualElementScheduledItem anim = null;
            anim = _goldLabel.schedule.Execute(() =>
            {
                step++;
                float t = Mathf.Clamp01((float)step / totalSteps);

                _displayedGold = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                _goldLabel.text = $"{_displayedGold} Gold";

                // A single bounce envelope across the whole count-up — zero at both ends,
                // peaking mid-flight — rather than a discrete pulse per tick, so it reads as
                // one springy motion instead of a jittery shake.
                float bounce = Mathf.Sin(t * Mathf.PI) * 0.22f;
                _goldLabel.style.scale = new Scale(Vector3.one * (1f + bounce));

                if (t >= 1f)
                {
                    anim.Pause();
                    _displayedGold = to;
                    _goldLabel.text = $"{to} Gold";
                    _goldLabel.style.scale = new Scale(Vector3.one);
                }
            }).Every(stepMs);
            _goldCountAnim = anim;
        }

        private void OnDestroy() => IsBlockingCombatInput = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) ToggleMap();
        }

        // ================================================================ //
        // Corner buttons                                                      //
        // ================================================================ //

        private static VisualElement MakeInventoryButton(System.Action onClick)
        {
            var button = new VisualElement();
            button.AddToClassList("act-inventory-btn");
            button.RegisterCallback<ClickEvent>(_ => onClick());

            var flap = new VisualElement();
            flap.AddToClassList("bag-icon-flap");
            button.Add(flap);

            var body = new VisualElement();
            body.AddToClassList("bag-icon-body");
            button.Add(body);

            return button;
        }

        /// <summary>Plain geometric node-path glyph (two dots + a connecting bar) — same
        /// "no sprite assets yet" convention as the bag icon; swap in a real map/compass
        /// sprite later by setting backgroundImage on the returned element instead.</summary>
        private static VisualElement MakeMapButton(System.Action onClick)
        {
            var button = new VisualElement();
            button.AddToClassList("act-map-btn");
            button.RegisterCallback<ClickEvent>(_ => onClick());

            var wrap = new VisualElement();
            wrap.AddToClassList("map-icon-wrap");
            button.Add(wrap);

            var line = new VisualElement();
            line.AddToClassList("map-icon-line");
            wrap.Add(line);

            var nodeA = new VisualElement();
            nodeA.AddToClassList("map-icon-node");
            nodeA.AddToClassList("map-icon-node-a");
            wrap.Add(nodeA);

            var nodeB = new VisualElement();
            nodeB.AddToClassList("map-icon-node");
            nodeB.AddToClassList("map-icon-node-b");
            wrap.Add(nodeB);

            return button;
        }

        // ================================================================ //
        // Read-only map overlay                                               //
        // ================================================================ //

        private void BuildMapOverlay()
        {
            _mapOverlay = new VisualElement();
            _mapOverlay.AddToClassList("act-map-root");
            _mapOverlay.AddToClassList("combat-map-overlay");
            _mapOverlay.style.display = DisplayStyle.None;
            _root.Add(_mapOverlay);

            var header = new VisualElement();
            header.AddToClassList("act-map-header");
            _mapOverlay.Add(header);

            var title = new Label("Act 1");
            title.AddToClassList("act-map-title");
            header.Add(title);

            var subtitle = new Label("Your progress so far.");
            subtitle.AddToClassList("act-map-subtitle");
            header.Add(subtitle);

            var hint = new Label("(Press M to close)");
            hint.AddToClassList("combat-map-hint");
            header.Add(hint);

            _mapScroll = new ScrollView(ScrollViewMode.Vertical);
            _mapScroll.AddToClassList("act-map-scroll");
            _mapScroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _mapScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _mapOverlay.Add(_mapScroll);

            _mapPath = new VisualElement();
            _mapPath.AddToClassList("act-map-path");
            _mapScroll.Add(_mapPath);

            // onNodeClicked: null — every node renders non-interactive, purely a glance.
            _mapView = new ActMapView(_mapPath, PartyManager.Instance.ActRun, null);
        }

        private void ToggleMap()
        {
            _mapOpen = !_mapOpen;
            _mapOverlay.style.display = _mapOpen ? DisplayStyle.Flex : DisplayStyle.None;

            if (_mapOpen)
            {
                _mapView.Render();
                _mapView.ScrollToCurrentWhenReady(_mapScroll);
            }

            RefreshBlockingState();
        }

        // ================================================================ //
        // Inventory modal shell (mirrors ActMapController's)                  //
        // ================================================================ //

        private void BuildModalShell()
        {
            _modalOverlay = new VisualElement();
            _modalOverlay.AddToClassList("fight-result-overlay");
            _modalOverlay.style.display = DisplayStyle.None;
            _root.Add(_modalOverlay);

            _modalChassis = new VisualElement();
            _modalChassis.AddToClassList("chassis");
            _modalChassis.AddToClassList("fight-result-chassis");
            _modalChassis.AddToClassList("act-modal-wide");
            _modalOverlay.Add(_modalChassis);

            _modalChassis.Add(MakeRivet("rivet-tl"));
            _modalChassis.Add(MakeRivet("rivet-tr"));
            _modalChassis.Add(MakeRivet("rivet-bl"));
            _modalChassis.Add(MakeRivet("rivet-br"));

            _modalTitle = new Label();
            _modalTitle.AddToClassList("fight-result-title");
            _modalTitle.AddToClassList("fight-result-title-win");
            _modalChassis.Add(_modalTitle);

            _modalContent = new VisualElement();
            _modalChassis.Add(_modalContent);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        private void OpenModal(string title, bool wide)
        {
            _modalTitle.text = title;
            _modalContent.Clear();
            _modalChassis.EnableInClassList("act-modal-inventory", wide);
            _modalOverlay.style.display = DisplayStyle.Flex;
            _modalOpen = true;
            RefreshBlockingState();
        }

        private void CloseModal()
        {
            _modalOverlay.style.display = DisplayStyle.None;
            _modalOpen = false;
            RefreshBlockingState();
        }

        private void RefreshBlockingState() => IsBlockingCombatInput = _mapOpen || _modalOpen;
    }
}
