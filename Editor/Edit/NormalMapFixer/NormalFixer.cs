using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using Unity.EditorCoroutines.Editor;

// from : https://github.com/Milk-Drinker01/UnityNormalMapInverter
//GUI is based on Diabolickal's HDRP mask map packer, system changed to invert normal maps instead
// updated by gekido to support batch conversion

namespace PixelWizards.Utilities
{

    public class ConversionModel
    {
        public string Name;
        public string Path;
        public Texture NormalMap;
        public Texture2D r_NormalMap;
        public Texture2D finalTexture;
    }

    public class NormalFixer : EditorWindow
    {
        public List<ConversionModel> conversionList = new();

        private Vector2Int texSize;
        private static EditorWindow window;
        private Vector2 scrollPos;
        private int dragDropWidth = 750;
        private int dragDropHeight = 150;

        private TextureImporter rawImporter;
        private TextureImporterType textureType;
        private bool mipmapEnabled;
        private bool isReadable;
        private FilterMode filterMode;
        private TextureImporterNPOTScale npotScale;
        private TextureWrapMode wrapMode;
        private bool sRGBTexture;
        private System.Int32 maxTextureSize;
        private TextureImporterCompression textureCompression;
        public const string OBJECTBASEPATH = "Assets/Converted/";

        private string path;
        private bool showObjectLog = false;
        private StringBuilder logMsg = new StringBuilder();
        private string objectLog = string.Empty;
        private GUIStyle log;
        private GUIStyle BigBold;
        private GUIStyle Wrap;
        private GUIStyle subTitle;
        private GUIStyle preview;
        private bool initialized = false;

        [MenuItem("Edit/Normal Map Correcter")]
        public static void ShowWindow()
        {
            window = GetWindow(typeof(NormalFixer), false);
        }

        private void Init()
        {
            path = OBJECTBASEPATH;
            log = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            BigBold = new GUIStyle
            {
                fontSize = 16,
                fontStyle = EditorStyles.boldLabel.fontStyle,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            Wrap = new GUIStyle
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            subTitle = new GUIStyle
            {
                richText = true,
                wordWrap = true,
                fontStyle = EditorStyles.boldLabel.fontStyle,
                alignment = TextAnchor.MiddleCenter
            };

            preview = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter
            };

            initialized = true;
        }

        private void OnInspectorUpdate()
        {
            if (!window)
            {
                window = GetWindow(typeof(NormalFixer), false);
                dragDropWidth = (int)window.position.size.x - 15;
            }

            if (!initialized)
            {
                Init();
            }
        }

        private void OnGUI()
        {
            if (!window)
            {
                window = GetWindow(typeof(NormalFixer), false);
            }

            GUILayout.BeginArea(new Rect(0, 0, window.position.size.x, window.position.size.y));
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(10f);
                    GUILayout.Label("Convert DirectX Normal Map to OpenGL", BigBold);
                    GUILayout.Space(10f);
                    GUILayout.Label("Or from OpenGL to DirectX for some reason, you weirdo", subTitle);
                    GUILayout.Space(10f);

                    //Normal Map Input
                    GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(window.position.size.x - 15));
                    {
                        GUILayout.Space(10f);

                        // NormalMap = (Texture2D)EditorGUILayout.ObjectField("Normal Map", 
                        //     NormalMap, 
                        //     typeof(Texture2D),
                        //     false);

                        // let the users drop objects so they can bulk edit
                        var textures = DropZone("Drag and Drop Normal Maps Here", (int)window.position.size.x - 25,
                            dragDropHeight);
                        if (textures != null)
                        {
                            foreach (Object entry in textures)
                            {
                                if (entry is not Texture2D texture2D) continue;

                                Debug.Log("Entry is texture 2d, adding to list");
                                conversionList.Add(new ConversionModel()
                                {
                                    Name = texture2D.name,
                                    NormalMap = texture2D,
                                });
                            }
                        }

                        GUILayout.Space(10f);
                    }
                    GUILayout.EndVertical();

                    if (showObjectLog)
                    {
                        GUILayout.Label(objectLog, log);
                    }
                    else
                    {
                        GUILayout.Label("Convert texture list:", EditorStyles.boldLabel);
                        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandHeight(true));
                        {
                            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true));
                            {
                                if (conversionList.Count > 0)
                                {
                                    for (var index = 0; index < conversionList.Count; index++)
                                    {
                                        var entry = conversionList[index];
                                        if (entry == null) continue;
                                        GUILayout.BeginHorizontal();
                                        {
                                            var localPath = path + entry.Name;
                                            GUILayout.Label(
                                                "\t<color=green>" + entry.Name + "</color>\t<b>Path:</b> <i>" +
                                                localPath +
                                                "</i>", log);
                                            if (GUILayout.Button("X", GUILayout.Width(20f), GUILayout.Height(20f)))
                                            {
                                                RemoveEntry(entry);
                                            }

                                            GUILayout.Space(5f);
                                        }
                                        GUILayout.EndHorizontal();
                                    }
                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndScrollView();
                        GUILayout.Space(15f);
                    }

                    GUILayout.Space(15);
                    GUILayout.Label("Current Save Path: " + path, EditorStyles.boldLabel);
                    GUILayout.Space(15);
                    GUILayout.BeginHorizontal();
                    {
                        if (conversionList.Count > 0)
                        {
                            if (GUILayout.Button("Invert Normal Maps", GUILayout.Width(250f), GUILayout.Height(35f)))
                            {
                                EditorCoroutineUtility.StartCoroutine(PackTextures(), this);
                            }
                        }

                        if (GUILayout.Button("Change Path", GUILayout.Width(250f), GUILayout.Height(35f)))
                        {
                            path = EditorUtility.SaveFolderPanel("Save converted textures to folder:", path,
                                OBJECTBASEPATH);
                            path += "/";

                            if (!Directory.Exists(path))
                            {
                                GUILayout.Label(objectLog, log);
                            }
                        }

                        if (GUILayout.Button("Clear List)", GUILayout.Width(250f), GUILayout.Height(35f)))
                        {
                            Log("Clearing conversion list...");
                            conversionList.Clear();
                            logMsg.Clear();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(25);

                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        private void RemoveEntry(ConversionModel entry)
        {
            conversionList.Remove(entry);
        }

        /// <summary>
        /// Returns a list of objects that were dropped on us
        /// </summary>
        /// <param name="title"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static object[] DropZone(string title, int w, int h)
        {
            GUILayout.Box(title, GUILayout.Width(w), GUILayout.Height(h));

            EventType eventType = Event.current.type;
            bool isAccepted = false;

            if (eventType != EventType.DragUpdated && eventType != EventType.DragPerform)
                return isAccepted ? DragAndDrop.objectReferences : null;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (eventType == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                isAccepted = true;
            }

            Event.current.Use();

            if (isAccepted)
            {
                Debug.Log("Drag and Drop complete: " + DragAndDrop.objectReferences.Length + " objects dropped!");
            }

            return isAccepted ? DragAndDrop.objectReferences : null;
        }

        private IEnumerator PackTextures()
        {
            if (conversionList.Count <= 0) yield break;
            
            var interval = 1 / conversionList.Count;
            var progress = 0;
            var msg = "Packing " + conversionList.Count + " Textures, please wait...";
            EditorUtility.DisplayProgressBar(msg, "", 0f);
            yield return new WaitForSeconds(1f);
            Log(msg);
            foreach (var entry in conversionList)
            {
                progress += interval;
                var filePath = path + entry.Name + ".png";
                msg = "Packing " + entry.Name + " to path: + " + filePath + "please wait...";
                EditorUtility.DisplayProgressBar(msg, "", progress);
                yield return new WaitForSeconds(0.1f);
                Log(msg);
                // copy and convert the texture into entry.finalTexture
                UpdateTexture(entry, false);
                yield return new WaitForSeconds(0.1f);

                Log("Encoding PNG...");
                var pngData = entry.finalTexture.EncodeToPNG();
                yield return new WaitForSeconds(0.1f);
                if (filePath.Length != 0)
                {
                    if (pngData != null)
                    {
                        Log("Writing file to path: " + filePath);
                        File.WriteAllBytes(filePath, pngData);
                    }
                }
                yield return new WaitForSeconds(0.1f);
                Log("Refresh Asset database...");
                AssetDatabase.Refresh();
                yield return new WaitForSeconds(0.1f);

                Log("revert original texture settings");
                //restore original texture settings
                rawImporter.textureType = textureType;
                rawImporter.mipmapEnabled = mipmapEnabled;
                rawImporter.isReadable = isReadable;
                rawImporter.filterMode = filterMode;
                rawImporter.npotScale = npotScale;
                rawImporter.wrapMode = wrapMode;
                rawImporter.sRGBTexture = sRGBTexture;
                rawImporter.maxTextureSize = maxTextureSize;
                rawImporter.textureCompression = textureCompression;
                rawImporter.SaveAndReimport();

                Log("Save and Reimport...");
                // reset new normal settings
                TextureImporter NormalImporter = (TextureImporter)AssetImporter.GetAtPath(filePath);
                NormalImporter.textureType = TextureImporterType.NormalMap;
                NormalImporter.maxTextureSize = maxTextureSize;
                NormalImporter.SaveAndReimport();
                yield return new WaitForSeconds(0.1f);
                Log("Texture Saved to: " + filePath);
            }

            msg = "Completed packing textures!";
            EditorUtility.DisplayProgressBar(msg, "", 1f);
            yield return new WaitForSeconds(1f);
            Log(msg);
            EditorUtility.ClearProgressBar();
            yield return new WaitForSeconds(1f);
        }

        private void UpdateTexture(ConversionModel entry, bool asPreview)
        {
            Log("Update Texture(): " + entry.Name);
            Log("Get Raw Normal Map");
            entry.r_NormalMap = (Texture2D)GetRawTexture(entry.NormalMap);
            Log("Create Final Texture");
            entry.finalTexture = new Texture2D(texSize.x, texSize.y, TextureFormat.RGBAFloat, true);
            Log("Converting...");
            for (int x = 0; x < texSize.x; x++)
            {
                for (int y = 0; y < texSize.y; y++)
                {
                    float R, G, B;
                    R = entry.r_NormalMap.GetPixel(x, y).r;
                    //R = r_NormalMap.getr(x, y).r;
                    G = 1 - entry.r_NormalMap.GetPixel(x, y).g;
                    B = entry.r_NormalMap.GetPixel(x, y).b;

                    entry.finalTexture.SetPixel(x, y, new Color(R, G, B));
                }
            }
            
            Log("Conversion completed, applying to final texture...");
            entry.finalTexture.Apply();
        }

        private Texture GetRawTexture(Texture original, bool sRGBFallback = false)
        {
            string originalPath = AssetDatabase.GetAssetPath(original);
            rawImporter = (TextureImporter)AssetImporter.GetAtPath(originalPath);

            //get current settings
            textureType = rawImporter.textureType;
            mipmapEnabled = rawImporter.mipmapEnabled;
            isReadable = rawImporter.isReadable;
            filterMode = rawImporter.filterMode;
            npotScale = rawImporter.npotScale;
            wrapMode = rawImporter.wrapMode;
            sRGBTexture = rawImporter.sRGBTexture;
            maxTextureSize = rawImporter.maxTextureSize;
            textureCompression = rawImporter.textureCompression;

            //set the required setings for the conversion
            rawImporter.textureType = TextureImporterType.Default;
            rawImporter.mipmapEnabled = false;
            rawImporter.isReadable = true;
            //rawImporter.filterMode = m_bilinearFilter ? FilterMode.Bilinear : FilterMode.Point;
            rawImporter.filterMode = true ? FilterMode.Bilinear : FilterMode.Point;
            rawImporter.npotScale = TextureImporterNPOTScale.None;
            rawImporter.wrapMode = TextureWrapMode.Clamp;

            int w, h;
            rawImporter.GetSourceTextureWidthAndHeight(out w, out h);
            texSize = new Vector2Int(w, h);

            Texture2D originalTex2D = original as Texture2D;
            rawImporter.sRGBTexture = (originalTex2D == null)
                ? sRGBFallback
                : (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(original)) as TextureImporter).sRGBTexture;

            rawImporter.maxTextureSize = 8192;

            rawImporter.textureCompression = TextureImporterCompression.Uncompressed;

            rawImporter.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture>(originalPath);
        }

        private void Log(string msg)
        {
            logMsg.AppendLine(msg);
            Debug.Log(msg);
        }
    }
}