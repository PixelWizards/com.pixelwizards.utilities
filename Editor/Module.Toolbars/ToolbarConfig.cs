using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelWizards.Toolbars
{
    public enum SceneLoaderType
    {
        Individual,
        MultiScene,
    }

    [CreateAssetMenu(fileName = "ToolbarConfig", menuName = "Toolbars/Create Toolbar Config", order = 1)]
    public class ToolbarConfig : ScriptableObject
    {
        // enable / disable macro sections of the toolbars
        public bool showSceneLoader = false;
        public bool showSceneTools = false;
        public bool showRenderSettings = false;
        public bool showSceneHelpers = false;
        public bool showLayoutModes = false;
        public SceneLoaderType sceneLoaderType = SceneLoaderType.Individual;

        [SerializeField] public Object[] sceneLoaderList = null;
        [SerializeField] public Object[] windowLayouts = null;
    }
}