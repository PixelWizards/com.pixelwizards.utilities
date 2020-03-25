/* This wizard will replace a selection with an object or prefab.
 * Scene objects will be cloned (destroying their prefab links).
 * Original coding by 'yesfish', nabbed from Unity Forums
 * 'keep parent' added by Dave A (also removed 'rotation' option, using localRotation
 */
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;

namespace PixelWizards.Utilities
{


    public class ReplaceSelectionWindow : EditorWindow
    {

        private static GameObject replacement = null;
        private static bool keep = false;
        private static bool applyChangesInChildren = false;

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
                GUILayout.Label("Should we keep the original GameObjects or remove them?", EditorStyles.helpBox);

                GUILayout.Space(10f);

                applyChangesInChildren = EditorGUILayout.Toggle("Apply Transform Changes in Children", applyChangesInChildren);
                GUILayout.Label("Attempt to apply Transform changes in child objects?", EditorStyles.helpBox);

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

                if( applyChangesInChildren)
                {
                    var count = t.childCount;
                    for( int j = 0; j < count; j++)
                    {
                        var child = t.GetChild(j);
                        var newChildren = g.GetComponentsInChildren<Transform>().ToList();
                        if (newChildren.Contains(child))
                        {
                            // found match in the new prefab
                            var newChild = newChildren.FirstOrDefault(n => n.name == child.name);
                            newChild.position = child.position;
                            newChild.rotation = child.rotation;
                            newChild.localScale = child.localScale;
                        }
                        else
                        {
                            Debug.Log("Existing child " + child.name + " does not existing in new prefab?");
                        }
                    }
                }
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