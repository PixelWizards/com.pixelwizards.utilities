using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.MultiScene
{
    /// <summary>
    /// all of the localization for the UI
    /// </summary>
    public static class Loc
    {
        public const string WindowTitle = "Multi-Scene Config";
        public const string TopDesc = "Allows you to define sets of scenes that can be loaded either as one 'set' or individually as desired. Useful for defining subsets of a project that different team members can work on independently.";
        public const string ConfigList = "Config List";
        public const string ConfigName = "Config name: ";
        public const string SceneName = "Scene:";
        public const string AddNewScene = "Add New Scene";
        public const string MoveConfig = "Move Config";
        public const string MoveTop = "Top";
        public const string MoveUp = "Up";
        public const string MoveDown = "Down";
        public const string MoveBottom = "Last";
        public const string RemoveConfig = "Remove Config";
        public const string AddNewConfig = "Add new Config";
        public const string NewConfigName = "New Config";
        public const string LoadAllScenes = "Load All Scenes";
        public const string LoadAllScenesDesc = "Loads all configs in the order they are defined";
        public const string LoadSubScenes = "Load Sub Config Scenes";
        public const string LoadSubScenesDesc = "Load scenes defined in a specific config, from above.";
        public const string LoadOnlyScenes = "Loads ONLY the scenes defined in ";
        public const string LoadXScenes = "Load {0} Scenes";
        public const string SceneLoading = "Scene Loaders";
        public const string SceneLoadTip = "Load ALL scenes, or only specific scenes from a given config.";
        public const string SceneList = "Scene List";
    }


    public class SceneConfigEntry
    {
        public SceneConfig config;              // the config this belongs to

        public SceneConfigEntry(SceneConfig thisConfig)
        {
            config = thisConfig;
        }
    }

    /// <summary>
    /// Custom inspector for the MultiScene Scriptable Object
    /// </summary>
    [CustomEditor(typeof(MultiSceneLoader))]
    public class MultiSceneLoaderEditor : Editor
    {
        /// <summary>
        /// The scriptable object that we are editing
        /// </summary>
        private static MultiSceneLoader sceneConfig;

        /// <summary>
        /// State for the foldout / dropdowns in the list UI
        /// </summary>
        private static Vector2 botScroll = Vector2.zero;
        private static bool botToggle = false;
        private static bool needToSave = false;

        private void OnEnable()
        {
            sceneConfig = (MultiSceneLoader)target;
        }

        /// <summary>
        /// Draw our inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            GUILayout.Label(Loc.WindowTitle, EditorStyles.boldLabel);

            GUILayout.Label(Loc.TopDesc, EditorStyles.helpBox);

            GUILayout.Space(15f);

            DrawDefaultInspector();

            EditorGUILayout.Space(10f);

            // show the load all scenes button all of the time
            GUILayout.Label(Loc.LoadAllScenes, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button(Loc.LoadAllScenes, GUILayout.MinHeight(100), GUILayout.Height(35)))
            {
                sceneConfig.LoadAllScenes();
            }
                
            GUILayout.Label(Loc.LoadAllScenesDesc, EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            // the sub scene loader if users want to 
            botToggle = EditorGUILayout.Foldout(botToggle, Loc.SceneLoading);
            if (botToggle)
            {
                botScroll = GUILayout.BeginScrollView(botScroll, false, true);
                {
                    EditorGUILayout.Space(5);
                    GUILayout.Label(Loc.LoadSubScenes, EditorStyles.boldLabel);
                    GUILayout.Label(Loc.LoadSubScenesDesc, EditorStyles.helpBox);

                    foreach (var entry in sceneConfig.config)
                    {
                        EditorGUILayout.Space(5);
                        var buttonText = string.Format(Loc.LoadXScenes, entry.name);
                        if (GUILayout.Button(buttonText, GUILayout.MinHeight(100), GUILayout.Height(35)))
                        {
                            sceneConfig.LoadSceneConfig(entry, true);
                        }
                            
                        GUILayout.Label(Loc.LoadOnlyScenes + entry.name + ".", EditorStyles.helpBox);
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.Space(5f);
            }

            EditorGUILayout.Space();

            if (GUI.changed)
            {
                needToSave = true;
            }

            if (GUILayout.Button("Save Changes", GUILayout.Height(35f)))
            {
                if (needToSave)
                {
                    SaveChanges(serializedObject);
                }
            }
        }

        private static void SaveChanges(SerializedObject thisObject)
        {
            thisObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(sceneConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}