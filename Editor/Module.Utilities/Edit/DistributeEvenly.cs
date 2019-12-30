using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace MWU.Shared.Utilities
{
    public static class DistributeEvenly
    {
        public enum DistMode
        {
            X,
            Y,
            Z,
        }

        [MenuItem("Edit/Distribute/Along X", false, 2)]
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Edit/Distribute/Along X", KeyCode.Period, ShortcutModifiers.Action)]
#endif
        public static void DistributeEvenlyX()
        {
            DistributeEvenlyInternal(DistMode.X);
        }

        [MenuItem("Edit/Distribute/Along Y", false, 2)]
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Edit/Distribute/Along Y", KeyCode.Slash, ShortcutModifiers.Action)]
#endif
        public static void DistributeEvenlyY()
        {
            DistributeEvenlyInternal(DistMode.Y);
        }

        [MenuItem("Edit/Distribute/Along Z", false, 2)]
#if UNITY_2019_1_OR_NEWER
        [Shortcut("Edit/Distribute/Along Z", KeyCode.Comma, ShortcutModifiers.Action)]
#endif
        public static void DistributeEvenlyZ()
        {
            DistributeEvenlyInternal(DistMode.Z);
        }

        private static void DistributeEvenlyInternal(DistMode mode)
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length < 1)
                return;

            var sourcePosition = selectedObjects[0];
            Undo.RegisterCompleteObjectUndo(selectedObjects, "Distribute Evenly");

            var previousSize = Vector3.zero;
            Vector3 nextPos = sourcePosition.transform.position;
            foreach (var selectedObject in selectedObjects)
            {
                var collider = selectedObject.AddComponent<BoxCollider>();
                if (selectedObject.transform.childCount > 0)
                    GrowToFitChildren(selectedObject, selectedObject);
                // bump the subsequent objects
                if( selectedObject != selectedObjects[0])
                {
                    nextPos += collider.bounds.extents;
                    nextPos.x += (collider.bounds.extents.x * 0.25f);   // add a bit of buffer between them
                }

                switch( mode )
                {
                    case DistMode.X:
                        {
                            selectedObject.transform.position = new Vector3(nextPos.x, sourcePosition.transform.position.y, sourcePosition.transform.position.z);

                            break;
                        }
                    case DistMode.Y:
                        {
                            selectedObject.transform.position = new Vector3(sourcePosition.transform.position.x, nextPos.y, sourcePosition.transform.position.z);

                            break;
                        }
                    case DistMode.Z:
                        {
                            selectedObject.transform.position = new Vector3(sourcePosition.transform.position.x, sourcePosition.transform.position.y, nextPos.z);

                            break;
                        }
                }
                nextPos = selectedObject.transform.position + collider.bounds.extents;

                GameObject.DestroyImmediate(collider);
            }
        }

        public static void GrowToFitChildren(GameObject thisGO, GameObject parent)
        {
            var collider = parent.GetComponent<BoxCollider>();
            // need to take into account the size of any children as well
            foreach (Transform child in thisGO.transform)
            {
                var childCollider = child.gameObject.AddComponent<BoxCollider>();
                var bounds = collider.bounds;
                bounds.Encapsulate(childCollider.bounds);
                collider.size = bounds.size;
                GameObject.DestroyImmediate(childCollider);

                if( child.childCount > 0)
                {
                    GrowToFitChildren(child.gameObject, parent);
                }
            }
        }
    }
}
