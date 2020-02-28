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


    public class ReplaceSelection : ScriptableWizard
    {
        static GameObject replacement = null;
        static bool keep = false;

        public GameObject ReplacementObject = null;
        public bool KeepOriginals = false;

        [MenuItem("Tools/Edit/Replace Selection...")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard(
                "Replace Selection", typeof(ReplaceSelection), "Replace");
        }

        public ReplaceSelection()
        {
            ReplacementObject = replacement;
            KeepOriginals = keep;
        }

        void OnWizardUpdate()
        {
            replacement = ReplacementObject;
            keep = KeepOriginals;
        }

        void OnWizardCreate()
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

                Transform gTransform = g.transform;
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