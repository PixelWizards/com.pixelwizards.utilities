using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{
    public static class ResetParentTransform
    {
        [MenuItem("Tools/Edit/Reset Parent Transform %_r", false, 0)]
        public static void DistributeObjectsEvenly()
        {
            var selectedObjects = Selection.gameObjects;
            var sourcePosition = selectedObjects[0];
            Undo.RegisterCompleteObjectUndo(selectedObjects, "Reset Parent Transforms");

            foreach (GameObject selectedObject in selectedObjects)
            {
                ResetChildTransforms(selectedObject.transform);
            }
        }

        private static void ResetChildTransforms(Transform parent)
        {
            // make a temp parent
            var newGo = new GameObject();

            // grab all of the children
            var children = new List<Transform>();
            for (var i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i));
            }

            // move the children out of the parent temporarily
            foreach (var child in children)
            {
                child.parent = newGo.transform;
            }

            // reset the parent transform
            parent.position = Vector3.zero;
            parent.rotation = Quaternion.identity;

            // and reparent
            foreach (var child in children)
            {
                child.parent = parent;
            }

            GameObject.DestroyImmediate(newGo);
        }
    }
}