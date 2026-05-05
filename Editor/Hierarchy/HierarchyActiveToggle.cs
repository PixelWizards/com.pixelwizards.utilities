using UnityEditor;
using UnityEngine;

namespace PixelWizards.EditorTools
{
    [InitializeOnLoad]
    public static class HierarchyActiveToggle
    {
        
        static HierarchyActiveToggle()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(EntityId entityId, Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
            {
                EditorApplication.RepaintHierarchyWindow();
            }
            
            var obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (!obj)
                return;

            // Fixed left gutter column.
            // Does not move with hierarchy indent.
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

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(obj, newActive ? "Enable GameObject" : "Disable GameObject");
                obj.SetActive(newActive);
                EditorUtility.SetDirty(obj);
            }
        }
    }
}