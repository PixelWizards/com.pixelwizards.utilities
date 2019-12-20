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
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.parent = Selection.activeTransform;
            Selection.activeObject = go;
        }
    }
}