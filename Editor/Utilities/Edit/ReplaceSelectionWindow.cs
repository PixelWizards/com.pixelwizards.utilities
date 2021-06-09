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
        private static bool keepName = false;
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

                keepName = EditorGUILayout.Toggle("Keep Original Names?", keepName);
                GUILayout.Label("Replacement objects should keep the same names as the originals", EditorStyles.helpBox);

                GUILayout.Space(5f);

                keep = EditorGUILayout.Toggle("Keep Original Objects?", keep);
                GUILayout.Label("Should we keep the original GameObjects or remove them?", EditorStyles.helpBox);

                GUILayout.Space(10f);

                applyChangesInChildren = EditorGUILayout.Toggle("Apply Transform Changes in Children", applyChangesInChildren);
                GUILayout.Label("Attempt to apply Transform overrides in child objects?", EditorStyles.helpBox);

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
            // can only do replacement if we have a new object to replace with
            if (replacement == null)
                return;

            var selectionTransforms = Selection.GetTransforms(
                SelectionMode.TopLevel | SelectionMode.Editable);

            Undo.RegisterCompleteObjectUndo(selectionTransforms, "Replace Selection");

            for(int i = 0; i <selectionTransforms.Length; i++)
            {
                var sourceObject = selectionTransforms[i];
                var prefab = PrefabUtility.GetPrefabAssetType(replacement);
                var replacementObject = (GameObject)PrefabUtility.InstantiatePrefab(replacement);

                var gTransform = replacementObject.transform;
                gTransform.parent = sourceObject.parent;
                replacementObject.name = replacement.name + "_" + i;
                gTransform.localPosition = sourceObject.localPosition;
                gTransform.localScale = sourceObject.localScale;
                gTransform.localRotation = sourceObject.localRotation;

                if (keepName)
                {
                    replacementObject.name = sourceObject.name;
                }

                if ( applyChangesInChildren)
                {
                    var count = sourceObject.childCount;
                    for( int j = 0; j < count; j++)
                    {
                        var child = sourceObject.GetChild(j);
                        var newChildren = replacementObject.GetComponentsInChildren<Transform>().ToList();
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

            // if we don't want to keep them then remove the originals
            if (!keep)
            {
                // remove the old objects
                foreach (var g in Selection.gameObjects)
                {
                    GameObject.DestroyImmediate(g);
                }
            }
        }
    }
}