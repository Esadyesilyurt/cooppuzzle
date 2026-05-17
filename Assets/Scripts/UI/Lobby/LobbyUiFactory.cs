using CoopPuzzle.Lobby;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Ana menü ve join panel için okunaklı lobi kodu giriş alanı oluşturur.</summary>
public static class LobbyUiFactory
{
    public static TMP_InputField CreateOdaKoduInput(Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var font = TMP_Settings.defaultFontAsset;

        var root = new GameObject("OdaKodu", typeof(RectTransform));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = sizeDelta;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.14f, 0.2f, 0.92f);

        var input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;
        input.contentType = TMP_InputField.ContentType.Alphanumeric;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = LobbyConstants.LobbyCodeLength;

        var textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(root.transform, false);
        StretchFull(textArea.GetComponent<RectTransform>(), 12f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(textArea.transform, false);
        StretchFull(viewport.GetComponent<RectTransform>(), 0f);

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform));
        placeholderGo.transform.SetParent(viewport.transform, false);
        StretchFull(placeholderGo.GetComponent<RectTransform>(), 0f);
        var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        placeholder.text = $"Lobi kodu ({LobbyConstants.LobbyCodeLength} karakter)";
        placeholder.fontSize = 30f;
        placeholder.color = new Color(1f, 1f, 1f, 0.5f);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) placeholder.font = font;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(viewport.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>(), 0f);
        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 36f;
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) text.font = font;

        input.textViewport = viewport.GetComponent<RectTransform>();
        input.textComponent = text;
        input.placeholder = placeholder;

        return input;
    }

    private static void StretchFull(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
