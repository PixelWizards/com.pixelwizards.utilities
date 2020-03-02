/* This wizard will replace a selection with an object or prefab.
 * Scene objects will be cloned (destroying their prefab links).
 * Original coding by 'yesfish', nabbed from Unity Forums
 * 'keep parent' added by Dave A (also removed 'rotation' option, using localRotation
 */
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PixelWizards.Utilities
{


    public class ReplaceSelectionWindow : EditorWindow
    {

        private static GameObject replacement = null;
        private static bool keep = false;

        [MenuItem("Edit/Replace Selection", false, -1)]
        private static void ShowWindow()
        {
            GetWindow<ReplaceSelectionWindow>(false, "Replace Selection", true);
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Space(10f);

                GUILayout.Label("Replace Selection", EditorStyles.boldLabel);
                GUILayout.Label("Replace the current selection with a specific prefab", EditorStyles.helpBox);
                GUILayout.Space(10f);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Replacement Object", GUILayout.Width(150f));
                    replacement = EditorGUILayout.ObjectField(replacement, typeof(GameObject), true, GUILayout.ExpandWidth(true)) as GameObject;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5f);

                keep = EditorGUILayout.Toggle("Keep Originals", keep);
                GUILayout.Space(10f);

                if ( GUILayout.Button("Replace Selection", GUILayout.Height(35f)))
                {
                    DoReplacement();
                }
            }
            GUILayout.EndVertical();
        }

        private void DoReplacement()
        {
            if (replacement == null)
                return;

            var transforms = Selection.GetTransforms(
                SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable);

            Undo.RegisterCompleteObjectUndo(transforms, "Replace Selection");

            for(int i = 0; i <transforms.Length; i++)
            {
                var t = transforms[i];
                var pref = PrefabUtility.GetPrefabAssetType(replacement);
                var g = (GameObject)PrefabUtility.InstantiatePrefab(replacement);

                var gTransform = g.transform;
                gTransform.parent = t.parent;
                g.name = replacement.name + "_" + i;
                gTransform.localPosition = t.localPosition;
                gTransform.localScale = t.localScale;
                gTransform.localRotation = t.localRotation;
            }

            if (!keep)
            {
                foreach (var g in Selection.gameObjects)
                {
                    GameObject.DestroyImmediate(g);
                }
            }
        }
    }
}