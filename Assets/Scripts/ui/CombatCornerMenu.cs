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
    /// already covers that).
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

            BuildMapOverlay();
            BuildModalShell();
            _inventoryPanel = new InventoryPanel(_modalContent, OpenModal, CloseModal);

            _root.Add(MakeMapButton(ToggleMap));
            _root.Add(MakeInventoryButton(() => _inventoryPanel.Open()));
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
