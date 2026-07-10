using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BaaroForce.Characters;
using BaaroForce.Passives;
using BaaroForce.Utils;
using BaaroForce.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    // ── Card display dimensions ────────────────────────────────────────────
    // The template sprite is 512×768 px (2:3 ratio).  CardWidth×CardHeight
    // displays it at exactly half its native resolution on the 1280×720 canvas.
    // Change CardWidth / CardHeight together (keeping the 2:3 ratio) to resize
    // all cards at once without touching any anchor or padding values.
    private const float CardWidth  = 256f;
    private const float CardHeight = 384f;

    // Horizontal distance from the canvas centre to each card's anchor point.
    // The three cards sit at –CardOffset, 0, and +CardOffset on the X axis.
    private const float CardOffset = 360f;

    // ── Character portrait ─────────────────────────────────────────────────
    // Portrait width is ~56 % of CardWidth.  PortraitHeight = PortraitWidth × 1.5
    // to preserve the 2:3 aspect ratio of the 512×768 portrait png.
    // If you change PortraitWidth, set PortraitHeight = PortraitWidth × 1.5.
    private const float PortraitWidth       = 144f;
    private const float PortraitHeight      = 216f;

    // Fraction of CardHeight from the card's top edge to the portrait's centre.
    // 0.50 = portrait centred vertically on the card.
    // Increase to push the portrait lower; decrease to raise it.
    private const float PortraitDownPercent = 0.50f;

    // ── Corner stat icons ──────────────────────────────────────────────────
    // All four corners share the same square icon for now (health_128x128.png).
    // Change CornerIconSize to resize all four icons uniformly without touching
    // any anchor or padding values.
    private const float  CornerIconSize      = 48f;
    private const string HealthSpritePath = "health_128x128";
    private const string MeleeAttackSpritePath = "melee_attack_128x128";
    private const string RangedAttackSpritePath = "ranged_attack_128x128";
    private const string MagicAttackSpritePath = "magic_attack_128x128";
    private const string ManaSpritePath = "mana_128x128";
    private const string MovementSpritePath = "movement_128x128";

    // ── Sprite paths ───────────────────────────────────────────────────────
    private const string CharacterTemplateSpritePath = "card_template_512x768";

    // Render textures created for model previews — released on scene unload.
    private readonly List<RenderTexture> _previewTextures = new List<RenderTexture>();

    private void OnDestroy()
    {
        foreach (var rt in _previewTextures)
            if (rt != null) rt.Release();
    }

    void Awake()
    {
        EnsureEventSystem();
        EnsureTooltipSystem();
        SetupCamera();
        GameObject canvasObj = CreateCanvas();
        CreateBackground(canvasObj.transform);
        CreateCards(canvasObj.transform);
    }

    private void EnsureTooltipSystem()
    {
        if (TooltipSystem.Instance == null)
            new GameObject("[TooltipSystem]").AddComponent<TooltipSystem>();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private void SetupCamera()
    {
        if (Camera.main != null)
        {
            // Match the camera clear colour to the realm background so that any
            // transparent gap in the Canvas — such as the portrait window before the
            // RenderTexture is ready, or the card template's transparent interior —
            // blends against the realm colour instead of white.
            Camera.main.backgroundColor = GetRealmBackgroundColor(PartyManager.Instance.CurrentRealm);
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    private GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("CharacterSelectionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // 1280×720 reference resolution gives the 256×384 card art room to breathe
        // and matches standard HD proportions.  All unit values in this file are
        // expressed in these reference pixels — change this to rescale everything.
        scaler.referenceResolution = new Vector2(1280, 720);
        // 0.5 = blend equally between width-based and height-based scaling so the
        // layout adapts sensibly to both landscape and near-square screens.
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    private void CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = GetRealmBackgroundColor(PartyManager.Instance.CurrentRealm);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
    }

    private void CreateCards(Transform parent)
    {
        Vector2[] positions = new Vector2[]
        {
            new Vector2(-CardOffset, 0f),
            new Vector2(0f, 0f),
            new Vector2(CardOffset, 0f)
        };

        Realm realm = PartyManager.Instance.CurrentRealm ?? Realm.EARTH;
        List<Character> characters = CharacterUtils.GetRandomCharacters(3, realm);

        var previewer = new CharacterPreviewRenderer(GetRealmBackgroundColor(realm));
        for (int i = 0; i < 3; i++)
        {
            RenderTexture rt = previewer.Capture(characters[i].characterModelPath);
            if (rt != null) _previewTextures.Add(rt);
            CreateCard(parent, positions[i], i, characters[i], rt);
        }
        previewer.Cleanup();
    }

    private void CreateCard(Transform parent, Vector2 anchoredPos, int index, Character character, RenderTexture previewTexture)
    {
        // The card root carries no Graphic of its own — it is a pure layout container.
        // Canvas sibling order drives draw order: sibling 0 is drawn first (furthest back),
        // higher siblings are drawn on top.  We exploit this to layer the portrait behind
        // the card template overlay.
        GameObject card = new GameObject("CharacterCard_" + index);
        card.transform.SetParent(parent, false);

        // AddComponent<RectTransform> replaces the plain Transform that a new GameObject
        // starts with.  Without this, GetComponent<RectTransform>() returns null because
        // no UI component was added to the root to trigger the automatic upgrade.
        RectTransform cardRect = card.AddComponent<RectTransform>();
        // Anchor collapsed to a single centre point so anchoredPosition is a direct
        // offset from the canvas centre.  sizeDelta then gives the explicit 256×384 size.
        cardRect.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = anchoredPos;
        // Set size before children are created so CreatePortrait can read sizeDelta.y.
        // The template sprite is 512×768 px; setting this explicitly avoids the card
        // overflowing the 1280×720 canvas if SetNativeSize() were called instead.
        cardRect.sizeDelta        = new Vector2(CardWidth, CardHeight);

        // ── Sibling 0: realm-colour fill ───────────────────────────────────
        // Covers the entire card so that any transparent gap in the template
        // (above the roof peak, rounded corners, etc.) shows the realm colour
        // instead of whatever the camera or canvas has behind it.
        // This is a plain opaque Image — no sprite, just a colour fill.
        GameObject cardBg          = new GameObject("CardBackground");
        cardBg.transform.SetParent(card.transform, false);
        Image cardBgImage          = cardBg.AddComponent<Image>();
        cardBgImage.color          = GetRealmBackgroundColor(PartyManager.Instance.CurrentRealm);
        RectTransform cardBgRect   = cardBg.GetComponent<RectTransform>();
        // anchorMin (0,0) + anchorMax (1,1) + sizeDelta (0,0) = full card fill.
        cardBgRect.anchorMin        = Vector2.zero;
        cardBgRect.anchorMax        = Vector2.one;
        cardBgRect.sizeDelta        = Vector2.zero;
        cardBgRect.anchoredPosition = Vector2.zero;

        // ── Sibling 1: portrait (renders over the fill, behind the template) ─
        CreatePortrait(card.transform, cardRect.sizeDelta.y, previewTexture);

        // ── Sibling 2: card template overlay (renders IN FRONT of portrait) ─
        // The template png should be transparent in the portrait window area so
        // the 3-D preview shows through.
        Sprite templateSprite = Resources.Load<Sprite>(CharacterTemplateSpritePath);
        GameObject overlayObj  = new GameObject("CardOverlay");
        overlayObj.transform.SetParent(card.transform, false);
        Image cardImage          = overlayObj.AddComponent<Image>();
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        // anchorMin (0,0) + anchorMax (1,1) + sizeDelta (0,0) stretches the overlay
        // to fill the card container exactly, matching CardWidth × CardHeight.
        overlayRect.anchorMin        = Vector2.zero;
        overlayRect.anchorMax        = Vector2.one;
        overlayRect.sizeDelta        = Vector2.zero;
        overlayRect.anchoredPosition = Vector2.zero;
        if (templateSprite != null)
        {
            cardImage.sprite = templateSprite;
            cardImage.type   = Image.Type.Simple;
        }
        else
        {
            // Fallback: semi-transparent grey card when the sprite is missing.
            cardImage.color = new Color(0.85f, 0.85f, 0.85f, 0.5f);
        }

        // ── Siblings 3+: stat icons / labels (render ON TOP of everything) ──
        AddCardStats(card.transform, cardRect.sizeDelta, character);

        CharacterCardHandler handler = card.AddComponent<CharacterCardHandler>();
        handler.Initialize(character);
    }

    private static Color GetRealmBackgroundColor(Realm? realm)
    {
        if (!realm.HasValue) return Color.white;
        switch (realm.Value)
        {
            case Realm.DARK:  return new Color(0.55f, 0.55f, 0.58f); // gray
            case Realm.LIGHT: return new Color(1.00f, 0.98f, 0.85f); // pale yellow
            case Realm.EARTH: return new Color(0.82f, 0.95f, 0.77f); // light green
            case Realm.WIND:  return new Color(0.96f, 0.96f, 0.94f); // eggshell white
            case Realm.FIRE:  return new Color(1.00f, 0.86f, 0.68f); // light orange
            case Realm.WATER: return new Color(0.73f, 0.90f, 1.00f); // light blue
            default:                                return Color.white;
        }
    }

    private void CreatePortrait(Transform cardParent, float cardHeight, RenderTexture previewTexture)
    {
        GameObject portrait = new GameObject("CharacterPortrait");
        portrait.transform.SetParent(cardParent, false);

        if (previewTexture != null)
        {
            RawImage rawImg = portrait.AddComponent<RawImage>();
            rawImg.texture = previewTexture;
        }
        else
        {
            Image img = portrait.AddComponent<Image>();
            img.color = new Color(0.4f, 0.6f, 0.8f);
        }

        RectTransform portraitRect = portrait.GetComponent<RectTransform>();
        // anchorMin == anchorMax == (0.5, 1) collapses the anchor to a single
        // point at the top-centre of the card.  anchoredPosition Y is therefore
        // measured straight downward (negative) from that top-centre edge.
        portraitRect.anchorMin = new Vector2(0.5f, 1f);
        portraitRect.anchorMax = new Vector2(0.5f, 1f);
        // pivot (0.5, 0.5) makes anchoredPosition point to the portrait's centre,
        // so PortraitDownPercent directly expresses where that centre sits.
        portraitRect.pivot     = new Vector2(0.5f, 0.5f);
        // PortraitWidth × PortraitHeight preserves the 2:3 aspect ratio of the
        // 512×768 portrait png.  To resize, change PortraitWidth and set
        // PortraitHeight = PortraitWidth × 1.5 to keep the ratio.
        portraitRect.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
        // Portrait centre Y = –(cardHeight × PortraitDownPercent) from the card top.
        // With PortraitDownPercent = 0.50 the centre lands exactly in the middle
        // of the card.  Increase the constant to push the portrait lower.
        portraitRect.anchoredPosition = new Vector2(0f, -(cardHeight * PortraitDownPercent));
    }

    private void AddCardStats(Transform cardParent, Vector2 cardSize, Character character)
    {
        float textH   = -48f;  // height of the stat-value label stacked above/below the icon
        float labelH  = 28f;  // height of the character-name label at the top of the card
        // Name label takes 54 % of card width so it fits between the two top corner icons.
        float nameW   = cardSize.x * 0.54f;

        // Load the shared corner icon once; all four corners reuse the same sprite for now.
        Sprite healthIcon = Resources.Load<Sprite>(HealthSpritePath);
        Sprite meleeAttackIcon = Resources.Load<Sprite>(MeleeAttackSpritePath);
        Sprite rangedAttackIcon = Resources.Load<Sprite>(RangedAttackSpritePath);
        Sprite magicAttackIcon = Resources.Load<Sprite>(MagicAttackSpritePath);
        Sprite manaIcon = Resources.Load<Sprite>(ManaSpritePath);
        Sprite movementIcon = Resources.Load<Sprite>(MovementSpritePath);

        // ── Top-left: HP ───────────────────────────────────────────────────
        // anchor (0,1) = top-left corner of the card.
        // Icon offset (+pad, –pad): moves RIGHT and DOWN away from the corner.
        // Value text stacked BELOW the icon at the same left edge
        // (Y = –(pad + CornerIconSize + iconGap) pushes it further down).
        float health_pad_x = -16f;
        float health_pad_y = 64f;
        CreateCornerStat(cardParent, healthIcon,
            character.characterStats.healthPoints.ToString(),
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(health_pad_x, -health_pad_y),
            new Vector2(health_pad_x, -(health_pad_y + CornerIconSize)),
            new Vector2(CornerIconSize, textH));

        // ── Top-right: Mana ────────────────────────────────────────────────
        // anchor (1,1) = top-right corner; pivot (1,1) makes anchoredPosition
        // reference the icon's own top-right corner.
        // Icon offset (–pad, –pad): moves LEFT and DOWN away from the corner.
        // Value text stacked BELOW the icon at the same right edge.
        float mana_pad_x = 16f;
        float mana_pad_y = 64f;
        CreateCornerStat(cardParent, manaIcon,
            character.characterStats.mana.ToString(),
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(mana_pad_x, -mana_pad_y),
            new Vector2(mana_pad_x, -(mana_pad_y + CornerIconSize)),
            new Vector2(CornerIconSize, textH));

        // ── Bottom-left: Movement ──────────────────────────────────────────
        // anchor (0,0) = bottom-left corner.
        // Icon offset (+pad, +pad): moves RIGHT and UP away from the corner.
        // Value text stacked ABOVE the icon (Y = +(pad + CornerIconSize + iconGap)
        // since positive Y goes upward when the anchor is at the bottom).
        float movement_pad_x = -16f;
        float movement_pad_y = 132f;
        CreateCornerStat(cardParent, healthIcon,
            character.characterStats.movement.ToString(),
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(movement_pad_x, movement_pad_y),
            new Vector2(movement_pad_x, movement_pad_y + CornerIconSize),
            new Vector2(CornerIconSize, textH));

        // ── Bottom-right: Base Attack ──────────────────────────────────────
        // anchor (1,0) = bottom-right corner; pivot (1,0) references the icon's
        // bottom-right corner.
        // Icon offset (–pad, +pad): moves LEFT and UP away from the corner.
        // Value text stacked ABOVE the icon at the same right edge.
        float attack_pad_x = 16f;
        float attack_pad_y = 132f;
        CreateCornerStat(cardParent, healthIcon,
            character.characterStats.baseAttack.ToString(),
            new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(attack_pad_x, attack_pad_y),
            new Vector2(attack_pad_x, attack_pad_y + CornerIconSize),
            new Vector2(CornerIconSize, textH));

        // ── Top-center: Character name ─────────────────────────────────────
        // anchor/pivot (0.5, 1) centres the label horizontally and anchors its
        // top edge flush with the card top.  Y = –(pad × 0.5) gives a small
        // breathing gap without encroaching on the corner icons.
        float character_name_pad = 168f;
        CreateLabel(cardParent, character.characterName,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, character_name_pad),
            new Vector2(nameW, labelH), TextAlignmentOptions.Top);

        // ── Ability labels (passive + spell names, hoverable for tooltip) ──
        // Stacked below the character name; each label shows the ability name
        // and shows a full description tooltip with keyword definitions on hover.
        float abilityLabelH  = 22f;
        float abilityLabelW  = cardSize.x * 0.72f;
        float abilitySpacing = 3f;
        float nextAbilityY   = character_name_pad - labelH - abilitySpacing;

        foreach (PassiveAbility passive in character.characterPassiveAbilities)
        {
            CreateAbilityLabel(cardParent, passive.Name, passive.Description,
                new Vector2(0f, nextAbilityY), new Vector2(abilityLabelW, abilityLabelH));
            nextAbilityY -= abilityLabelH + abilitySpacing;
        }
        foreach (Spell spell in character.characterSpells)
        {
            CreateAbilityLabel(cardParent, spell.name, spell.description,
                new Vector2(0f, nextAbilityY), new Vector2(abilityLabelW, abilityLabelH));
            nextAbilityY -= abilityLabelH + abilitySpacing;
        }
    }

    // Creates a hoverable ability-name label.  The label shows only the ability
    // name; the full description (with keyword highlighting) appears in a tooltip
    // when the pointer enters the label.
    private void CreateAbilityLabel(Transform parent, string abilityName, string description,
        Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject("AbilityLabel_" + abilityName);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text             = abilityName;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = 9f;
        tmp.fontSizeMax      = 14f;
        tmp.fontStyle        = FontStyles.Italic;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = new Color(0.15f, 0.15f, 0.15f);
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2-Bold SDF");

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f); // bottom-centre anchor
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 1f); // top edge is the reference point
        rect.sizeDelta        = size;
        rect.anchoredPosition = anchoredPos;

        CardAbilityHoverHandler handler = obj.AddComponent<CardAbilityHoverHandler>();
        handler.Initialize(abilityName, description);
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchor, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Label_" + text);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = 24f;
        tmp.alignment = alignment;
        tmp.color = Color.black;
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2-Bold SDF");

        RectTransform rect = obj.GetComponent<RectTransform>();
        // Collapsing anchorMin == anchorMax to a single point means anchoredPosition
        // is a direct offset from that anchor rather than a stretch-relative value.
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
    }

    // Creates one square icon Image and one stat-value TextMeshPro label,
    // both anchored to the same corner of the card and stacked orthogonally:
    //   • top corners   — text sits BELOW  the icon (iconAnchoredPos.y is –ve)
    //   • bottom corners — text sits ABOVE the icon (iconAnchoredPos.y is +ve)
    //
    // Parameters
    //   anchor / pivot     — which card corner, e.g. (0,1) = top-left.
    //   iconAnchoredPos    — 2-D offset from that corner to the icon pivot.
    //                        Sign convention: +X right, –X left, +Y up, –Y down.
    //   textAnchoredPos    — same-corner offset to the text pivot; should place
    //                        the label flush against the icon's far edge.
    //   textSize           — (width, height) of the stat-value label.
    private void CreateCornerStat(Transform cardParent, Sprite iconSprite, string statValue,
        Vector2 anchor, Vector2 pivot,
        Vector2 iconAnchoredPos, Vector2 textAnchoredPos,
        Vector2 textSize)
    {
        // ── Icon ───────────────────────────────────────────────────────────
        GameObject iconObj = new GameObject("CornerIcon");
        iconObj.transform.SetParent(cardParent, false);

        Image iconImg = iconObj.AddComponent<Image>();
        if (iconSprite != null) iconImg.sprite = iconSprite;
        iconImg.type = Image.Type.Simple;
        // preserveAspect = false: sizeDelta forces a perfect square so the icon
        // stays square even if a non-square sprite is swapped in later.
        iconImg.preserveAspect = false;

        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        // anchorMin == anchorMax collapses the anchor to a single corner point
        // so anchoredPosition is a plain 2-D offset from that corner.
        iconRect.anchorMin        = anchor;
        iconRect.anchorMax        = anchor;
        iconRect.pivot            = pivot;
        // iconAnchoredPos nudges the icon inward from the chosen corner.
        // +X = right, –X = left, +Y = up, –Y = down (see method doc above).
        iconRect.anchoredPosition = iconAnchoredPos;
        // Both dimensions are CornerIconSize to guarantee a square display
        // regardless of the source png's pixel dimensions (128×128 here).
        iconRect.sizeDelta        = new Vector2(CornerIconSize, CornerIconSize);

        // ── Value label ────────────────────────────────────────────────────
        GameObject textObj = new GameObject("StatValue_" + statValue);
        textObj.transform.SetParent(cardParent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text             = statValue;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin      = 24f;
        tmp.fontSizeMax      = 32f;
        // Centre the number within the label so it aligns under/above the icon.
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = Color.black;
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2-Bold SDF");

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        // Same corner anchor / pivot as the icon so both share the same origin.
        textRect.anchorMin        = anchor;
        textRect.anchorMax        = anchor;
        textRect.pivot            = pivot;
        // textAnchoredPos places the label flush against the icon's far edge:
        //   top corners   → Y = –(pad + CornerIconSize + gap)  (further down)
        //   bottom corners → Y = +(pad + CornerIconSize + gap)  (further up)
        textRect.anchoredPosition = textAnchoredPos;
        textRect.sizeDelta        = textSize;
    }

    // ------------------------------------------------------------------ //
    // Off-screen model-to-texture renderer                                //
    // ------------------------------------------------------------------ //

    private sealed class CharacterPreviewRenderer
    {
        private readonly GameObject rig;
        private readonly Camera     cam;

        // Placed far from the main scene — CharacterSelectionScene has no 3D world objects.
        private static readonly Vector3 StagePos = new Vector3(5000f, 0f, 0f);

        public CharacterPreviewRenderer(Color bgColor)
        {
            rig = new GameObject("[CharPreviewRig]");
            rig.transform.position = StagePos;

            // Point light that wraps the model in soft fill light.
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(rig.transform, false);
            lightGo.transform.localPosition = new Vector3(-2f, 4f, -3f);
            var l = lightGo.AddComponent<Light>();
            l.type      = LightType.Point;
            l.intensity = 3f;
            l.range     = 20f;
            l.color     = Color.white;

            // Camera: slightly elevated, slight three-quarter angle.
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.8f, -2.5f);
            camGo.transform.LookAt(rig.transform.position + new Vector3(0f, 0.4f, 0f));
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView   = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 20f;
            // Use the realm colour as the background so the model sits on the same
            // colour as the scene behind the card.  Transparent-clear approaches are
            // unreliable across Unity render paths (alpha may not be written correctly),
            // so matching the background colour is the guaranteed solution.
            cam.backgroundColor = bgColor;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.enabled         = false; // render on demand only
        }

        /// <summary>
        /// Instantiates the model at the off-screen stage, renders a 128×128 snapshot,
        /// destroys the temporary model, and returns the RenderTexture.
        /// Returns null when the model asset cannot be found.
        /// </summary>
        public RenderTexture Capture(string modelPath)
        {
            var prefab = Resources.Load<GameObject>(modelPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CharacterPreviewRenderer] Model not found: '{modelPath}'");
                return null;
            }

            var model = Object.Instantiate(prefab, rig.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale    = Vector3.one;
            NormalizeScale(model, 1.5f);

            // 256×384 matches PortraitWidth×PortraitHeight exactly (2:3 ratio) so the
            // captured model preview fills the portrait without any stretching.
            var rt = new RenderTexture(256, 384, 16);
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            Object.Destroy(model);
            return rt;
        }

        /// <summary>Scales the model so its largest dimension equals <paramref name="targetSize"/>.</summary>
        private static void NormalizeScale(GameObject obj, float targetSize)
        {
            obj.transform.localScale = Vector3.one;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (maxDim > 0f)
                obj.transform.localScale = Vector3.one * (targetSize / maxDim);
        }

        /// <summary>Destroys the entire preview rig (camera + light).</summary>
        public void Cleanup() => Object.Destroy(rig);
    }
}
