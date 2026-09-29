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
    public static class HierarchyActiveToggle
    {
#if UNITY_6000_6_OR_NEWER
        private const string ColumnId = "PixelWizards/GameObjectActive";

        // The new 6.6 hierarchy is column based. This creates a small fixed column
        // to the left of the Name column, which is the closest equivalent to the
        // old hard-coded left gutter toggle.
        [HierarchyViewColumnDescriptor(ColumnId)]
        internal static void CreateColumnDescriptor(HierarchyViewColumnDescriptor desc)
        {
            desc.Title = string.Empty;
            desc.Tooltip = "Enable or disable GameObject";
            desc.DefaultPriority = -1000;
            desc.DefaultWidth = 24;
            desc.DefaultVisibility = true;
        }

        [HierarchyViewCellDescriptor(ColumnId, typeof(HierarchyGameObjectHandler))]
        internal static void CreateGameObjectCellDescriptor(HierarchyViewCellDescriptor desc)
        {
            desc.BindCell = cell =>
            {
                // Cells are recycled, so rebuild the tiny control on every bind.
                cell.Clear();

                HierarchyGameObjectHandler handler = (HierarchyGameObjectHandler)cell.Handler;
                GameObject go = handler.GetGameObject(cell.Node);
                if (go == null)
                    return;

                var hoverArea = new VisualElement
                {
                    pickingMode = PickingMode.Position
                };
                hoverArea.style.flexGrow = 1;
                hoverArea.style.alignItems = Align.Center;
                hoverArea.style.justifyContent = Justify.Center;

                var toggle = new Toggle
                {
                    pickingMode = PickingMode.Position
                };
                toggle.SetValueWithoutNotify(go.activeSelf);
                toggle.style.width = 16;
                toggle.style.height = 16;
                toggle.style.opacity = 0;

                hoverArea.RegisterCallback<PointerEnterEvent>(_ => toggle.style.opacity = 1);
                hoverArea.RegisterCallback<PointerLeaveEvent>(_ => toggle.style.opacity = 0);

                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (go == null || go.activeSelf == evt.newValue)
                        return;

                    Undo.RecordObject(go, evt.newValue ? "Enable GameObject" : "Disable GameObject");
                    go.SetActive(evt.newValue);
                    EditorUtility.SetDirty(go);
                });

                hoverArea.Add(toggle);
                cell.Add(hoverArea);
                cell.IsDefaultValue = false;
            };
        }

        static HierarchyActiveToggle()
        {
            // Descriptor attributes do the registration in Unity 6.6+.
        }
#else
        static HierarchyActiveToggle()
        {
    #if UNITY_6000_5_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
    #else
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    #endif
        }

    #if UNITY_6000_5_OR_NEWER
        private static void OnHierarchyGUI(EntityId entityId, Rect rect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
            DrawLegacyToggle(obj, rect);
        }
    #else
        private static void OnHierarchyGUI(int instanceId, Rect rect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            DrawLegacyToggle(obj, rect);
        }
    #endif

        private static void DrawLegacyToggle(GameObject obj, Rect rect)
        {
            if (!obj)
                return;

            if (Event.current.type == EventType.MouseMove)
                EditorApplication.RepaintHierarchyWindow();

            Rect toggleRect = new Rect(
                32f,
                rect.y,
                18f,
                rect.height
            );

            bool isHoveringToggle = toggleRect.Contains(Event.current.mousePosition);
            if (!isHoveringToggle)
                return;

            EditorGUI.BeginChangeCheck();
            bool newActive = GUI.Toggle(toggleRect, obj.activeSelf, GUIContent.none);

            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObject(obj, newActive ? "Enable GameObject" : "Disable GameObject");
            obj.SetActive(newActive);
            EditorUtility.SetDirty(obj);
        }
#endif
    }
}
