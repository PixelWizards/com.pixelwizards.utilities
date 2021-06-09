//#define USE_LOGGING
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelWizards.MultiScene
{
    /// <summary>
    /// Simple wrapper that the timeline track talks to.
    /// 
    /// Note: implements the scene loading separately from the MultiSceneConfig 
    /// so we can do callbacks on scene load (coroutines etc) that we can't do in a scriptable objects
    /// </summary>
    [ExecuteAlways]
    public class MultiSceneSwapHelper : SceneLoadingFramework
    {
        [Header("the scene config that this swap helper uses")]
        public MultiSceneLoader multiSceneConfig;

        [Header("Load configs on Awake()?")]
        public bool loadConfigOnAwake = false;
        [Header("Do Autoload in the Editor (not in play mode)?")]
        public bool autoLoadConfigInEditor = false;

        [Header("List of configs that we want to load on Awake()")]
        public List<string> configList = new List<string>();

        // cache of configs that are currently loaded
        private List<string> configCache = new List<string>();

        /// <summary>
        /// Added support for runtime 'on demand' loading - load one master scene that triggers a set of sub-scenes to be loaded if they aren't already
        /// </summary>
        public void Awake()
        {
            configCache.Clear();

            if (loadConfigOnAwake)
            {
#if UNITY_EDITOR
                if (!autoLoadConfigInEditor && !Application.isPlaying)
                    return;
#endif
                foreach( var config in configList)
                {
                    if (!IsConfigLoaded(config))
                    {
                    LoadSceneConfigByName(config, false, true);
					}
                }
            }

            // Added custom callbacks for lightprobe additive loading
            LightProbes.needsRetetrahedralization += LightProbes_needsRetetrahedralization;
            LightProbes.tetrahedralizationCompleted += LightProbes_tetrahedralizationCompleted;
        }

        public void OnDisable()
        {
            // remove our event hooks
            LightProbes.needsRetetrahedralization -= LightProbes_needsRetetrahedralization;
            LightProbes.tetrahedralizationCompleted -= LightProbes_tetrahedralizationCompleted;
        }

        /// <summary>
        /// Event which is called after [[LightProbes.Tetrahedralize]] or [[LightProbes.TetrahedralizeAsync]] has finished computing a tetrahedralization.
        /// </summary>
        private void LightProbes_tetrahedralizationCompleted()
        {
#if USE_LOGGING
            Debug.Log("LightProbes_tetrahedralizationCompleted callback received");
#endif
        }
/// <summary>
        /// check if all of the scenes in a given config have completed loading yet
        /// </summary>
        /// <param name="configName"></param>
        /// <returns></returns>
        public bool IsConfigLoaded( string configName)
        {
            var loaded = true;
            var config = multiSceneConfig.config.FirstOrDefault(c => c.name == configName);

            foreach (var scene in config.sceneList)
            {
                if (!IsScene_CurrentlyLoaded(scene.SceneName))
                {
                    loaded = false;
                }
            }
            
            return loaded;
        }

        public void LoadConfig(string configName)
        {
            LoadConfig(configName, true);
        }

        /// <summary>
        /// Event which is called when [[LightProbes.Tetrahedralize]] or [[LightProbes.TetrahedralizeAsync]] needs to be executed to include or clean up new LightProbes for lighting.
        /// </summary>
        private void LightProbes_needsRetetrahedralization()
        {
#if USE_LOGGING
            Debug.Log("LightProbes_needsRetetrahedralization callback received");
#endif
            // do lightprobe gi magic
            LightProbes.TetrahedralizeAsync();
        }

        /// <summary>
        /// Load a given config from our MultiSceneConfig by name, optionally unloading all existing scenes and optionally using Async loading 
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="unloadExisting"></param>
        /// <param name="useAsyncLoading"></param>
        /// <param name="callback"></param>
        public void LoadConfig( string configName, bool unloadExisting, bool useAsyncLoading, Action<string> callback = null)
        {
            LoadSceneConfigByName(configName, unloadExisting, useAsyncLoading, cb =>
            {
                callback?.Invoke(configName);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="unloadExisting"></param>
        /// <param name="callback"></param>
        public void LoadConfig( string configName, bool unloadExisting, Action<string> callback = null)
        {
            LoadSceneConfigByName(configName, unloadExisting, false, cb =>
            {
                callback?.Invoke(configName);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configName"></param>
        public void UnloadConfig( string configName)
        {
            UnloadConfigInternal(configName);
        }

        private void LoadSceneConfigByName(string configName, bool unloadExisting, bool useAsyncLoading, Action<string> callback = null)
        {
            foreach (var entry in multiSceneConfig.config)
            {
                if (entry.name == configName)
                {
                    LoadSceneConfig(entry, unloadExisting, useAsyncLoading, callback);
                    return;
                }
            }
            // if we get here, we didn't find the config
            Debug.LogWarning("MultiSceneLoader::LoadSceneConfigByName() - could not find config: " + configName);
        }

        private void LoadSceneConfig(SceneConfig config, bool unloadExisting, bool useAsyncLoading, Action<string> callback = null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (useAsyncLoading)
                {
#if USE_LOGGING
                    Debug.Log("Not in play mode - Async loading ignored");
#endif
                }
            }
#endif

            // is this config already in our cache? ie are the scenes loaded already?
            if (configCache.Contains(config.name))
            {
               // Debug.Log("LoadSceneConfig() - " + config.name + " is already in our cache, ignoring call");
                return;
            }
            else
            {
#if USE_LOGGING
                Debug.Log("Starting scene load for config: " + config.name);
#endif
                configCache.Add(config.name);
            }
                
            for (int i = 0; i < config.sceneList.Count; i++)
            {
                var sceneName = config.sceneList[i].SceneName;
#if USE_LOGGING
                Debug.Log("Loading scene from config : " + sceneName);
#endif
                if (Application.isPlaying)
                {
                    if (SceneManager.GetSceneByName(sceneName) == null)
                    {
                        Debug.LogError("Scene: " + sceneName + " doesn't exist in build settings");
                    }
                        

                    if (!IsScene_CurrentlyLoaded(sceneName))
                    {
                        if (i == 0)
                        {
                            if (unloadExisting )
                            {
                                // if we need to unload existing, then load the first in single mode, otherwise everything is additive
                                LoadNewScene(sceneName, useAsyncLoading, innercallback =>
                                {
                                    callback?.Invoke(sceneName);

                                    if (IsScene_CurrentlyLoaded(sceneName))
                                    {
                                        if (config.setSceneActive)
                                        {
                                            if (sceneName == config.activeSceneName)
                                            {
                                                SetActiveScene(sceneName);
                                            }
                                        }
#if CUSTOM_GI_BUILD
                                        // Tetrahedralize is done in  LightProbes_needsRetetrahedralization callback now
#else
                                        // if so, then do light magic
                                        LightProbes.TetrahedralizeAsync();
#endif
                                    }
                                });
                            }
                            else
                            {
                                // and the rest additive
                                LoadSceneAdditive(sceneName, useAsyncLoading, innercallback =>
                                {
                                    callback?.Invoke(sceneName);

                                    if (IsScene_CurrentlyLoaded(sceneName))
                                    {
                                        if( config.setSceneActive)
                                        {
                                            if( sceneName == config.activeSceneName)
                                            {
                                                SetActiveScene(sceneName);
                                            }
                                        }
#if CUSTOM_GI_BUILD
                                        // Tetrahedralize is done in  LightProbes_needsRetetrahedralization callback now
#else
                                        // if so, then do light magic
                                        LightProbes.TetrahedralizeAsync();
#endif
                                    }
                                });
                            }

                        }
                        else
                        {
                            // and the rest additive
                            LoadSceneAdditive(sceneName, useAsyncLoading, innercallback =>
                            {
                                callback?.Invoke(sceneName);

                                if (IsScene_CurrentlyLoaded(sceneName))
                                {
                                    if (config.setSceneActive)
                                    {
                                        if (sceneName == config.activeSceneName)
                                        {
                                            SetActiveScene(sceneName);
                                        }
                                    }
#if CUSTOM_GI_BUILD
                                    // Tetrahedralize is done in  LightProbes_needsRetetrahedralization callback now
#else
                                        // if so, then do light magic
                                        LightProbes.TetrahedralizeAsync();
#endif
                                }
                            });
                        }
                    }
                }
                else
                {
#if UNITY_EDITOR
                    // if it's not already loaded
                    if (!IsScene_CurrentlyLoaded_inEditor(sceneName))
                    {
#if USE_LOGGING
                        Debug.Log("Editor: loading scene: " + sceneName);
#endif
                        // load the scene
                        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(config.sceneList[i].Scene), OpenSceneMode.Additive);

                        // now is it loaded?
                        if (IsScene_CurrentlyLoaded_inEditor(sceneName))
                        {
                            if (config.setSceneActive)
                            {
                                if (sceneName == config.activeSceneName)
                                {
                                    SetActiveScene(sceneName);
                                }
                            }
#if CUSTOM_GI_BUILD
                            // Tetrahedralize is done in  LightProbes_needsRetetrahedralization callback now
#else
                                        // if so, then do light magic
                                        LightProbes.TetrahedralizeAsync();
#endif
                        }
                    }
#endif
                }
            }
#if USE_LOGGING
            Debug.Log("Scene load for config: " + config.name + " COMPLETE");
#endif
        }

        /// <summary>
        /// Unloads any scenes within the specified config
        /// </summary>
        /// <param name="thisConfig"></param>
        private void UnloadConfigInternal(string thisConfig)
        {
            if( configCache.Contains(thisConfig))
            {
#if USE_LOGGING
                Debug.Log("UnloadConfig : " + thisConfig + " starting...");
#endif
                configCache.Remove(thisConfig);
            }
            else
            {
               // Debug.Log("unloadConfig: " + thisConfig + " is not loaded, ignoring");
                return;
            }

            foreach (var entry in multiSceneConfig.config)
            {
                if (entry.name == thisConfig)
                {
                    UnloadConfigInternal(entry);
                }
            }
#if USE_LOGGING
            Debug.Log("UnloadConfig : " + thisConfig + " complete...");
#endif
        }

        /// <summary>
        /// Unloads any scene from a specific config ( if they are loaded )
        /// </summary>
        /// <param name="thisConfig"></param>
        private void UnloadConfigInternal(SceneConfig thisConfig)
        {
            try
            {
                foreach( var thisScene in thisConfig.sceneList)
                {
                    var scene = SceneManager.GetSceneByName(thisScene.SceneName);
                    if (scene != null)
                    {
                        if( Application.isPlaying)
                        {
                            if( scene.isLoaded)
                            {
#if USE_LOGGING
                                Debug.Log("Unload scene async: " + scene.name);
#endif

                                SceneManager.UnloadSceneAsync(scene);
                            }
                        }
#if UNITY_EDITOR
                        else
                        {
#if USE_LOGGING
                            Debug.Log("Editor: close scene : " + scene.name);
#endif

                            EditorSceneManager.CloseScene(scene, true);
                        }
#endif
                    }
                }
            }
            finally
            {

            }
        }


    }
}