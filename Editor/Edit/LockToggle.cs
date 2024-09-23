using System;
using System.Reflection;
using UnityEditor;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.ShortcutManagement;
#endif
using UnityEngine;

/// <summary>
/// Inspired by https://forum.unity.com/threads/shortcut-key-for-lock-inspector.95815/#post-5013983
/// modded from: https://github.com/rfadeev/pump-editor/wiki/Lock-Toggle-Shortcuts
/// 
/// Provides toggle locking for various windows in the editor:
/// - Scene Hierarchy
/// - Project Window
/// - Inspector
/// - Timeline Window
/// </summary>
namespace PixelWizards.Utilities
{
    public static class LockToggle
    {
        [Shortcut("Window/Toggle Lock Focused Window", KeyCode.W, ShortcutModifiers.Action)]
        private static void ToggleLockFocusedWindow()
        {
            ToggleLockEditorWindow(EditorWindow.focusedWindow);
        }


        [Shortcut("Window/Toggle Lock Mouse Over Window", KeyCode.E, ShortcutModifiers.Action)]
        private static void ToggleLockMouseOverWindow()
        {
            ToggleLockEditorWindow(EditorWindow.mouseOverWindow);
        }

        [Shortcut("Window/Toggle Lock All Windows", KeyCode.W, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        private static void ToggleLockAllWindows()
        {
            var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var editorWindow in allWindows)
            {
                ToggleLockEditorWindow(editorWindow);
            }
        }

        private static void ToggleLockEditorWindow(EditorWindow editorWindow)
        {
            var editorAssembly = Assembly.GetAssembly(typeof(Editor));
            var projectBrowserType = editorAssembly.GetType("UnityEditor.ProjectBrowser");
            var inspectorWindowType = editorAssembly.GetType("UnityEditor.InspectorWindow");
            var sceneHierarchyWindowType = editorAssembly.GetType("UnityEditor.SceneHierarchyWindow");
            var timelineWindowType = Type.GetType("UnityEditor.Timeline.TimelineWindow, Unity.Timeline.Editor");

            var editorWindowType = editorWindow.GetType();
            if (editorWindowType == projectBrowserType)
            {
                // Unity C# reference: https://github.com/Unity-Technologies/UnityCsReference/blob/c6ec7823//Editor/Mono/ProjectBrowser.cs#L113
                var propertyInfo = projectBrowserType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.NonPublic);

                var value = (bool)propertyInfo.GetValue(editorWindow);
                propertyInfo.SetValue(editorWindow, !value);
            }
            else if (editorWindowType == inspectorWindowType)
            {
                // Unity C# reference: https://github.com/Unity-Technologies/UnityCsReference/blob/c6ec7823//Editor/Mono/Inspector/InspectorWindow.cs##L492
                var propertyInfo = inspectorWindowType.GetProperty("isLocked");

                var value = (bool)propertyInfo.GetValue(editorWindow);
                propertyInfo.SetValue(editorWindow, !value);
            }
            else if (editorWindowType == sceneHierarchyWindowType)
            {
                // Unity C# reference: https://github.com/Unity-Technologies/UnityCsReference/blob/c6ec7823/Editor/Mono/SceneHierarchyWindow.cs#L34
                var sceneHierarchyPropertyInfo = sceneHierarchyWindowType.GetProperty("sceneHierarchy");
                var sceneHierarchy = sceneHierarchyPropertyInfo.GetValue(editorWindow);

                // Unity C# reference: https://github.com/Unity-Technologies/UnityCsReference/blob/c6ec7823/Editor/Mono/SceneHierarchy.cs#L88
                var sceneHierarchyType = editorAssembly.GetType("UnityEditor.SceneHierarchy");
                var propertyInfo = sceneHierarchyType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.NonPublic);

                var value = (bool)propertyInfo.GetValue(sceneHierarchy);
                propertyInfo.SetValue(sceneHierarchy, !value);
            }
            else if ( editorWindowType == timelineWindowType)
            {
                var timelineWindow = Resources.FindObjectsOfTypeAll(timelineWindowType)[0] as EditorWindow;
                var propertyInfo = timelineWindow.GetType().GetProperty("locked");
                var value = (bool)propertyInfo.GetValue(timelineWindow, null);
                propertyInfo.SetValue(timelineWindow, !value, null);
            }
            else
            {
                return;
            }

            editorWindow.Repaint();
        }
    }
}
