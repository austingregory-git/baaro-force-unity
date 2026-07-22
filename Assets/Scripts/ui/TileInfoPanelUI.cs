using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.UI
{
    /// <summary>
    /// Bottom-right Combat HUD panel shown when a tile is selected for inspection (see
    /// TurnManager.SetInspectedTile) — terrain name/description/effects, plus a compact
    /// unit-info row when the tile is occupied. Built the same way as ActionPanelUI (procedural
    /// VisualElements added to the scene's existing UIDocument, chassis/rivet chrome), reusing
    /// CombatHud.uss's existing status-chip/seg-bar vocabulary rather than inventing new.
    /// </summary>
    public class TileInfoPanelUI : MonoBehaviour
    {
        private VisualElement _panel;
        private Label _nameLabel;
        private Label _descLabel;
        private VisualElement _chipsRow;
        private VisualElement _divider;
        private VisualElement _unitRow;
        private Label _unitNameLabel;
        private Label _unitTagLabel;
        private VisualElement _hpRow;
        private VisualElement _hpBar;
        private Label _hpNumLabel;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()     => BuildPanel();
        private void OnDestroy() => _panel?.RemoveFromHierarchy();

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildPanel()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[TileInfoPanelUI] No UIDocument found in scene.");
                return;
            }

            _panel = new VisualElement();
            _panel.AddToClassList("tile-info-panel");
            _panel.style.display = DisplayStyle.None;

            var chassis = new VisualElement();
            chassis.AddToClassList("chassis");
            chassis.AddToClassList("tile-info-chassis");
            _panel.Add(chassis);

            chassis.Add(MakeRivet("rivet-tl"));
            chassis.Add(MakeRivet("rivet-tr"));
            chassis.Add(MakeRivet("rivet-bl"));
            chassis.Add(MakeRivet("rivet-br"));

            _nameLabel = new Label();
            _nameLabel.AddToClassList("tile-info-name");
            chassis.Add(_nameLabel);

            _descLabel = new Label();
            _descLabel.AddToClassList("tile-info-desc");
            chassis.Add(_descLabel);

            _chipsRow = new VisualElement();
            _chipsRow.AddToClassList("tile-info-chips");
            chassis.Add(_chipsRow);

            _divider = new VisualElement();
            _divider.AddToClassList("tile-info-divider");
            chassis.Add(_divider);

            _unitRow = new VisualElement();
            _unitRow.AddToClassList("tile-info-unit-row");
            _unitNameLabel = new Label();
            _unitNameLabel.AddToClassList("tile-info-unit-name");
            _unitTagLabel = new Label();
            _unitTagLabel.AddToClassList("tile-info-unit-tag");
            _unitRow.Add(_unitNameLabel);
            _unitRow.Add(_unitTagLabel);
            chassis.Add(_unitRow);

            _hpRow = new VisualElement();
            _hpRow.AddToClassList("tile-info-hp-row");
            _hpBar = new VisualElement();
            _hpBar.AddToClassList("seg-bar");
            _hpBar.AddToClassList("tile-info-hp-bar");
            _hpNumLabel = new Label();
            _hpNumLabel.AddToClassList("tile-info-hp-num");
            _hpRow.Add(_hpBar);
            _hpRow.Add(_hpNumLabel);
            chassis.Add(_hpRow);

            doc.rootVisualElement.Add(_panel);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Rebuilds and shows the panel for <paramref name="tile"/>.</summary>
        public void Show(MapTile tile)
        {
            if (_panel == null || tile == null) return;

            TerrainInfo info = TerrainInfoRegistry.Get(tile.TerrainType);
            _nameLabel.text = info.DisplayName;
            _descLabel.text = info.Description;

            _chipsRow.Clear();
            if (info.MovementCost != 1)
                _chipsRow.Add(MakeChip($"Movement Cost {info.MovementCost}", "status-chip-debuff"));
            if (info.RegenPerTurn != 0)
                _chipsRow.Add(MakeChip($"Regen +{info.RegenPerTurn}/turn", "status-chip-buff"));
            _chipsRow.style.display = _chipsRow.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            Character unit = tile.OccupyingUnit;
            bool occupied = unit != null;
            _divider.style.display = occupied ? DisplayStyle.Flex : DisplayStyle.None;
            _unitRow.style.display = occupied ? DisplayStyle.Flex : DisplayStyle.None;
            _hpRow.style.display   = occupied ? DisplayStyle.Flex : DisplayStyle.None;

            if (occupied)
            {
                _unitNameLabel.text = unit.CharacterName;
                _unitTagLabel.text  = $"Lv {unit.Level} · {ResolveTag(unit)}";

                int max = Mathf.Max(1, unit.CharacterStats.MaxHealthPoints);
                int cur = Mathf.Clamp(unit.CharacterStats.HealthPoints, 0, max);
                SetHpBar(max, cur);
                _hpNumLabel.text = $"{cur}/{max}";
            }

            _panel.style.display = DisplayStyle.Flex;
        }

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            if (_panel != null) _panel.style.display = DisplayStyle.None;
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private static string ResolveTag(Character unit) =>
            unit is Npc npc ? npc.Specialty.ToString() : (unit.CharacterClass?.ClassID ?? "—");

        private void SetHpBar(int max, int filled)
        {
            _hpBar.Clear();
            for (int i = 0; i < max; i++)
            {
                var seg = new VisualElement();
                seg.AddToClassList("seg");
                if (i < filled) seg.AddToClassList("seg-fill-hp");
                _hpBar.Add(seg);
            }
        }

        private static Label MakeChip(string text, string variantClass)
        {
            var chip = new Label(text);
            chip.AddToClassList("status-chip");
            chip.AddToClassList(variantClass);
            return chip;
        }
    }
}
