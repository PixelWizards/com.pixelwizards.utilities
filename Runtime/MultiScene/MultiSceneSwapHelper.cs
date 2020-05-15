using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelWizards.MultiScene
{
    /// <summary>
    /// Simple wrapper that the timeline track talks to
    /// </summary>
    public class MultiSceneSwapHelper : MonoBehaviour
    {
        public MultiSceneLoader multiSceneConfig;
        
        public void LoadConfig(SceneConfig thisConfig, bool unloadExisting)
        {
            multiSceneConfig.LoadSceneConfig(thisConfig, unloadExisting);
        }

        public void LoadConfig( string configName, bool unloadExisting)
        {
            multiSceneConfig.LoadSceneConfigByName(configName, unloadExisting);
        }

        public void UnloadConfig( string configName)
        {
            multiSceneConfig.UnloadConfig(configName);
        }
    }
}