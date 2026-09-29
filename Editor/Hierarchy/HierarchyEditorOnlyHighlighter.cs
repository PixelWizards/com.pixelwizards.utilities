using UnityEditor;
using UnityEngine;

#if UNITY_6000_6_OR_NEWER
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEngine.UIElements;
#endif

namespace PixelWizards.EditorTools
{
    [InitializeOnLoad]
    public static class HierarchyEditorOnlyHighlighter
    {
        private static readonly Color BackgroundColor = new Color(1f, 0.2f, 0.2f, 0.16f);
        private static readonly Color LabelColor = new Color(1f, 0.4f, 0.4f, 1f);

        private const bool ShowBadge = true;

#if UNITY_6000_6_OR_NEWER
        private const string BackgroundElementName = "pw-editoronly-background";
        private const string BadgeElementName = "pw-editoronly-badge";
        private const string LabelElementName = "pw-editoronly-label";
#endif

        private static Texture2D badgeTexture;

        static HierarchyEditorOnlyHighlighter()
        {
            badgeTexture = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;

#if UNITY_6000_6_OR_NEWER
            // Unity 6.6+ uses the new UI Toolkit based Hierarchy window by default.
            HierarchyWindow.BindViewItem -= OnBindViewItem;
            HierarchyWindow.BindViewItem += OnBindViewItem;
#else
    #if UNITY_6000_5_OR_NEWER
            // Unity 6.5 uses EntityId for the legacy IMGUI hierarchy callback.
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
    #else
            // Older Unity versions use instance IDs.
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    #endif
#endif
        }

#if UNITY_6000_6_OR_NEWER
        private static void OnBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            if (item.Handler is not HierarchyGameObjectHandler handler)
                return;

            GameObject go = handler.GetGameObject(item.Node);
            bool isEditorOnly = go != null && go.CompareTag("EditorOnly");

            UpdateBackground(item, isEditorOnly);
            UpdateBadge(item, isEditorOnly);
            UpdateSuffixLabel(item, isEditorOnly);
        }

        private static void UpdateBackground(HierarchyViewItem item, bool visible)
        {
            VisualElement background = item.RowContainer.Q<VisualElement>(BackgroundElementName);

            if (!visible)
            {
                background?.RemoveFromHierarchy();
                return;
            }

            if (background != null)
                return;

            background = new VisualElement
            {
                name = BackgroundElementName,
                pickingMode = PickingMode.Ignore
            };

            background.style.position = Position.Absolute;
            background.style.left = 0;
            background.style.right = 0;
            background.style.top = 0;
            background.style.bottom = 0;
            background.style.backgroundColor = BackgroundColor;

            item.RowContainer.Add(background);
            background.SendToBack();
        }

        private static void UpdateBadge(HierarchyViewItem item, bool visible)
        {
            Image badge = item.RightCustomContainer.Q<Image>(BadgeElementName);

            if (!visible || !ShowBadge || badgeTexture == null)
            {
                badge?.RemoveFromHierarchy();
                return;
            }

            if (badge != null)
                return;

            badge = new Image
            {
                name = BadgeElementName,
                image = badgeTexture,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };

            badge.style.width = 14;
            badge.style.height = 14;
            badge.style.marginLeft = 2;
            badge.style.marginRight = 2;

            item.RightCustomContainer.Add(badge);
        }

        private static void UpdateSuffixLabel(HierarchyViewItem item, bool visible)
        {
            Label label = item.RightCustomContainer.Q<Label>(LabelElementName);

            if (!visible)
            {
                label?.RemoveFromHierarchy();
                return;
            }

            if (label != null)
                return;

            label = new Label("[EditorOnly]")
            {
                name = LabelElementName,
                pickingMode = PickingMode.Ignore
            };

            label.style.color = LabelColor;
            label.style.fontSize = 9;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.marginRight = 4;

            item.RightCustomContainer.Add(label);
        }
#else
    #if UNITY_6000_5_OR_NEWER
        private static void OnHierarchyGUI(EntityId entityId, Rect selectionRect)
        {
            GameObject go = EditorUtility.EntityIdToObject(entityId) as GameObject;
            DrawLegacyRow(go, selectionRect);
        }
    #else
        private static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            DrawLegacyRow(go, selectionRect);
        }
    #endif

        private static void DrawLegacyRow(GameObject go, Rect selectionRect)
        {
            if (go == null || !go.CompareTag("EditorOnly"))
                return;

            EditorGUI.DrawRect(selectionRect, BackgroundColor);

            var style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = LabelColor },
                fontSize = 9,
                alignment = TextAnchor.MiddleRight
            };

            Rect labelRect = selectionRect;
            labelRect.xMax -= 4f;
            EditorGUI.LabelField(labelRect, "[EditorOnly]", style);

            if (!ShowBadge || badgeTexture == null)
                return;

            Rect iconRect = selectionRect;
            iconRect.x += 2f;
            iconRect.width = 16f;
            GUI.DrawTexture(iconRect, badgeTexture);
        }
#endif
    }
}
