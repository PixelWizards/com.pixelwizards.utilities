using System;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelWizards.MultiScene
{

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

            StartCoroutine(LoadSceneInternal(model.levelLoading, LoadSceneMode.Single, useAsyncLoading, callback));
        }

        /// <summary>
        /// Load a new Level additively. Fires a callback with the name of the newly loaded level once the load is completed.
        /// </summary>
        protected void LoadSceneAdditive(string newEnvironment, bool useAsyncLoading, Action<string> callback = null)
        {
            model.levelLoading = newEnvironment;

            StartCoroutine(LoadSceneInternal(model.levelLoading, LoadSceneMode.Additive, useAsyncLoading, callback));
        }

        /// <summary>
        /// Set the specified scene to be the active one
        /// </summary>
        /// <param name="thisScene"></param>
        protected void SetActiveScene(string thisScene)
        {
            Debug.Log("Set active scene : " + thisScene);
            var activeScene = SceneManager.GetSceneByName(thisScene);
            SceneManager.SetActiveScene(activeScene);
        }

        protected void UnloadScene(string sceneName)
        {
            Debug.Log("Unload scene async : " + sceneName);
            SceneManager.UnloadSceneAsync(sceneName);
        }

        /// <summary>
        /// Async level loading using the new Unity 5 Scene Management API
        /// </summary>
        protected IEnumerator LoadSceneInternal(string newScene, LoadSceneMode sceneMode, bool useAsyncLoading, Action<string> callback = null)
        {
            Debug.Log("LoadSceneInternal: " + newScene);
            AsyncOperation async = new AsyncOperation();
            try
            {
                if (useAsyncLoading)
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

            if (useAsyncLoading)
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