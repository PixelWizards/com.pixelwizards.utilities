using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PixelWizards.MultiScene
{
    /// <summary>
    /// Simple wrapper that the timeline track talks to.
    /// 
    /// Note: implements the scene loading separately from the MultiSceneConfig 
    /// so we can do callbacks on scene load (coroutines etc) that we can't do in a scriptable objects
    /// </summary>
    [ExecuteAlways]
    public class MultiSceneSwapHelper : MonoBehaviour
    {
        public class SceneModel
        {
            public bool isLevelLoading = false;
            public string levelLoading = String.Empty;
        }

        [Header("the scene config that this swap helper uses")]
        public MultiSceneLoader multiSceneConfig;

        [Header("Load configs on Awake()?")]
        public bool loadConfigOnAwake = false;
        [Header("Do Autoload in the Editor (not in play mode)?")]
        public bool autoLoadConfigInEditor = false;

        [Header("List of configs that we wantt to load on Awake()")]
        public List<string> configList = new List<string>();

        // our internal data model
        private SceneModel model = new SceneModel();

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
                if (!autoLoadConfigInEditor)
                    return;
#endif
                foreach( var config in configList)
                {
                    LoadSceneConfigByName(config, false, true);
                }
            }
        }

        /// <summary>
        /// Load a given config from our MultiSceneConfig by name, optionally unloading all existing scenes and optionally using Async loading 
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="unloadExisting"></param>
        /// <param name="useAsyncLoading"></param>
        public void LoadConfig( string configName, bool unloadExisting, bool useAsyncLoading, Action<string> callback = null)
        {
            LoadSceneConfigByName(configName, unloadExisting, useAsyncLoading, callback);
        }

        public void LoadConfig( string configName, bool unloadExisting, Action<string> callback = null)
        {
            LoadSceneConfigByName(configName, unloadExisting, false, callback);
        }

        public void UnloadConfig( string configName)
        {
            UnloadConfigInternal(configName);
        }

        /// <summary>
        /// Loads a scene config by name
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="unloadExisting"></param>
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

        /// <summary>
        /// Load a specific scene config, optionally unloads existing first (ie can be optionally additively loaded)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="unloadExisting"></param>
        private void LoadSceneConfig(SceneConfig config, bool unloadExisting, bool useAsyncLoading, Action<string> callback = null)
        {
            // is this config already in our cache? ie are the scenes loaded already?
            if (configCache.Contains(config.name))
            {
               // Debug.Log("LoadSceneConfig() - " + config.name + " is already in our cache, ignoring call");
                return;
            }
            else
            {
                Debug.Log("Starting scene load for config: " + config.name);
                configCache.Add(config.name);
            }
                
            for (int i = 0; i < config.sceneList.Count; i++)
            {
                var sceneName = config.sceneList[i].SceneName;
                Debug.Log("Loading scene from config : " + sceneName);
                if (Application.isPlaying)
                {
                    if (SceneManager.GetSceneByName(sceneName) == null)
                        Debug.LogError("Scene: " + sceneName + " doesn't exist in build settings");

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
                                        // if so, then do light magic
                                        LightProbes.Tetrahedralize();
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
                                        // if so, then do light magic
                                        LightProbes.TetrahedralizeAsync();
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
                                    // if so, then do light magic
                                    LightProbes.TetrahedralizeAsync();
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
                        Debug.Log("Editor: loading scene: " + sceneName);
                        // load the scene
                        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(config.sceneList[i].Scene), OpenSceneMode.Additive);

                        // now is it loaded?
                        if (IsScene_CurrentlyLoaded_inEditor(sceneName))
                        {
                            // if so, then do light magic
                            LightProbes.Tetrahedralize();
                        }
                    }
#endif
                }
            }

            Debug.Log("Scene load for config: " + config.name + " COMPLETE");
        }

        /// <summary>
        /// Unloads any scenes within the specified config
        /// </summary>
        /// <param name="thisConfig"></param>
        private void UnloadConfigInternal(string thisConfig)
        {
            if( configCache.Contains(thisConfig))
            {
                Debug.Log("UnloadConfig : " + thisConfig + " starting...");
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

            Debug.Log("UnloadConfig : " + thisConfig + " complete...");
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
                                Debug.Log("Unload scene async: " + scene.name);

                                SceneManager.UnloadSceneAsync(scene);
                            }
                        }
#if UNITY_EDITOR
                        else
                        {
                            Debug.Log("Editor: close scene : " + scene.name);

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

        private void LoadNewScene(string newEnvironment, bool useAsyncLoading, Action<string> callback = null)
        {
            model.levelLoading = newEnvironment;

            StartCoroutine(LoadSceneInternal(model.levelLoading, false, useAsyncLoading, callback));
        }

        /// <summary>
        /// Load a new Level additively. Fires a callback with the name of the newly loaded level once the load is completed.
        /// </summary>
        private void LoadSceneAdditive(string newEnvironment, bool useAsyncLoading, Action<string> callback = null)
        {
            model.levelLoading = newEnvironment;

            StartCoroutine(LoadSceneInternal(model.levelLoading, true, useAsyncLoading, callback));
        }

        /// <summary>
        /// Set the specified scene to be the active one
        /// </summary>
        /// <param name="thisScene"></param>
        private void SetActiveScene(string thisScene)
        {
            Debug.Log("Set active scene : " + thisScene);
            var activeScene = SceneManager.GetSceneByName(thisScene);
            SceneManager.SetActiveScene(activeScene);
        }

        private void UnloadScene(string sceneName)
        {
            Debug.Log("Unload scene async : " + sceneName);
            SceneManager.UnloadSceneAsync(sceneName);
        }

        /// <summary>
        /// Async level loading using the new Unity 5 Scene Management API
        /// </summary>
        private IEnumerator LoadSceneInternal(string newScene, bool useAdditive, bool useAsyncLoading, Action<string> callback = null)
        {
            Debug.Log("LoadSceneInternal: " + newScene);
            AsyncOperation async = new AsyncOperation();
            try
            {
                var sceneMode = LoadSceneMode.Single;
                if (useAdditive)
                    sceneMode = LoadSceneMode.Additive;

                if( useAsyncLoading)
                {
                    Debug.Log("Start async scene load: " + newScene);
                    async = SceneManager.LoadSceneAsync(newScene, sceneMode);
                    async.allowSceneActivation = true;          // scenes will activate themselves
                }
                else
                {
                    SceneManager.LoadScene(newScene, sceneMode);
                    Debug.Log("LoadSceneInternal: " + newScene + " COMPLETE");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Caught Exception " + e.Message + " while loading scene: " + newScene + " - might not be in your build settings?");
                async = null;
                yield break;
            }

            if( useAsyncLoading)
            {
                if (async != null)
                {
                    while (!async.isDone)
                    {
                        model.isLevelLoading = true;
                        yield return null;
                    }

                    if (async.isDone)
                    {
                        model.isLevelLoading = false;
                        // TODO: should add a timer so we can log how long level loads take

                        callback?.Invoke(newScene);
                        
                        Debug.Log("LoadSceneInternal: async load " + newScene + " COMPLETE");

                        model.levelLoading = string.Empty;
                        async = null;
                        yield break;
                    }
                }
                else
                {
                    async = null;
                    Debug.LogError("Async loading of scene failed for scene: " + newScene + " - might not be in your build settings?");
                }
            }

            yield break;
        }


        /// <summary>
        /// Check if a scene is loaded at edit time
        /// </summary>
        /// <param name="thisScene"></param>
        /// <returns></returns>
#if UNITY_EDITOR
        private bool IsScene_CurrentlyLoaded_inEditor(string thisScene)
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (scene.name == thisScene)
                {
                   // Debug.Log("Editor: Scene already loaded");
                    return true; //the scene is already loaded
                }
            }

         //   Debug.Log("Editor: Scene not loaded");
            //scene not currently loaded in the hierarchy:
            return false;
        }
#endif

        /// <summary>
        /// Check if a scene is loaded at runtime
        /// </summary>
        /// <param name="thisScene"></param>
        /// <returns></returns>
        private bool IsScene_CurrentlyLoaded(string thisScene)
        {
#if UNITY_EDITOR
            return IsScene_CurrentlyLoaded_inEditor(thisScene);
#else
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == thisScene)
                {
                    if( scene.isLoaded)
                    {
                        //the scene is already loaded
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;   //scene not currently loaded in the hierarchy
#endif
        }

    }
}