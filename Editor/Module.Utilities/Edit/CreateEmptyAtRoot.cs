using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{

    public class CreateEmptyAtRoot : MonoBehaviour
    {
        [MenuItem("GameObject/Create Empty at Root", false, 0)]
        public static void Create()
        {
            var go = new GameObject();
            go.name = "GameObject";
            go.transform.rotation = Quaternion.identity;
            go.transform.parent = Selection.activeTransform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            go.transform.localScale = Vector3.one;
            Selection.activeObject = go;
        }
    }
}