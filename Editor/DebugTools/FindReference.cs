
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace PixelWizards.Utilities
{
    public static class FindReferenceEditorTool
    {
        [MenuItem("Tools/Cleanup/Find all references #%&f", false, 0)]
        public static void FindAllReferencesToGameObject()
        {
            var go = Selection.activeGameObject;

            if (go == null)
            {
                Debug.LogWarning("No Object selected!");

                return;
            }

            // Get all MonoBehaviours in the scene
            List<MonoBehaviour> behaviours = new List<MonoBehaviour>();

            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                {
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    behaviours.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));
                }
            }

            var foundGameObjects = new List<GameObject>();

            foreach (var beh in behaviours)
            {
                //Debug.Log("MonoBehaviour: " + beh);

                if (beh == null)
                {
                    continue;
                }

                Type type = beh.GetType();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var f in fields) 
                {
                    var fieldType = f.FieldType;

                    //Debug.Log("Field: " + f + "type: " + fieldType);

                    if (fieldType == typeof(GameObject))
                    {
                        var o = f.GetValue(beh) as GameObject;

                        Validate(o, go, beh, foundGameObjects);
                    }
                    else if (fieldType == typeof(GameObject[]))
                    {
                        var array = f.GetValue(beh) as GameObject[];

                        foreach (var a in array)
                        {
                            Validate(a, go, beh, foundGameObjects);
                        }
                    }
                    else if (fieldType == typeof(Transform))
                    {
                        var t = f.GetValue(beh) as Transform;

                        if (t != null)
                        {
                            Validate(t.gameObject, go, beh, foundGameObjects);
                        }
                    }
                    else if (f.FieldType == typeof(UnityEvent))
                    {
                        var e = f.GetValue(beh) as UnityEvent;
                        for (int i = e.GetPersistentEventCount() - 1; i >= 0; --i)
                        {
                            var comp = e.GetPersistentTarget(i) as Component;

                            if (comp != null)
                            {
                                Validate(comp.gameObject, go, beh, foundGameObjects);
                            }
                        }
                    }
                    else if (f.FieldType == typeof(List<GameObject>))
                    {
                        var list = f.GetValue(beh) as List<GameObject>;

                        if (list != null)
                        {
                            foreach (var l in list)
                            {
                                Validate(l, go, beh, foundGameObjects);
                            }
                        }
                    }
                    else if (f.FieldType == typeof(List<Transform>))
                    {
                        var list = f.GetValue(beh) as List<Transform>;

                        if (list != null)
                        {
                            foreach (var l in list)
                            {
                                if (l != null)
                                {
                                    Validate(l.gameObject, go, beh, foundGameObjects);
                                }
                            }
                        }
                    }
                }
            }

            if (foundGameObjects.Count == 0)
            {
                Debug.LogWarning("No references found to " + go);
            }
            else
            {
                EditorGUIUtility.PingObject(go);
                Selection.objects = foundGameObjects.ToArray();
            }
        }

        private static void Validate(GameObject test, GameObject target, MonoBehaviour beh, List<GameObject> foundList)
        {
            if (test == target)
            {
                Debug.Log("Reference: " + beh.gameObject + "\n" + beh.gameObject.GetHierarchy());

                foundList.Add(beh.gameObject);
            }
        }

        public static string GetHierarchy(this GameObject go)
        {
            if (go == null)
            {
                return "";
            }

            List<string> hierarchy = new List<string>(6);
            StringBuilder sb = new StringBuilder();
            Transform transform = go.transform;

            while (transform != null)
            {
                hierarchy.Add(transform.name);
                transform = transform.parent;
            }

            if (hierarchy.Count > 0)
            {
                sb.Append(hierarchy[hierarchy.Count - 1]);
            }

            for (int i = hierarchy.Count - 2; i >= 0; --i)
            {
                sb.AppendFormat(" > {0}", hierarchy[i]);
            }

            return sb.ToString();
        }
    }
}
