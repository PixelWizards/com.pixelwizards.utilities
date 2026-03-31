using UnityEditor;
using UnityEngine;

namespace PixelWizards.EditorTools
{
    [InitializeOnLoad]
    public static class EditorOnlyHierarchyHighlighter
    {
        private static readonly Color BackgroundColor = new Color(1f, 0.2f, 0.2f, 0.16f);
        private static readonly Color LabelColor = new Color(1f, 0.4f, 0.4f, 1f);

        // Toggle this if you want a badge icon
        private const bool ShowBadge = true;

        private static Texture2D badgeTexture;

        static EditorOnlyHierarchyHighlighter()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

            // Simple built-in icon (you can swap this)
            badgeTexture = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null || !go.CompareTag("EditorOnly"))
                return;

            DrawBackground(selectionRect);
            DrawSuffixLabel(selectionRect);

            if (ShowBadge)
                DrawBadge(selectionRect);
        }

        private static void DrawBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, BackgroundColor);
        }

        private static void DrawSuffixLabel(Rect rect)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = LabelColor },
                fontSize = 9,
                alignment = TextAnchor.MiddleRight
            };

            Rect labelRect = rect;
            labelRect.xMax -= 4f;

            EditorGUI.LabelField(labelRect, "[EditorOnly]", style);
        }

        private static void DrawBadge(Rect rect)
        {
            if (badgeTexture == null)
                return;

            Rect iconRect = rect;
            iconRect.x += 2f;
            iconRect.width = 16f;

            GUI.DrawTexture(iconRect, badgeTexture);
        }
    }
}
