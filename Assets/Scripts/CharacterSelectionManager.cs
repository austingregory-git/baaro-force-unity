using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.Passives;
using BaaroForce.Utils;
using BaaroForce.UI;
using BaaroForce.Classes;
using BaaroForce.Spells;
using BaaroForce.GameController;

namespace BaaroForce
{
public class CharacterSelectionManager : MonoBehaviour
{
    private const string ActMapSceneName = "ActMapScene";

    // Resources-relative paths (no Inspector wiring exists for this scene, so
    // everything the UI needs is loaded by name — same convention CombatCornerMenu
    // uses for ActMap.uss).
    private const string PanelSettingsResourcePath        = "CombatHudPanelSettings";
    private const string CombatHudStyleSheetResourcePath  = "CombatHud";
    private const string ActMapStyleSheetResourcePath      = "ActMap";
    private const string SelectStyleSheetResourcePath      = "CharacterSelect";

    // Matches CharacterHudController.ZoneClasses — each card is themed by the
    // character's own realm, same as their in-combat HUD panel would be.
    private static readonly string[] ZoneClasses =
    {
        "zone-fire", "zone-water", "zone-earth", "zone-wind", "zone-light", "zone-dark"
    };

    private VisualElement _cardRow;

    void Awake()
    {
        EnsureEventSystem();
        EnsureTooltipSystem();
        SetupCamera();
        BuildUI();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Dev-only reroll: stripped from release builds by the compiler directive above,
    // so there's no risk of shipping a cheat key.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            RerollCards();
    }

    /// <summary>Clears and rebuilds the three character cards with a fresh random
    /// roll, so new stat/spell/passive combinations can be tried without restarting
    /// the scene.</summary>
    private void RerollCards()
    {
        _cardRow.Clear();
        BuildCards();
        Debug.Log("[CharacterSelectionManager] Rerolled character options.");
    }
#endif

    private void EnsureTooltipSystem()
    {
        if (TooltipSystem.Instance == null)
            new GameObject("[TooltipSystem]").AddComponent<TooltipSystem>();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // Same dark backdrop ActMapController's screen uses (.act-map-root) — a plain
    // fallback so there's no flash of a different colour before the UI Toolkit
    // panel paints its own full-screen background on the first frame.
    private static readonly Color BackdropColor = new Color(14f / 255f, 10f / 255f, 16f / 255f);

    private void SetupCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = BackdropColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    // ------------------------------------------------------------------ //
    // Screen chrome                                                        //
    // ------------------------------------------------------------------ //

    private void BuildUI()
    {
        UIDocument document = GetComponent<UIDocument>();
        if (document == null) document = gameObject.AddComponent<UIDocument>();
        if (document.panelSettings == null)
            document.panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);

        VisualElement root = document.rootVisualElement;
        AddStyleSheet(root, CombatHudStyleSheetResourcePath);
        AddStyleSheet(root, ActMapStyleSheetResourcePath);
        AddStyleSheet(root, SelectStyleSheetResourcePath);

        // .act-map-root gives the full-screen dark backdrop + centred header
        // layout — the same shell ActMapController uses one screen later.
        var screen = new VisualElement();
        screen.AddToClassList("act-map-root");
        root.Add(screen);

        var header = new VisualElement();
        header.AddToClassList("act-map-header");
        screen.Add(header);

        var title = new Label("Choose Your Champion");
        title.AddToClassList("act-map-title");
        header.Add(title);

        var subtitle = new Label("One hero steps forward to join the party.");
        subtitle.AddToClassList("act-map-subtitle");
        header.Add(subtitle);

        _cardRow = new VisualElement();
        _cardRow.AddToClassList("select-card-row");
        screen.Add(_cardRow);

        BuildCards();
    }

    private static void AddStyleSheet(VisualElement root, string resourcePath)
    {
        StyleSheet sheet = Resources.Load<StyleSheet>(resourcePath);
        if (sheet != null) root.styleSheets.Add(sheet);
        else Debug.LogWarning($"[CharacterSelectionManager] Style sheet not found in Resources: '{resourcePath}'");
    }

    // ------------------------------------------------------------------ //
    // Cards                                                                //
    // ------------------------------------------------------------------ //

    private void BuildCards()
    {
        Realm realm = PartyManager.Instance.CurrentRealm ?? Realm.Earth;
        List<Character> characters = CharacterUtils.GetRandomCharacters(3, realm, PartyManager.Instance.Party.Members);
        foreach (Character character in characters)
            _cardRow.Add(BuildCard(character));
    }

    private static VisualElement BuildCard(Character character)
    {
        var card = new VisualElement();
        card.AddToClassList("chassis");
        card.AddToClassList("select-card");
        ApplyExclusiveClass(card, ZoneClasses, $"zone-{ResolveZoneId(character)}");

        // Rivets + crest are direct children of the card itself (not the clipped
        // .select-card-content below), same as the crest sits outside .chassis as
        // a sibling in the combat HUD panel — so it can keep poking out past the
        // card's corner instead of being clipped along with overflowing content.
        card.Add(MakeRivet("rivet-tl"));
        card.Add(MakeRivet("rivet-tr"));
        card.Add(MakeRivet("rivet-bl"));
        card.Add(MakeRivet("rivet-br"));

        var crest = new VisualElement();
        crest.AddToClassList("crest");
        card.Add(crest);

        var content = new VisualElement();
        content.AddToClassList("select-card-content");
        card.Add(content);

        var portrait = new VisualElement();
        portrait.AddToClassList("portrait");
        portrait.AddToClassList("select-card-portrait");
        Sprite profileSprite = !string.IsNullOrEmpty(character.CharacterProfilePicPath)
            ? Resources.Load<Sprite>(character.CharacterProfilePicPath)
            : null;
        if (profileSprite != null)
            portrait.style.backgroundImage = new StyleBackground(profileSprite);
        else
            Debug.LogWarning($"[CharacterSelectionManager] Profile picture not found: '{character.CharacterProfilePicPath}'");
        content.Add(portrait);

        var who = new VisualElement();
        who.AddToClassList("select-card-who");
        content.Add(who);

        var nameLabel = new Label(character.CharacterName);
        nameLabel.AddToClassList("unit-name");
        nameLabel.AddToClassList("select-card-name");
        who.Add(nameLabel);

        var classLabel = new Label(character.CharacterClass?.ClassID ?? "");
        classLabel.AddToClassList("select-card-class");
        who.Add(classLabel);

        var levelBadge = new Label($"Lv {character.Level}");
        levelBadge.AddToClassList("lv");
        who.Add(levelBadge);

        var statList = new VisualElement();
        statList.AddToClassList("stat-list");
        statList.AddToClassList("select-card-stats");
        content.Add(statList);

        CharacterStats stats = character.CharacterStats;
        // Pre-battle stats are always at full — the bars/pips render entirely
        // filled, same visual language as a topped-up combat HUD panel.
        statList.Add(BuildBarStat("stat-badge-hp", stats.MaxHealthPoints, "seg-fill-hp"));
        statList.Add(BuildBarStat("stat-badge-mana", stats.MaxMana, "seg-fill-mana"));
        statList.Add(BuildPipStat(stats.Movement));
        statList.Add(BuildAttackStat(character.CharacterClass, stats.TotalAttack));

        // Three separately-labelled groups rather than one undifferentiated list —
        // Passive (this character's own starting passive), Character Spell (their
        // one personal kit spell), Class Spell (the one random spell rolled from
        // their class).
        // Character/Class spell chips get a distinct tint too (see .ability-chip-spell
        // / .ability-chip-class-spell) so the categories still read apart at a glance.
        var passiveChips = new List<VisualElement>();
        foreach (PassiveAbility passive in character.CharacterPassiveAbilities)
            passiveChips.Add(BuildAbilityChip(passive.Name,
                passive.GetSummary(character), passive.GetDetailedDescription(character), null));

        var characterSpellChips = new List<VisualElement>();
        var classSpellChips = new List<VisualElement>();
        foreach (Spell spell in character.CharacterSpells)
        {
            string tintClass = spell is ClassSpell ? "ability-chip-class-spell" : "ability-chip-spell";
            VisualElement chip = BuildAbilityChip(spell.Name,
                spell.GetSummary(character), spell.GetDetailedDescription(character), tintClass);
            (spell is ClassSpell ? classSpellChips : characterSpellChips).Add(chip);
        }

        AddAbilitySection(content, "Passive", passiveChips);
        AddAbilitySection(content, "Character Spell", characterSpellChips);
        AddAbilitySection(content, "Class Spell", classSpellChips);

        // Whole card is the click target — ClickEvent bubbles up from any child
        // (portrait, stats, ability chips, ...), same pattern act-choice-row and
        // the shop/recruit rows already rely on elsewhere in the Act Map UI.
        card.RegisterCallback<ClickEvent>(_ =>
        {
            PartyManager.Instance.AddMember(character);
            // This card resolves whichever CharacterSelect node the Act Map is
            // currently on (node 0, 2, or 12) — the map decides what comes next.
            PartyManager.Instance.ActRun.CompleteCurrentNode();
            SceneManager.LoadScene(ActMapSceneName);
        });

        return card;
    }

    // Pill shape lives on a plain VisualElement wrapper, not the Label itself —
    // a Label auto-sized by its own padding kept rendering its (bold, or even
    // plain) text wider than the box Yoga measured for it, spilling text past
    // the pill's edges on anything longer than a word or two. A wrapper whose
    // size just follows its child's ordinary flex content size sidesteps that
    // Label-measures-its-own-padding quirk entirely.
    /// <summary>internal, not private — reused by CharacterInspectUI's spell/passive section.</summary>
    internal static VisualElement BuildAbilityChip(string abilityName, string summaryBody, string detailedBody, string tintClass)
    {
        var chip = new VisualElement();
        chip.AddToClassList("ability-chip");
        if (tintClass != null) chip.AddToClassList(tintClass);

        var label = new Label(abilityName);
        label.AddToClassList("ability-chip-label");
        chip.Add(label);

        chip.RegisterCallback<PointerEnterEvent>(_ => TooltipSystem.Instance?.Show(abilityName, summaryBody, detailedBody));
        chip.RegisterCallback<PointerLeaveEvent>(_ => TooltipSystem.Instance?.Hide());
        return chip;
    }

    /// <summary>Adds a labelled ability-category group (heading + wrapped chip row) to
    /// the card, or nothing at all when the category is empty — a character with no
    /// personal spells shouldn't show an empty "Spells" heading.</summary>
    /// <summary>internal, not private — reused by CharacterInspectUI's spell/passive section.</summary>
    internal static void AddAbilitySection(VisualElement content, string label, List<VisualElement> chips)
    {
        if (chips.Count == 0) return;

        var section = new VisualElement();
        section.AddToClassList("select-card-ability-section");
        content.Add(section);

        var heading = new Label(label);
        heading.AddToClassList("select-card-ability-heading");
        section.Add(heading);

        var row = new VisualElement();
        row.AddToClassList("select-card-abilities");
        section.Add(row);

        foreach (VisualElement chip in chips) row.Add(chip);
    }

    // ------------------------------------------------------------------ //
    // Stat rows — reuse CombatHud.uss's stat-badge / seg-bar / pips        //
    // classes directly so the icons and fills are pixel-identical to the  //
    // in-combat HUD panel.                                                //
    // ------------------------------------------------------------------ //

    /// <summary>internal, not private — reused by CharacterInspectUI's stats column.</summary>
    internal static VisualElement BuildStatRow(string badgeClass)
    {
        var row = new VisualElement();
        row.AddToClassList("stat-row");
        var badge = new VisualElement();
        badge.AddToClassList("stat-badge");
        badge.AddToClassList(badgeClass);
        row.Add(badge);
        return row;
    }

    private static VisualElement BuildBarStat(string badgeClass, int value, string fillClass)
    {
        VisualElement row = BuildStatRow(badgeClass);
        var bar = new VisualElement();
        bar.AddToClassList("seg-bar");
        row.Add(bar);
        SetSegmentedBar(bar, value, value, fillClass);
        var num = new Label(value.ToString());
        num.AddToClassList("stat-num");
        row.Add(num);
        return row;
    }

    private static VisualElement BuildPipStat(int movement)
    {
        VisualElement row = BuildStatRow("stat-badge-move");
        var pips = new VisualElement();
        pips.AddToClassList("pips");
        row.Add(pips);
        SetPips(pips, movement, movement);
        var num = new Label(movement.ToString());
        num.AddToClassList("stat-num");
        row.Add(num);
        return row;
    }

    /// <summary>internal, not private — reused by CharacterInspectUI's stats column.</summary>
    internal static VisualElement BuildAttackStat(CharacterClass characterClass, int totalAttack)
    {
        VisualElement row = BuildStatRow(WeaponBadgeClass(characterClass));
        var num = new Label(totalAttack.ToString());
        num.AddToClassList("stat-num");
        row.Add(num);
        return row;
    }

    private static string WeaponBadgeClass(CharacterClass characterClass)
    {
        switch (characterClass?.Specialty)
        {
            case CharacterClass.ClassSpecialty.Ranged: return "weapon-ranged";
            case CharacterClass.ClassSpecialty.Magic:  return "weapon-magic";
            default:                                    return "weapon-melee";
        }
    }

    /// <summary>Rebuilds a segmented bar (children = individual segment divs) to show
    /// filled/total. Mirrors CharacterHudController.SetSegmentedBar.</summary>
    private static void SetSegmentedBar(VisualElement bar, int filled, int total, string fillClass)
    {
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

    /// <summary>Rebuilds the diamond movement pips to show filled/total. Mirrors
    /// CharacterHudController.SetPips.</summary>
    private static void SetPips(VisualElement pips, int filled, int total)
    {
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

    // ------------------------------------------------------------------ //
    // Small shared helpers — mirror ActMapController / CombatCornerMenu's //
    // own copies of the same chassis/zone helpers.                        //
    // ------------------------------------------------------------------ //

    private static VisualElement MakeRivet(string variantClass)
    {
        var rivet = new VisualElement();
        rivet.AddToClassList("rivet");
        rivet.AddToClassList(variantClass);
        return rivet;
    }

    private static string ResolveZoneId(Character character)
    {
        List<Realm> realms = character.CharacterRealms;
        if (realms != null && realms.Count > 0)
            return realms[0].ToString().ToLowerInvariant();
        return "earth";
    }

    private static void ApplyExclusiveClass(VisualElement el, string[] allClasses, string activeClass)
    {
        foreach (var c in allClasses) el.RemoveFromClassList(c);
        el.AddToClassList(activeClass);
    }

    /// <summary>
    /// The realm's pale background colour. No longer used for the card backdrop itself
    /// (the select screen now uses the same dark chassis backdrop as the rest of the UI —
    /// see BuildUI), but kept here as the shared palette source: public so
    /// BaaroForce.UI.CombatTextColors can reuse the exact same colours for floating
    /// combat-text damage colours.
    /// </summary>
    public static Color GetRealmBackgroundColor(Realm? realm)
    {
        if (!realm.HasValue) return Color.white;
        switch (realm.Value)
        {
            case Realm.Dark:  return new Color(0.55f, 0.55f, 0.58f); // gray
            case Realm.Light: return new Color(1.00f, 0.98f, 0.85f); // pale yellow
            case Realm.Earth: return new Color(0.82f, 0.95f, 0.77f); // light green
            case Realm.Wind:  return new Color(0.96f, 0.96f, 0.94f); // eggshell white
            case Realm.Fire:  return new Color(1.00f, 0.86f, 0.68f); // light orange
            case Realm.Water: return new Color(0.73f, 0.90f, 1.00f); // light blue
            default:                                return Color.white;
        }
    }
}
}
