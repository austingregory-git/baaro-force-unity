using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BaaroForce.Characters;

public class CharacterSelectionManager : MonoBehaviour
{
    // Fallback sizes (used only when sprites fail to load)
    private const float CardWidth = 64f;
    private const float CardHeight = 96f;
    private const float CardOffset = 90f;
    private const float PortraitWidth = 36f;
    private const float PortraitHeight = 48f;
    private const float PortraitDownPercent = 0.3575f;

    void Awake()
    {
        SetupCamera();
        GameObject canvasObj = CreateCanvas();
        CreateBackground(canvasObj.transform);
        CreateCards(canvasObj.transform);
    }

    private void SetupCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = Color.white;
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
        scaler.referenceResolution = new Vector2(320, 180);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    private void CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = Color.white;
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

        for (int i = 0; i < 3; i++)
        {
            Character character = new Winston();
            CreateCard(parent, positions[i], i, character);
        }
    }

    private void CreateCard(Transform parent, Vector2 anchoredPos, int index, Character character)
    {
        Sprite templateSprite = Resources.Load<Sprite>("character_template");

        GameObject card = new GameObject("CharacterCard_" + index);
        card.transform.SetParent(parent, false);

        Image cardImage = card.AddComponent<Image>();
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = anchoredPos;

        if (templateSprite != null)
        {
            cardImage.sprite = templateSprite;
            cardImage.type = Image.Type.Simple;
            cardImage.SetNativeSize();
        }
        else
        {
            cardImage.color = new Color(0.85f, 0.85f, 0.85f);
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
        }

        CreatePortrait(card.transform, cardRect.sizeDelta.y);
        AddCardStats(card.transform, cardRect.sizeDelta, character);
    }

    private void CreatePortrait(Transform cardParent, float cardHeight)
    {
        Sprite winstonSprite = Resources.Load<Sprite>("winston_48x36");

        GameObject portrait = new GameObject("CharacterPortrait");
        portrait.transform.SetParent(cardParent, false);

        Image portraitImage = portrait.AddComponent<Image>();
        RectTransform portraitRect = portrait.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.5f, 1f);
        portraitRect.anchorMax = new Vector2(0.5f, 1f);
        portraitRect.pivot = new Vector2(0.5f, 0.5f);

        if (winstonSprite != null)
        {
            portraitImage.sprite = winstonSprite;
            portraitImage.SetNativeSize();
        }
        else
        {
            portraitImage.color = new Color(0.4f, 0.6f, 0.8f);
            portraitRect.sizeDelta = new Vector2(PortraitWidth, PortraitHeight);
        }

        // Center of portrait sits at 35.75% down from the top of the card
        portraitRect.anchoredPosition = new Vector2(0f, -(cardHeight * PortraitDownPercent));
    }

    private void AddCardStats(Transform cardParent, Vector2 cardSize, Character character)
    {
        float pad = 2f;
        // Corner labels each take ~23% of card width, leaving ~54% for the centered name
        float cornerW = cardSize.x * 0.23f;
        float nameW   = cardSize.x * 0.54f;
        float labelH  = 7f;

        // Top-left: HP
        CreateLabel(cardParent, "HP: " + character.characterStats.healthPoints,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(pad, -pad),
            new Vector2(cornerW, labelH), TextAlignmentOptions.TopLeft);

        // Top-right: Mana
        CreateLabel(cardParent, "MP: " + character.characterStats.mana,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-pad, -pad),
            new Vector2(cornerW, labelH), TextAlignmentOptions.TopRight);

        // Bottom-left: Movement
        CreateLabel(cardParent, "MOV: " + character.characterStats.movement,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(pad, pad),
            new Vector2(cornerW, labelH), TextAlignmentOptions.BottomLeft);

        // Bottom-right: Base Attack
        CreateLabel(cardParent, "ATK: " + character.characterStats.baseAttack,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-pad, pad),
            new Vector2(cornerW, labelH), TextAlignmentOptions.BottomRight);

        // Top-center: Character name
        CreateLabel(cardParent, character.characterName,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -pad),
            new Vector2(nameW, labelH), TextAlignmentOptions.Top);
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchor, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Label_" + text);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 2f;
        tmp.fontSizeMax = 5f;
        tmp.alignment = alignment;
        tmp.color = Color.black;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
    }
}
