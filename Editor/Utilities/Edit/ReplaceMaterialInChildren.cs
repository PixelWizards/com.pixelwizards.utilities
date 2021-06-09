/*
 * bulk update of materials in child objects
 * 
 */
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PixelWizards.Utilities
{


    public class ReplaceMaterialInChildren : ScriptableWizard
    {
        public Material newMaterial = null;

        [MenuItem("Edit/Replace Materials in Children")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard(
                "Replace Materials in Children", typeof(ReplaceMaterialInChildren), "Replace");
        }

        void OnWizardCreate()
        {
            var transforms = Selection.GetTransforms(
                SelectionMode.TopLevel | SelectionMode.Editable);

            Undo.RegisterCompleteObjectUndo(transforms, "Replace Materials in Selection");

            foreach (Transform t in transforms)
            {
                Debug.Log("Replacing all materials on object : " + t.name + " with " + newMaterial.name);

                var mrs = t.GetComponentsInChildren<MeshRenderer>();
                if (mrs != null)
                {
                    foreach (var mr in mrs)
                    {
                        mr.sharedMaterial = newMaterial;
                    }
                }
            }
        }
    }
}