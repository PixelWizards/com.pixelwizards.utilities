#define USE_LOGGING
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelWizards.MultiScene
{
    /// <summary>
    /// Wrapper for Unity's scene loading system to provide loading / unloading of individual scenes in either sync or async manner
    /// </summary>
    public class SceneLoadingFramework : MonoBehaviour
    {
        public class SceneModel
        {
            public bool isLevelLoading = false;
            public string levelLoading = String.Empty;
        }

        // our internal data model
        private SceneModel model = new SceneModel();

        protected void LoadNewScene(string newEnvironment, bool useAsyncLoading, Action<string> callback = null)
        {
            model.levelLoading = newEnvironment;

            StartCoroutine(LoadSceneInternal(model.levelLoading, LoadSceneMode.Single, useAsyncLoading, cb =>
            {
#if USE_LOGGING
                Debug.Log("SceneLoadingFramework::LoadNewScene Callback recieved!");
#endif
                callback?.Invoke(newEnvironment);
            }));
        }

        /// <summary>
        /// Load a new Level additively. Fires a callback with the name of the newly loaded level once the load is completed.
        /// </summary>
        protected void LoadSceneAdditive(string newEnvironment, bool useAsyncLoading, Action<string> callback = null)
        {
            model.levelLoading = newEnvironment;

            StartCoroutine(LoadSceneInternal(model.levelLoading, LoadSceneMode.Additive, useAsyncLoading, cb =>
            {
#if USE_LOGGING
                Debug.Log("SceneLoadingFramework::LoadSceneAdditive Callback recieved!");
#endif
                callback?.Invoke(newEnvironment);
            }));
        }

        /// <summary>
        /// Set the specified scene to be the active one
        /// </summary>
        /// <param name="thisScene"></param>
        protected void SetActiveScene(string thisScene)
        {
#if USE_LOGGING
            Debug.Log("SceneLoadingFramework::Set active scene : " + thisScene);
#endif
            var activeScene = SceneManager.GetSceneByName(thisScene);
            if (activeScene.isLoaded)
            {
                SceneManager.SetActiveScene(activeScene);    
            }
            else
            {
                Debug.LogWarning("Scene: " + thisScene + " is not loaded, can't be made active?");
            }
        }

        protected void UnloadScene(string sceneName)
        {
            Debug.Log("SceneLoadingFramework::Unload scene async : " + sceneName);
            SceneManager.UnloadSceneAsync(sceneName);
        }

        /// <summary>
        /// Async level loading using the new Unity 5 Scene Management API
        /// </summary>
        protected IEnumerator LoadSceneInternal(string newScene, LoadSceneMode sceneMode, bool useAsyncLoading, Action<string> callback = null)
        {
#if USE_LOGGING
            Debug.Log("SceneLoadingFramework::LoadSceneInternal: " + newScene);
#endif
            AsyncOperation async = new AsyncOperation();
            try
            {
                if (useAsyncLoading)
                {
#if USE_LOGGING
                    Debug.Log("SceneLoadingFramework::Start async scene load: " + newScene);
#endif
                    async = SceneManager.LoadSceneAsync(newScene, sceneMode);
                    async.allowSceneActivation = false;          // do not let scenes activate themselves to prevent stall
                }
                else
                {
                    SceneManager.LoadScene(newScene, sceneMode);
                    callback?.Invoke(newScene);
#if USE_LOGGING
                    Debug.Log("SceneLoadingFramework::LoadSceneInternal: " + newScene + " COMPLETE");
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogError("SceneLoadingFramework::Caught Exception " + e.Message + " while loading scene: " + newScene + " - might not be in your build settings?");
                async = null;
                yield break;
            }

            if (useAsyncLoading)
            {
                if (async != null)
                {
                    while(!async.isDone)
                    {
#if USE_LOGGING
                        Debug.Log("SceneLoadingFramework::" + newScene + " loading progress: " + async.progress + " async isdone: " + async.isDone);
#endif
                        if( async.progress >= 0.9f)
                        {
                            // now let the scenes activate
                            async.allowSceneActivation = true;

                            callback?.Invoke(newScene);
#if USE_LOGGING
                            Debug.Log("SceneLoadingFramework::LoadSceneInternal: async load " + newScene + " COMPLETE");
#endif
                            model.isLevelLoading = false;
                            model.levelLoading = string.Empty;
                            // exit out of this
                            yield break;
                        }

                        yield return null;
                    }
                }
                else
                {
                    async = null;
                    Debug.LogError("SceneLoadingFramework::Async loading of scene failed for scene: " + newScene + " - might not be in your build settings?");
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
        protected bool IsScene_CurrentlyLoaded_inEditor(string thisScene)
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
        protected bool IsScene_CurrentlyLoaded(string thisScene)
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                return IsScene_CurrentlyLoaded_inEditor(thisScene);
#else
                return false;
#endif
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == thisScene)
                    {
                        //if (scene.isLoaded)
                        //{
                            //the scene is already loaded
                            return true;
                        //}
                        //else
                        //{
                        //    return false;
                        //}
                    }
                }
                return false;   //scene not currently loaded in the hierarchy
            }
        }
    }
}