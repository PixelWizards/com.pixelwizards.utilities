
using UnityEditor;
using UnityEngine;

namespace PixelWizards.EditorTools
{
    [InitializeOnLoad]
    public static class EditorOnlyHierarchyHighlighter
    {
        private static readonly Color BackgroundColor =
            new Color(1f, 0.2f, 0.2f, 0.16f);

        private static readonly Color LabelColor =
            new Color(1f, 0.4f, 0.4f, 1f);

        // Toggle this if you want a badge icon
        private const bool ShowBadge = true;

        private static Texture2D badgeTexture;

        static EditorOnlyHierarchyHighlighter()
        {
#if UNITY_6000_5_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI
                -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI
                += OnHierarchyGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI
                -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI
                += OnHierarchyGUI;
#endif

            badgeTexture = EditorGUIUtility
                .IconContent("console.warnicon").image as Texture2D;
        }

#if UNITY_6000_5_OR_NEWER
        private static void OnHierarchyGUI(
            EntityId entityId, Rect selectionRect)
        {
            GameObject go =
                EditorUtility.EntityIdToObject(entityId) as GameObject;

            DrawHierarchyItem(go, selectionRect);
        }
#else
        private static void OnHierarchyGUI(
            int instanceId, Rect selectionRect)
        {
            GameObject go =
                EditorUtility.InstanceIDToObject(instanceId) as GameObject;

            DrawHierarchyItem(go, selectionRect);
        }
#endif

        private static void DrawHierarchyItem(
            GameObject go, Rect selectionRect)
        {
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
            if (!badgeTexture)
                return;

            Rect iconRect = rect;
            iconRect.x += 2f;
            iconRect.width = 16f;

            GUI.DrawTexture(iconRect, badgeTexture);
        }
    }
}
