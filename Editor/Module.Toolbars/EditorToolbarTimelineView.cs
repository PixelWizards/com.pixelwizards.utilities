using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.Toolbars
{
    public class EditorToolbarTimelineView : EditorWindow
    {
        public static Dictionary<string, string> sceneLoader = new Dictionary<string, string>();

        private static Vector2 maxWindowSize = new Vector2(1920f, 50f);
        private static Vector2 minWindowSize = new Vector2(960f, 50f);
        private Vector2 curWindowSize = new Vector2(1920f, 50f);
        private Vector2 defaultButtonSize = new Vector2(115f, 35f);
        private Vector2 _scroll = Vector2.zero;

        private static ToolbarConfig config;

        private static bool initialized = false;

        private void OnEnable()
        {
            config = AssetDatabase.LoadAssetAtPath("Assets/Settings/Toolbar/ToolbarConfig.asset", typeof(ToolbarConfig)) as ToolbarConfig;
            if (config == null)
            {
                Debug.Log("Could not load toolbar config?");
            }
            else
            {
                initialized = true;
            }
        }

        [MenuItem(EditorToolbarLoc.TIMELINETOOLBAR_MENULABEL, false, -100)]
        static void Init()
        {
            var window = EditorWindow.GetWindow<EditorToolbarTimelineView>(EditorToolbarLoc.TIMELINETOOLBAR_WINDOWNAME);
           // RenderSettings.SetInitialFurCount();
            window.Show();
            window.maxSize = new Vector2(maxWindowSize.x, maxWindowSize.y);
            window.minSize = new Vector2(minWindowSize.x, minWindowSize.y);
            initialized = true;
        }

        void OnGUI()
        {
            if (!initialized)
                return;
            curWindowSize.x = position.width;
            curWindowSize.y = position.height;

            var colShortcuts = new Color(1f, 0.75f, 1f, 1f);
            var colPerformance = new Color(1f, 1f, 0.75f, 1f);
            var colOverrides = new Color(0.75f, 1f, 1f, 1f);

            //_scroll = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Width(width), GUILayout.Height(height));
            {
                GUILayout.BeginHorizontal(GUILayout.MinWidth(minWindowSize.x), GUILayout.MinHeight(minWindowSize.y));
                    GUILayout.Space(10f);

                        if( config.showSceneTools)
                        {
                            // toolbar buttons
                            GUILayout.BeginVertical();
                                GUILayout.Label(EditorToolbarLoc.TIMELINETOOLBAR_SCENESHORTCUTS, EditorStyles.centeredGreyMiniLabel);

                                GUILayout.BeginHorizontal();
                                    GUI.backgroundColor = colShortcuts;
                                    if (GUILayout.Button(EditorToolbarLoc.TIMELINETOOLBAR_SHORTCUT_MASTERTIMELINE, GUILayout.MaxWidth(defaultButtonSize.x), GUILayout.MaxHeight(defaultButtonSize.y)))
                                    {
                                    //    EditorUtilities.FindSceneObject("MasterTimeline");
                                    }

                                    if (GUILayout.Button(EditorToolbarLoc.TIMELINETOOLBAR_SHORTCUT_SCENESETTINGS, GUILayout.MaxWidth(defaultButtonSize.x), GUILayout.MaxHeight(defaultButtonSize.y)))
                                    {
                                    //    EditorUtilities.FindSceneObject("SceneSettings");
                                    }

                                    if (GUILayout.Button(EditorToolbarLoc.TIMELINETOOLBAR_GLOBALPOST, GUILayout.MaxWidth(defaultButtonSize.x), GUILayout.MaxHeight(defaultButtonSize.y)))
                                    {
                                    //    EditorUtilities.FindSceneObject("PostVolume");
                                    }
                                GUILayout.EndHorizontal();
                             GUILayout.EndVertical();
                        }
                        //if (RenderSettings.FindRenderSettingsObject() != null && config.showRenderSettings)
                        //{
                        //    GUILayout.BeginVertical();
                        //    GUILayout.Label(EditorToolbarLoc.TIMELINETOOLBAR_RENDERSETTINGS, EditorStyles.centeredGreyMiniLabel);
                        //    GUI.backgroundColor = colPerformance;

                        //    GUILayout.BeginHorizontal();
                        //    var detailSettings = RenderSettings.GetRenderSettings();
                        //    foreach (var setting in detailSettings)
                        //    {
                        //        if (GUILayout.Button(setting, GUILayout.MaxWidth(defaultButtonSize.x), GUILayout.MaxHeight(defaultButtonSize.y)))
                        //        {
                        //            RenderSettings.ActivateRenderSettings(setting);
                        //        }
                        //    }

                        //    if (GUILayout.Button(EditorToolbarLoc.TIMELINETOOLBAR_EDITRENDERSETTINGS, GUILayout.MaxWidth(defaultButtonSize.x), GUILayout.MaxHeight(defaultButtonSize.y)))
                        //    {
                        //        EditorUtilities.FindSceneObject("RenderSettings");
                        //    }
                        //    GUILayout.EndHorizontal();
                        //    GUILayout.EndVertical();
                        //}
                        GUILayout.BeginVertical();
                        GUILayout.Label(EditorToolbarLoc.TIMELINETOOLBAR_PERFORMANCE, EditorStyles.centeredGreyMiniLabel);
                        GUI.backgroundColor = colOverrides;
                        GUILayout.BeginHorizontal();
#if HDRP_FUR
                             GUILayout.BeginVertical();
                                GUILayout.Label(EditorToolbarLoc.TIMELINETOOLBAR_FURSHELLCOUNT);
                                RenderSettings.furShellCount = (int) EditorGUILayout.Slider(RenderSettings.furShellCount, RenderSettings.furMinCount, RenderSettings.furMaxCount, GUILayout.MinWidth( 100f));
                            GUILayout.EndVertical();
                            if (GUILayout.Button(EditorToolbarLoc.TIMELINETOOLBAR_UDPATEFUR, GUILayout.MaxWidth(defaultButtonSize.x), GUILayout.MaxHeight(defaultButtonSize.y)))
                            {
                                RenderSettings.UpdateFur();
                            }
#endif
                        GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            // EditorGUILayout.EndScrollView();
        }
    }
}