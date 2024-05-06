using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PixelWizards.MultiScene
{

    public class CreateMenuEntries : MonoBehaviour
    {

        [MenuItem("GameObject/Scene Management/Create MultiScene Loader", false, 0)]
        private static void CreateMultiSceneLoader()
        {
            var go = new GameObject();
            go.name = "MultiScene Loader";
            go.AddComponent<MultiSceneSwapHelper>();
        }

    }
}