using PixelWizards.Utilities;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelWizards.MultiScene
{
	/// <summary>
	/// Stores a single config of a multi-scene loading scenario. You can have any number of them in a single project.
	/// 
	/// Common scenarios include having a 'master' setup with key scenes for gameplay, camera, etc and then additional
	/// configs for individual sets (that contain sub-scenes for set / lighting / fx etc)
	/// </summary>
	[System.Serializable]
	public class SceneConfig
	{
        /// <summary>
        /// The name of this set of scenes
        /// </summary>
		[Header("Name")]
		public string name = "Main Scenes";

        /// <summary>
        /// Should we set a specific scene active as the lighting scene?
        /// </summary>
        [Header("Set Scene Active?")]
        public bool setSceneActive = false;
        public string activeSceneName = string.Empty;

        /// <summary>
        /// List of scenes that are in this set
        /// </summary>
		[SerializeField]
		[Header("Scene List")]
		public List<SceneReference> sceneList = new List<SceneReference>();
	}

	/// <summary>
	/// Scriptable Object for the Multi-Scene loading system, also provides an integrated API for loading the scenes
	/// </summary>
    [CreateAssetMenu(fileName = "Multi-Scene Loader", menuName = "Scene Management/Multi-Scene Loader", order = 2)]
    public class MultiSceneLoader : ScriptableObject
    {
        /// <summary>
        /// The list of Configs that we can load
        /// </summary>
		[Header("Scene Config")]
		public List<SceneConfig> config = new List<SceneConfig>();

		/// <summary>
		/// Unloads any scenes currently loaded and then loads all of the defined scenes from config 
		/// </summary>
        public void LoadAllScenes()
		{
            Debug.Log("MultiSceneLoader::LoadAllScenes()");
			if (config.Count == 0)
			{
				Debug.LogError("No scene configs have been defined - nothing to load!");
				return;
			}

			if (config[0].sceneList[0] == null)
			{
				Debug.LogError("Scene config doesn't have any scenes defined - nothing to load!");
			}
#if UNITY_EDITOR
			if( ! Application.isPlaying)
			{
				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
			}
#endif

			// load the first scene in the list
			LoadScene(config[0].sceneList[0], false);

			// load the rest of the scenes
			for( var i = 0; i < config.Count; i++)
			{
				var counter = 0;
				if( i == 0)
				{
					// skip the first scene since we already loaded it
					counter = 1;
				}
				for (int j = counter; j < config[i].sceneList.Count; j++)
				{
					LoadScene(config[i].sceneList[j], true);
				}
			}
		}

        /// <summary>
        /// Load a specific scene config, optionally unloads existing first (ie can be optionally additively loaded)
        /// </summary>
        /// <param name="config"></param>
        /// <param name="unloadExisting"></param>
        public void LoadSceneConfig( SceneConfig config, bool unloadExisting)
		{
			for( int i = 0; i < config.sceneList.Count; i++)
			{
				if (i == 0)
				{
					// if we need to unload existing, then load the first in single mode, otherwise everything is additive
					LoadScene(config.sceneList[i], !unloadExisting);
				}
				else
				{
					// and the rest additive
					LoadScene(config.sceneList[i], true);
				}
			}
		}

        /// <summary>
        /// Loads a scene config by name
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="unloadExisting"></param>
        public void LoadSceneConfigByName(string configName, bool unloadExisting)
        {
            foreach (var entry in config)
            {
                if( entry.name == configName)
                {
                    LoadSceneConfig(entry, unloadExisting);
                    return;
                }
            }
            Debug.LogWarning("MultiSceneLoader::LoadSceneConfigByName() - could not find config: " + configName);
        }

        /// <summary>
        /// Unloads any scenes within the specified config
        /// </summary>
        /// <param name="thisConfig"></param>
        public void UnloadConfig( string thisConfig)
        {
            foreach( var entry in config)
            {
                if( entry.name == thisConfig)
                {
                    UnloadConfig(entry);
                }
            }
        }

        /// <summary>
        /// Unloads any scene from a specific config ( if they are loaded )
        /// </summary>
        /// <param name="thisConfig"></param>
        public void UnloadConfig( SceneConfig thisConfig)
        {
            var loadedSceneCount = SceneManager.sceneCount;
            for( var i = 0; i < loadedSceneCount; i++)
            {
                var loadedScene = SceneManager.GetSceneAt(i);
                foreach( var scene in thisConfig.sceneList)
                {
                    if( loadedScene.name == scene.SceneName)
                    {
                        if( Application.isPlaying)
                        {
                            if (IsScene_CurrentlyLoaded(loadedScene.name))
                            {
                                if (loadedScene.isLoaded)
                                {
                                  //  Debug.Log("Unload scene: " + loadedScene.name);
                                    SceneManager.UnloadSceneAsync(loadedScene);
                                }
                            }
                        }
#if UNITY_EDITOR
                        else
                        {
                            EditorSceneManager.CloseScene(loadedScene, true);
                        }
#endif        
                    }
                }
            }
        }

        /// <summary>
        /// Loads an individual scene, optionally done additively. This is a wrapper for SceneManager (runtime loading) and EditorSceneManager (edit-time loading) for scenes
        /// </summary>
        /// <param name="thisScene">the scene you would like to load</param>
        /// <param name="isAdditive">whether you want to use additve loading or not</param>
		private void LoadScene( SceneReference thisScene, bool isAdditive)
		{
			if (thisScene == null)
			{
				Debug.Log("Scene config has empty scene!");
				return;
			}

			if (isAdditive)
			{
				if (Application.isPlaying)
				{
					if (SceneManager.GetSceneByName(thisScene.SceneName) == null)
						Debug.LogError("Scene: " + thisScene.SceneName + " doesn't exist in build settings");
					else
                    {
                        if(!IsScene_CurrentlyLoaded(thisScene.SceneName))
                        {
                            SceneManager.LoadScene(thisScene.SceneName, LoadSceneMode.Additive);
                            // kick the light probes
                            LightProbes.TetrahedralizeAsync();
                        }
                    }
				}
				else
				{
#if UNITY_EDITOR
					EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(thisScene.Scene), OpenSceneMode.Additive);
                    // kick the light probes
                    LightProbes.TetrahedralizeAsync();
#endif
                }
			}
			else
			{
				if (Application.isPlaying)
				{
                    if (SceneManager.GetSceneByName(thisScene.SceneName) == null)
                        Debug.LogError("Scene: " + thisScene.SceneName + " doesn't exist in build settings");
                    else
                    if (!IsScene_CurrentlyLoaded(thisScene.SceneName))
                    {
                        SceneManager.LoadScene(thisScene.SceneName, LoadSceneMode.Single);
                        // kick the light probes
                        LightProbes.TetrahedralizeAsync();
                    }
				}
				else
				{

#if UNITY_EDITOR
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(thisScene.Scene), OpenSceneMode.Single);
                    // kick the light probes
                    LightProbes.TetrahedralizeAsync();
#endif
                }
			}
		}

        /// <summary>
        /// Check if a scene is loaded at edit time
        /// </summary>
        /// <param name="thisScene"></param>
        /// <returns></returns>
#if UNITY_EDITOR
        private bool IsScene_CurrentlyLoaded_inEditor(string thisScene)
        {
            for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; ++i)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);

                if (scene.name == thisScene)
                {
                    return true; //the scene is already loaded
                }
            }

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
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == thisScene)
                {
                    //the scene is already loaded
                    return true;
                }
            }

            return false;   //scene not currently loaded in the hierarchy
        }
    }
}