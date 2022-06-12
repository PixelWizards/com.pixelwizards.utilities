using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEditor.ShortcutManagement;

namespace PixelWizards.Utilities
{

    [InitializeOnLoad]
    public class Shortcuts
    {
        static Shortcuts()
        {
            SceneView.duringSceneGui += CheckSceneShortcuts;
        }

        static void CheckSceneShortcuts(SceneView view)
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Keypad7:
                        view.LookAt(view.pivot, Quaternion.LookRotation(Vector3.down, Vector3.forward), view.size, true);
                        evt.Use();
                        break;
                    case KeyCode.Keypad1:
                        view.LookAt(view.pivot, Quaternion.LookRotation(Vector3.right, Vector3.up), view.size, true);
                        evt.Use();
                        break;
                    case KeyCode.Keypad3:
                        view.LookAt(view.pivot, Quaternion.LookRotation(Vector3.forward, Vector3.up), view.size, true);
                        evt.Use();
                        break;
                    case KeyCode.Keypad5:
                        view.LookAt(view.pivot, view.rotation, view.size, !view.orthographic);
                        evt.Use();
                        break;
                    case KeyCode.Keypad0:
                        if (Camera.main)
                        {
                            view.LookAt(view.pivot, Camera.main.transform.rotation, view.size, false);
                            evt.Use();
                        }
                        break;
                }

            }
        }

        [MenuItem("Edit/Find In Project", false, 3)]
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Edit/Find In Project", KeyCode.G, ShortcutModifiers.Alt)]
#endif
        public static void ProjectSearch()
        {
            // Get the internal UnityEditor.ObjectBrowser window
            System.Type t = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");

            // Get the window & focus it
            EditorWindow win = EditorWindow.GetWindow(t);
            win.Focus();

            // Send a find command
            Event e = new Event();
            e.type = EventType.ExecuteCommand;
            e.commandName = "Find";
            win.SendEvent(e);
        }

        [MenuItem("Edit/Group/Create Group", false, 4)]
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Edit/Group/Create Group", KeyCode.G, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
#endif
        public static void CreateGroup()
        {
            GameObject newGO = new GameObject("Group");
            foreach (GameObject go in Selection.gameObjects)
                go.transform.parent = newGO.transform;
            Selection.activeGameObject = newGO;
            CenterOnChildren();
        }

        [MenuItem("Edit/Group/Center Group on Children", false, 5)]
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Edit/Group/Center Group on Children", KeyCode.G, ShortcutModifiers.Alt)]
#endif
        public static void CenterOnChildren()
        {
            foreach (Transform root in Selection.GetFiltered(typeof(Transform), SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable))
            {
                Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                Vector3 max = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);
                List<Vector3> origPos = new List<Vector3>();
                bool found = false;

                foreach (Transform t in root)
                {
                    found = true;
                    min = Vector3.Min(min, t.position);
                    max = Vector3.Max(max, t.position);
                    origPos.Add(t.position);
                }

                if (found)
                {
                    Vector3 centerPoint = (max + min) / 2f;
                    root.position = centerPoint;

                    int idx = 0;
                    foreach (Transform t in root)
                        t.position = origPos[idx++];
                }
            }
        }

        [MenuItem("Edit/Disable All Gizmos")]
        public static void DisableAllGizmos()
        {
            SceneView.lastActiveSceneView.drawGizmos = false;
        }

        [MenuItem("Edit/Enable All Gizmos")]
        public static void EnableAllGizmos()
        {
            SceneView.lastActiveSceneView.drawGizmos = true;
        }

    }
}