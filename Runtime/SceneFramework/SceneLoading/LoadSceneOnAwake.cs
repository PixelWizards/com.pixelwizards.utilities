using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelWizards.MultiScene
{
    /// <summary>
    /// Loads an scene when the current scene runs
    /// </summary>
    public class LoadSceneOnAwake : SceneLoadingFramework
    {
        [Header("The name of the scene to load (without the .unity at the end)")]
        public string sceneName;

        [Header("Whether we should use Single or Additive loading")]
        public LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        [Header("Whether we should do an async load or not")]
        public bool useAsyncLoading = false;

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

            if (useAsyncLoading)
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
    }
}