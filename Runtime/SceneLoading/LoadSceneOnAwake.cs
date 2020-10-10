using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnAwake : MonoBehaviour
{
    public string sceneName;
    public LoadSceneMode loadSceneMode = LoadSceneMode.Single;
    public bool useAsyncLoading = false;

    private bool isLevelLoading = false;

    // Start is called before the first frame update
    void Start()
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        Debug.Log("LoadSceneOnAwake() - checking for scene in build settings..." + sceneName);

        if (SceneManager.GetSceneByName(sceneName) == null)
        {
            Debug.LogError("Scene: " + sceneName + " doesn't exist in build settings");
            return;
        }

        if( useAsyncLoading)
        {
            StartCoroutine(LoadSceneInternal(sceneName, loadSceneMode, useAsyncLoading, callback =>
            {
                Debug.Log("Scene : " + sceneName + " completed loading...");
            }));
        }
        else
        {
            SceneManager.LoadScene(sceneName, loadSceneMode);
        }
    }

    /// <summary>
    /// Async level loading using the new Unity 5 Scene Management API
    /// </summary>
    private IEnumerator LoadSceneInternal(string newScene, LoadSceneMode sceneMode, bool useAsyncLoading, Action<string> callback = null)
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
                    Debug.Log("Loading progress..." + async.progress);
                    isLevelLoading = true;
                    yield return null;
                }

                if (async.isDone)
                {
                    isLevelLoading = false;

                    callback?.Invoke(newScene);

                    Debug.Log("LoadSceneInternal: async load " + newScene + " COMPLETE");

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
}
