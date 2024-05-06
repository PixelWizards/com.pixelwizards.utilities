using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UnityEditor.SceneManagement;

namespace PixelWizards.MultiScene
{
    [CustomEditor(typeof(MultiSceneSwapHelper))]
    public class MultiSceneSwapHelperEditor : Editor
    {
        private static MultiSceneSwapHelper helper;
     
        private static class Loc
        {
            public const string WindowTitle = "Multi-Scene Helper";
            public const string TopDesc = "Allows you to automatically load additional sub-scenes automatically, based on a MultiScene Loader config.";
            public const string MultiSceneConfig = "MultiScene Config";
            public const string CreateNewConfig = "Create";
            public const string AutoLoadInEditor = "Automatically load in editor";
            public const string LoadOnAwake = "Load Configs on Awake";
            public const string AutoLoadEditorTip = "Will automatically load the specific scene configs in the editor when THIS scene is loaded";
            public const string AutoLoadAwakeTip = "Will automatically load the specific scene configs on Awake at runtime when THIS scene is loaded";
            public const string SceneConfigTip = "The MultiScene Config allows you to define collections of scenes that can be loaded at once";
            public const string ConfigListTip = "Specify the list of Configs that you want to load, enter the name of the config";
        }

        
        private void OnEnable()
        {
            helper = (MultiSceneSwapHelper)target;
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

            EditorGUILayout.Space();
            
            // the multi-scene loader config
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(Loc.MultiSceneConfig);
                helper.multiSceneConfig = (MultiSceneLoader) EditorGUILayout.ObjectField(helper.multiSceneConfig, typeof(MultiSceneLoader), false);
                if (helper.multiSceneConfig == null)
                {
                    if (GUILayout.Button(Loc.CreateNewConfig))
                    {
                        CreateNewSceneLoader();
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(Loc.SceneConfigTip, EditorStyles.helpBox);
            
            // show our list of configs
            var helperConfig = new UnityEditor.SerializedObject(helper.multiSceneConfig);
            var sceneConfigList = helperConfig.FindProperty("config");
            EditorGUILayout.PropertyField(sceneConfigList);
            helperConfig.ApplyModifiedProperties();

            EditorGUILayout.Space();
            helper.loadConfigOnAwake = EditorGUILayout.Toggle(Loc.LoadOnAwake, helper.loadConfigOnAwake);
            GUILayout.Label(Loc.AutoLoadAwakeTip, EditorStyles.helpBox);
            EditorGUILayout.Space();
            
            helper.autoLoadConfigInEditor = EditorGUILayout.Toggle(Loc.AutoLoadInEditor, helper.autoLoadConfigInEditor);
            GUILayout.Label(Loc.AutoLoadEditorTip, EditorStyles.helpBox);
            
            EditorGUILayout.Space();
            
            // show our scene configs
            var configList = serializedObject.FindProperty("configList");
            EditorGUILayout.PropertyField(configList);
            GUILayout.Label(Loc.ConfigListTip, EditorStyles.helpBox);

            // Apply changes to the serializedProperty - always do this at the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(helper);
                EditorSceneManager.MarkSceneDirty(helper.gameObject.scene);
            }

        }
        
        /// <summary>
        ///  file browser pop up to create a new multi-scene loader asset and save it in the project
        /// </summary>
        private static void CreateNewSceneLoader()
        {
            var asset = ScriptableObject.CreateInstance<MultiSceneLoader>();

            var path = EditorUtility.SaveFilePanelInProject("Save MultiSceneLoader", "MultiSceneLoader.asset", "asset",
                "Please specify where to save the MultiScene Loader!");
            
            if(path.Length != 0)
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                helper.multiSceneConfig = asset;
            }
        }
    }
}