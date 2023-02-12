using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System;

public class TextToTextMeshPro : Editor
{
    public class TextMeshProSettings
    {
        public bool Enabled;
        public FontStyles FontStyle;
        public float FontSize;
        public float FontSizeMin;
        public float FontSizeMax;
        public float LineSpacing;
        public bool EnableRichText;
        public bool EnableAutoSizing;
        public TextAlignmentOptions TextAlignmentOptions;
        public bool WrappingEnabled;
        public TextOverflowModes TextOverflowModes;
        public string Text;
        public Color Color;
        public bool RayCastTarget;
    }

    [MenuItem("Edit/Text To TextMeshPro", false, 4000)]
    static void DoIt()
    {
        if (TMPro.TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("ERROR!", "Assign a default font asset in project settings!", "OK", "");
            return;
        }

        // iterate through all of the children of the selection and convert them
        foreach (GameObject gameObject in Selection.gameObjects)
        {
            var childCount = gameObject.transform.childCount;
            for (var i = 0; i < childCount; i++)
            {
                ConvertTextToTextMeshPro(gameObject.transform.GetChild(i).gameObject);   
            }
        }
    }

    static void ConvertTextToTextMeshPro(GameObject target)
    {
        var settings = GetTextMeshProSettings(target);
        if (settings == null)
            return;

        try
        {
            DestroyImmediate(target.GetComponent<Text>());
        }
        catch(Exception e)
        {
            Debug.Log("Could not delete Text component from object " + target.name + " : Message: " + e.Message);
            return;
        }

        TextMeshProUGUI tmp = target.AddComponent<TextMeshProUGUI>();
        tmp.enabled = settings.Enabled;
        tmp.fontStyle = settings.FontStyle;
        tmp.fontSize = settings.FontSize;
        tmp.fontSizeMin = settings.FontSizeMin;
        tmp.fontSizeMax = settings.FontSizeMax;
        tmp.lineSpacing = settings.LineSpacing;
        tmp.richText = settings.EnableRichText;
        tmp.enableAutoSizing = settings.EnableAutoSizing;
        tmp.alignment = settings.TextAlignmentOptions;
        tmp.enableWordWrapping = settings.WrappingEnabled;
        tmp.overflowMode = settings.TextOverflowModes;
        tmp.text = settings.Text;
        tmp.color = settings.Color;
        tmp.raycastTarget = settings.RayCastTarget;
    }

    static TextMeshProSettings GetTextMeshProSettings(GameObject gameObject)
    {
        Text uiText = gameObject.GetComponent<Text>();
        if (uiText == null)
        {
            //EditorUtility.DisplayDialog("ERROR!", "You must select a Unity UI Text Object to convert.", "OK", "");
            return null;
        }

        return new TextMeshProSettings
        {
            Enabled = uiText.enabled,
            FontStyle = FontStyleToFontStyles(uiText.fontStyle),
            FontSize = uiText.fontSize,
            FontSizeMin = uiText.resizeTextMinSize,
            FontSizeMax = uiText.resizeTextMaxSize,
            LineSpacing = uiText.lineSpacing,
            EnableRichText = uiText.supportRichText,
            EnableAutoSizing = uiText.resizeTextForBestFit,
            TextAlignmentOptions = TextAnchorToTextAlignmentOptions(uiText.alignment),
            WrappingEnabled = HorizontalWrapModeToBool(uiText.horizontalOverflow),
            TextOverflowModes = VerticalWrapModeToTextOverflowModes(uiText.verticalOverflow),
            Text = uiText.text,
            Color = uiText.color,
            RayCastTarget = uiText.raycastTarget
        };
    }

    static bool HorizontalWrapModeToBool(HorizontalWrapMode overflow)
    {
        return overflow == HorizontalWrapMode.Wrap;
    }

    static TextOverflowModes VerticalWrapModeToTextOverflowModes(VerticalWrapMode verticalOverflow)
    {
        return verticalOverflow == VerticalWrapMode.Truncate ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
    }

    static FontStyles FontStyleToFontStyles(FontStyle fontStyle)
    {
        switch (fontStyle)
        {
            case FontStyle.Normal:
                return FontStyles.Normal;

            case FontStyle.Bold:
                return FontStyles.Bold;

            case FontStyle.Italic:
                return FontStyles.Italic;

            case FontStyle.BoldAndItalic:
                return FontStyles.Bold | FontStyles.Italic;
        }

        Debug.LogWarning("Unhandled font style " + fontStyle);
        return FontStyles.Normal;
    }

    static TextAlignmentOptions TextAnchorToTextAlignmentOptions(TextAnchor textAnchor)
    {
        switch (textAnchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;

            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;

            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;

            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;

            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;

            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;

            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;

            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;

            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
        }

        Debug.LogWarning("Unhandled text anchor " + textAnchor);
        return TextAlignmentOptions.TopLeft;
    }
}
