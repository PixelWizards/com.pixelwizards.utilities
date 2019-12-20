
#if !(UNITY_5_3_OR_NEWER)
#define NO_ON_SELECTION_CHANGED
#endif


using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.Collections.Generic;


namespace Unity.IG.UVTools
{
    //
    // class UVInspector
    //

    public class UVInspector : EditorWindow
    {

        class InspectedObject
        {
            public UnityEngine.Object   obj = null;
            public GameObject           gameObject = null;
            public Mesh                 mesh = null;
            public bool[]               hasUV = { false, false, false, false };
            public bool                 hasColors = false;
            public int                  vertexCount = 0;
            public int                  triangleCount = 0;
			public int 					subMeshCount = 0;
            public int[]                triangles = null;
            public Vector3[]            vertices = null;
            public int[]                adjacency = null;
            public int[]                uvBorders = null;
        }

        enum UVSet
        {
            UV1 = 0, UV2 = 1, UV3 = 2, UV4 = 3
        }

		enum PreviewTextureSource
		{
			None,
			FromMaterial,
			Custom,
		}
		
		enum ColorChannels
		{
			R, G, B, A, All,
		}

        static class Styles
        {
            public static Color     backgroundColor = new Color32(71, 71, 71, 255);
            public static Color     gridColor       = new Color32(100, 100, 100, 255);
            public static Color     wireframeColor  = new Color32(255, 255, 255, 255);
            public static Color     wireframeColor2 = new Color32(93, 118, 154, 255);

            public static GUIStyle  logoFont;

            public static GUIStyle  hudFont;

            public static string[]  uvSetNames      = Enum.GetNames (typeof (UVSet));
			public static string[]  colorChannelsNames = Enum.GetNames (typeof (ColorChannels));

			public static GUIStyle  buttonLeft;
			public static GUIStyle  buttonMid;
			public static GUIStyle  buttonRight;

			public const int		kSubMeshButtonWitdh = 30;
			public static string[]  subMeshLabels = new string[32];

			public static Color     foldoutTintColor;

            static Styles()
            {
                logoFont = new GUIStyle(EditorStyles.label);
                logoFont.alignment = TextAnchor.MiddleCenter;
                logoFont.fontSize = 20;

                hudFont = new GUIStyle(EditorStyles.boldLabel);
                hudFont.alignment = TextAnchor.LowerCenter;
                hudFont.normal.textColor = Color.white;

				buttonLeft = GUI.skin.GetStyle("buttonLeft");
				buttonMid = GUI.skin.GetStyle("buttonMid");
				buttonRight = GUI.skin.GetStyle("buttonRight");

				for(int i = 0; i < 32; i++)
					subMeshLabels[i] = "#" + i.ToString();

				foldoutTintColor = EditorGUIUtility.isProSkin 
					? new Color (1f, 1f, 1f, 0.05f) : new Color (0f, 0f, 0f, 0.05f);
            }
        }

		static class nGUI
		{
			static int	s_ToggleHash = "nTools.nGUI.Toggle".GetHashCode();

			public static bool Toggle(Rect rect, bool value, string label, GUIStyle style)
			{
				Event e = Event.current;
				int controlID = GUIUtility.GetControlID(s_ToggleHash, FocusType.Passive, rect);
				
				switch(e.GetTypeForControl(controlID))
				{
				case EventType.MouseDown:
					if(rect.Contains(e.mousePosition) && e.button == 0)
					{
						GUIUtility.keyboardControl = controlID;
						GUIUtility.hotControl = controlID;
						e.Use();
					}
					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlID && e.button == 0)
					{
						GUI.changed = true;
						value = !value;
						GUIUtility.hotControl = 0;
						e.Use();
					}
					break;
				case EventType.Repaint:
					{
						style.Draw(rect, label, GUI.enabled && GUIUtility.hotControl == controlID, GUI.enabled && GUIUtility.hotControl == controlID, value, false);
					}
					break;
				}

				return value;
			}

			public static bool Foldout(bool foldout, string content)
			{
				Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.foldout);
				
				EditorGUI.DrawRect(EditorGUI.IndentedRect(rect), Styles.foldoutTintColor);
				
				Rect foldoutRect = rect;
				foldoutRect.width = EditorGUIUtility.singleLineHeight;
				foldout = EditorGUI.Foldout(rect, foldout, "", true);
				
				rect.x += EditorGUIUtility.singleLineHeight;
				EditorGUI.LabelField(rect, content, EditorStyles.boldLabel);
				
				return foldout;
			}
		}



		class BitField32
		{
			public int bitfield;
		
			public bool this[int index]
			{
				get
				{
					return (bitfield & (1 << index)) != 0;
				}
				set
				{
					if(value)
						bitfield |= (1 << index);
					else 
						bitfield &= ~(1 << index);
				}
			}

			public static implicit operator BitField32(int value) 
			{
				return new BitField32(value);
			}

			public BitField32(int value)
			{
				bitfield = value;
			}

			public int ToInt()
			{
				return bitfield;
			}

			public int NumberOfSetBits(int bits)
			{
				int i = bitfield & (Mathf.RoundToInt(Mathf.Pow(2, bits)) - 1);	
				i = i - ((i >> 1) & 0x55555555);
				i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
				return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
			}
		}


        static int              s_UVPreviewWindowHash = "nTools.UVInspector.UVPreviewWindow".GetHashCode();
        static GUIContent       s_TempContent = new GUIContent();
				
		#if NO_ON_SELECTION_CHANGED
		const float             kUpdateInterval = 0.33f;
		float                   lastUpdateTime = 0;
		UnityEngine.Object[]    selectedObjects = null;
		#endif
		
		Material                uvPreviewMaterial = null;
        Material                simpleMaterial = null;
		Vector2					scroolPosition = Vector2.zero;
		Vector2                 previewWindowPosition = new Vector2(-0.5f, -0.5f);
		float                   previewWindowScale = 1.8f;
		Vector2                 viewportSize = new Vector2();
		
		
		List<InspectedObject>   inspectedObjects = new List<InspectedObject>();

        UVSet                   previewUVSet = UVSet.UV1;
		BitField32      		subMeshToggleField = ~0;
        bool                    showVertexColors = false;
        bool                    showGrid = true;
		bool					tilePreviewTexture = false;
		bool					showSubmeshesByOne = true;
        bool                    settingsFoldout = true;
		bool                    previewFoldout = true;
		PreviewTextureSource	previewTextureSource = PreviewTextureSource.None;
		Color                   previewTextureTintColor = Color.white;
		Texture2D               customPreviewTexture = null;
		ColorChannels			previewTextureChannels = ColorChannels.All;

		Texture2D               previewTexture = null;
		string 					preferredTextureProperty = "_MainTex";



        // Unity Editor Menu Item
        [MenuItem ("Window/Analysis/UV Inspector")]
        static void Init ()
        {
            // Get existing open window or if none, make a new one:
            UVInspector window = (UVInspector)EditorWindow.GetWindow (typeof (UVInspector));
            window.ShowUtility(); 
        }


        bool LoadMaterials()
        {
            Shader uvPreviewShader = Shader.Find("Hidden/nTools/UvInspector/UvPreview");
            Shader simpleShader = Shader.Find("Hidden/nTools/UvInspector/Simple");

            if(uvPreviewShader == null || simpleShader == null)
            {
                return false;
            }

            uvPreviewMaterial = new Material(uvPreviewShader);
			uvPreviewMaterial.hideFlags = HideFlags.HideAndDontSave;

            simpleMaterial = new Material(simpleShader);
			simpleMaterial.hideFlags = HideFlags.HideAndDontSave;
            return true;
        }


        void OnEnable () 
        {
            #if (UNITY_5_0)
            title = "UV Inspector";
            #else
            titleContent = new GUIContent("UV Inspector");
            #endif

			this.minSize = new Vector2(350, 400);

			previewUVSet = (UVSet)EditorPrefs.GetInt("nTools.UVInspector.previewUVSet", (int)previewUVSet);
			subMeshToggleField = EditorPrefs.GetInt("nTools.UVInspector.subMeshToggleField", subMeshToggleField.bitfield);
			showVertexColors = EditorPrefs.GetBool("nTools.UVInspector.showVertexColors", showVertexColors);
			showGrid = EditorPrefs.GetBool("nTools.UVInspector.showGrid", showGrid);
			showSubmeshesByOne = EditorPrefs.GetBool("nTools.UVInspector.showSubmeshesByOne", showSubmeshesByOne);
			tilePreviewTexture = EditorPrefs.GetBool("nTools.UVInspector.tilePreviewTexture", tilePreviewTexture);
			previewTextureSource = (PreviewTextureSource)EditorPrefs.GetInt("nTools.UVInspector.previewTextureSource", (int)previewTextureSource);
			customPreviewTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString("nTools.UVInspector.customPreviewTexture", ""), typeof(Texture2D));
			previewTextureTintColor = IntToColor(EditorPrefs.GetInt("nTools.UVInspector.previewTextureTintColor", ColorToInt(previewTextureTintColor)));
			preferredTextureProperty = EditorPrefs.GetString("nTools.UVInspector.preferredTextureProperty", preferredTextureProperty);
			previewTextureChannels = (ColorChannels)EditorPrefs.GetInt("nTools.UVInspector.previewTextureChannels", (int)previewTextureChannels);
			settingsFoldout = EditorPrefs.GetBool("nTools.UVInspector.settingsFoldout", settingsFoldout);
			previewFoldout = EditorPrefs.GetBool("nTools.UVInspector.previewFoldout", previewFoldout);


            if(!LoadMaterials())
            {
                Debug.LogWarning("UV Inspector Error: shaders not found. Reimport asset.");
                Close();
                return;
            }

            #if NO_ON_SELECTION_CHANGED
            if(EditorApplication.update != OnEditorUpdate)
                EditorApplication.update += OnEditorUpdate;
            #else
            if(Selection.selectionChanged != OnSelectionChanged)
                Selection.selectionChanged += OnSelectionChanged;
            #endif

            LoadMeshes();
        }




        void OnDisable () 
        {   
            #if NO_ON_SELECTION_CHANGED
            EditorApplication.update -= OnEditorUpdate;
            #else
            Selection.selectionChanged -= OnSelectionChanged;
            #endif

            EditorPrefs.SetInt("nTools.UVInspector.previewUVSet", (int)previewUVSet);
			EditorPrefs.SetInt("nTools.UVInspector.subMeshToggleField", subMeshToggleField.ToInt());
            EditorPrefs.SetBool("nTools.UVInspector.showVertexColors", showVertexColors);
            EditorPrefs.SetBool("nTools.UVInspector.showGrid", showGrid);
			EditorPrefs.SetBool("nTools.UVInspector.showSubmeshesByOne", showSubmeshesByOne);
			EditorPrefs.SetBool("nTools.UVInspector.tilePreviewTexture", tilePreviewTexture);
			EditorPrefs.SetInt("nTools.UVInspector.previewTextureSource", (int)previewTextureSource);
			if(customPreviewTexture != null)
				EditorPrefs.SetString("nTools.UVInspector.customPreviewTexture", AssetDatabase.GetAssetPath(customPreviewTexture) ?? "");
			EditorPrefs.SetInt("nTools.UVInspector.previewTextureTintColor", ColorToInt(previewTextureTintColor));
			EditorPrefs.SetString("nTools.UVInspector.preferredTextureProperty", preferredTextureProperty);
			EditorPrefs.SetInt("nTools.UVInspector.previewTextureChannels", (int)previewTextureChannels);
            EditorPrefs.SetBool("nTools.UVInspector.settingsFoldout", settingsFoldout);
			EditorPrefs.SetBool("nTools.UVInspector.previewFoldout", previewFoldout);
        }

        #if NO_ON_SELECTION_CHANGED
        void OnEditorUpdate()
        {
			// Sometimes Time.realtimeSinceStartup restarting
			if(Time.realtimeSinceStartup - lastUpdateTime < 0f)
				lastUpdateTime = 0f;
        			
            if(Time.realtimeSinceStartup - lastUpdateTime > kUpdateInterval)
            {
				
                lastUpdateTime = Time.realtimeSinceStartup;

                if(selectedObjects == null)
                {
                    selectedObjects = Selection.objects;

                    OnSelectionChanged();
                }
                else
                {
                    UnityEngine.Object[] currentSelection = Selection.objects;

                    if(selectedObjects.Length != currentSelection.Length)
                    {
                        selectedObjects = currentSelection;

                        OnSelectionChanged();
                    }
                    else
                    {
                        for(int i = 0; i < selectedObjects.Length; i++)
                        {
                            if(selectedObjects[i] != currentSelection[i])
                            {
                                selectedObjects = currentSelection;
                                OnSelectionChanged();
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endif


        void OnSelectionChanged()
        {
            LoadMeshes();
        }



		void AddObject(UnityEngine.Object obj, Mesh mesh, GameObject gameObject)
		{
			if (inspectedObjects.Count >= 10)
				return;

            // https://docs.unity3d.com/ScriptReference/ModelImporter-isReadable.html
            // In the Unity editor access is always permitted when not in play mode.

			InspectedObject inspectedObj = new InspectedObject();
			inspectedObj.obj = obj;
			inspectedObj.gameObject = gameObject;
			inspectedObj.mesh = mesh;
            inspectedObj.triangles = mesh.triangles;		
            inspectedObj.vertices = mesh.vertices;
			
						
            if(inspectedObj.triangles != null)
                inspectedObj.triangleCount = inspectedObj.triangles.Length/3;
			
			inspectedObj.vertexCount = inspectedObj.mesh.vertexCount;
			inspectedObj.subMeshCount = Mathf.Min (mesh.subMeshCount, 32);
			
			Vector2[] uvs;
			inspectedObj.hasUV[0] = (uvs = inspectedObj.mesh.uv) != null && uvs.Length > 0;
			inspectedObj.hasUV[1] = (uvs = inspectedObj.mesh.uv2) != null && uvs.Length > 0;
			inspectedObj.hasUV[2] = (uvs = inspectedObj.mesh.uv3) != null && uvs.Length > 0;
			inspectedObj.hasUV[3] = (uvs = inspectedObj.mesh.uv4) != null && uvs.Length > 0;
			
			Color32[] colors;
			inspectedObj.hasColors = (colors = inspectedObj.mesh.colors32) != null && colors.Length > 0;

			inspectedObjects.Add(inspectedObj);
		}


		void AddGameObject(GameObject gameObject)
		{
			MeshFilter meshFilter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
			if(meshFilter != null && meshFilter.sharedMesh != null)
			{
				AddObject(gameObject, meshFilter.sharedMesh, gameObject);
			}
			else
			{
				SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
				if(skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
				{
					AddObject(gameObject, skinnedMeshRenderer.sharedMesh, gameObject);
				}
			}
		}


        void LoadMeshes()
        {
            UnityEngine.Object[] selectedObjects = Selection.objects;
            int selectedObjectsCount = selectedObjects.Length;

            inspectedObjects.Clear();

            for(int i = 0; i < selectedObjectsCount; i++)
            {
				if(selectedObjects[i] is GameObject)
				{
					ForAllInHierarchy(selectedObjects[i] as GameObject, (go) => { AddGameObject(go);  });
				}
				else
				if(selectedObjects[i] is Mesh)
				{
					AddObject(selectedObjects[i], selectedObjects[i] as Mesh, null);
				}
            }

            Repaint();
        }





        void UVPreviewWindow(Rect rect)
        {
            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(s_UVPreviewWindowHash, FocusType.Passive, rect);

			switch(e.GetTypeForControl(controlID))
            {
            case EventType.MouseDown:
                if(rect.Contains(e.mousePosition) && e.alt)
                {
					if(e.button == 0 || e.button == 1 || e.button == 2)
					{
	                    GUI.changed = true;

	                    GUIUtility.keyboardControl = controlID;
	                    GUIUtility.hotControl = controlID;
	                    e.Use();
					}
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID && e.alt)
                {
                    GUI.changed = true;

                    if (e.button == 0 || e.button == 2)
                        previewWindowPosition += new Vector2(e.delta.x, -e.delta.y) * (2.0f / rect.width) / previewWindowScale;

                    if (e.button == 1)
					{   
						float aspect = Mathf.Min(viewportSize.x, viewportSize.y) / Mathf.Max(viewportSize.x, viewportSize.y, 1f);
						previewWindowScale += e.delta.magnitude / aspect * Mathf.Sign(Vector2.Dot(e.delta, new Vector2(1.0f, 0.0f))) * (2.0f / rect.width) * (previewWindowScale) * 0.5f;
                        previewWindowScale = Mathf.Max(previewWindowScale, 0.01f);
                    }

                    e.Use();
                }
                break;
            case EventType.ScrollWheel:
                if(rect.Contains(e.mousePosition))
                {
                    GUI.changed = true;

					float aspect = Mathf.Min(viewportSize.x, viewportSize.y) / Mathf.Max(viewportSize.x, viewportSize.y, 1f);

					previewWindowScale += e.delta.magnitude / aspect * Mathf.Sign(Vector2.Dot(e.delta, new Vector2(1.0f, -0.1f).normalized)) * (2.0f / rect.width) * (previewWindowScale) * 5.5f;                
                    previewWindowScale = Mathf.Max(previewWindowScale, 0.01f);

                    e.Use();
                }
                break;
            case EventType.Repaint:
                {
                
					GUI.BeginGroup (rect);
                                                        				

                    Rect viewportRect = rect;
                    
					viewportRect.position = viewportRect.position - scroolPosition;// apply scroll 	
					
                    // clamp rect position zero
                    if(viewportRect.position.x < 0f)
                    {
                        viewportRect.width += viewportRect.position.x; // -= abs(x)
                        viewportRect.position = new Vector2(0f, viewportRect.position.y);

                        if(viewportRect.width <= 0f)
                            break;
                    }
                    if(viewportRect.position.y < 0f)
                    {
                        viewportRect.height += viewportRect.position.y; // -= abs(y)
                        viewportRect.position = new Vector2(viewportRect.position.x, 0f);

                        if(viewportRect.height <= 0f)
                            break;
                    }

                    viewportSize = rect.size; // save size

                    // convert gui to screen coord
                    Rect screenViewportRect = viewportRect;
                    screenViewportRect.y = this.position.height - screenViewportRect.y - screenViewportRect.height; 
                                                           
					#if (UNITY_5_4_OR_NEWER)
                    GL.Viewport(EditorGUIUtility.PointsToPixels(screenViewportRect));
					#else	
					GL.Viewport(screenViewportRect);
					#endif
                    GL.PushMatrix();

                    // Clear bg
					{
					    GL.LoadIdentity();
						GL.LoadProjectionMatrix(Matrix4x4.Ortho(0f, 1f, 0f, 1f, -1f, 1f));

						SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", false);
						SetMaterialKeyword(simpleMaterial, "_NORMALMAP", false);
						simpleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
						simpleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
						simpleMaterial.SetTexture("_MainTex", null);
						simpleMaterial.SetColor("_Color", Color.white);
						simpleMaterial.SetPass(0);

						GL.Begin(GL.TRIANGLE_STRIP);
						GL.Color(Styles.backgroundColor);
						GL.Vertex3(1, 0, 0);
						GL.Vertex3(0, 0, 0);
						GL.Vertex3(1, 1, 0);
						GL.Vertex3(0, 1, 0);
						GL.End();
					}

				    GL.LoadIdentity();
                    //float aspect = Mathf.Min(GUIUtility.ScreenToGUIRect(this.position).height - rect.y, rect.height) / rect.width;
                    float aspect = viewportRect.height / viewportRect.width;
                    Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-1f, 1f, -1f * aspect, 1f * aspect, -1f, 1f);
				    GL.LoadProjectionMatrix(projectionMatrix);
                    Matrix4x4 viewMatrix = Matrix4x4.Scale(new Vector3(previewWindowScale, previewWindowScale, previewWindowScale))
						* Matrix4x4.TRS(new Vector3(previewWindowPosition.x, previewWindowPosition.y, 0), Quaternion.identity, Vector3.one); // u5.0 have no translate
                    GL.MultMatrix(viewMatrix);


					// Preview texture
					if((previewTextureSource == PreviewTextureSource.Custom && customPreviewTexture != null) || 
				       (previewTextureSource == PreviewTextureSource.FromMaterial && previewTexture != null))
					{
						Texture2D texture = (previewTextureSource == PreviewTextureSource.Custom) ? customPreviewTexture : previewTexture;
						
						SetMaterialKeyword(simpleMaterial, "_NORMALMAP", false);
											
						string texPath = AssetDatabase.GetAssetPath(texture);
						if(texPath != null) {
							TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath (texPath);
							if(textureImporter != null) {
							#if (UNITY_5_5_OR_NEWER)
								if(textureImporter.textureType == TextureImporterType.NormalMap)
									SetMaterialKeyword(simpleMaterial, "_NORMALMAP", true);
							#else								
								if(textureImporter.textureType == TextureImporterType.Bump)
									SetMaterialKeyword(simpleMaterial, "_NORMALMAP", true);
							#endif
							}
						}							
						
						switch(previewTextureChannels) {
						case ColorChannels.R:
							SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", true);
							simpleMaterial.SetColor("_Color", new Color(1,0,0,0));
							break;
						case ColorChannels.G:
							SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", true);
							simpleMaterial.SetColor("_Color", new Color(0,1,0,0));
							break;
						case ColorChannels.B:
							SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", true);
							simpleMaterial.SetColor("_Color", new Color(0,0,1,0));
							break;
						case ColorChannels.A:
							SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", true);
							simpleMaterial.SetColor("_Color", new Color(0,0,0,1));
							break;
						case ColorChannels.All:
							SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", false);
							simpleMaterial.SetColor("_Color", new Color(1,1,1,1));
							break;						
						}
						
						simpleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
						simpleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
												
						simpleMaterial.SetTexture("_MainTex", texture);
						simpleMaterial.SetPass(0);
						
						float min = tilePreviewTexture ? -100f : 0;
						float max = tilePreviewTexture ? 100f : 1;
					
						GL.Begin(GL.TRIANGLE_STRIP);
						GL.Color(previewTextureTintColor);
						GL.TexCoord2(max, min);
						GL.Vertex3(max, min, 0);
						GL.TexCoord2(min, min);
						GL.Vertex3(min, min, 0);
						GL.TexCoord2(max, max);
						GL.Vertex3(max, max, 0);
						GL.TexCoord2(min, max);
						GL.Vertex3(min, max, 0);
						GL.End();      
					}
				


                    // grid
                    if(showGrid)
                    {
                        GL.wireframe = false;

						SetMaterialKeyword(simpleMaterial, "_COLOR_MASK", false);
						SetMaterialKeyword(simpleMaterial, "_NORMALMAP", false);
                        simpleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        simpleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        simpleMaterial.SetTexture("_MainTex", null);
						simpleMaterial.SetColor("_Color", Color.white);
                        simpleMaterial.SetPass(0);


                        GL.Begin(GL.LINES);

                        float x = -1.0f;
                        GL.Color(Styles.gridColor);
                        for(int i = 0; i <= 20; i++, x+=0.1f)
                        {
                            GL.Vertex3(x, 1, 0);
                            GL.Vertex3(x, -1, 0);
                        }


                        float y = -1.0f;
                        GL.Color(Styles.gridColor);
                        for(int i = 0; i <= 20; i++, y+=0.1f)
                        {
                            GL.Vertex3(1, y, 0);
                            GL.Vertex3(-1, y, 0);
                        }

                        GL.Color(Color.gray);
                        GL.Vertex3(1, 0, 0);
                        GL.Vertex3(-1, 0, 0);
                        GL.Vertex3(0, 1, 0);
                        GL.Vertex3(0, -1, 0);

                        GL.Color(Color.red);
                        GL.Vertex3(0.3f, 0, 0);
                        GL.Vertex3(0, 0, 0);


                        GL.Color(Color.green);
                        GL.Vertex3(0, 0.3f, 0);
                        GL.Vertex3(0, 0, 0);

                        GL.End();
                    }



                    // mesh uvs
                    {
                        SetMaterialKeyword(uvPreviewMaterial, "_UV1", false);
                        SetMaterialKeyword(uvPreviewMaterial, "_UV2", false);
                        SetMaterialKeyword(uvPreviewMaterial, "_UV3", false);

                        switch(previewUVSet)
                        {
                        case UVSet.UV2:
                            SetMaterialKeyword(uvPreviewMaterial, "_UV1", true);
                            break;
                        case UVSet.UV3:
                            SetMaterialKeyword(uvPreviewMaterial, "_UV2", true);
                            break;
                        case UVSet.UV4:
                            SetMaterialKeyword(uvPreviewMaterial, "_UV3", true);
                            break;
                        }


                        GL.wireframe = true;


                        for(int i = 0; i < inspectedObjects.Count; i++)
                        {    
                            SetMaterialKeyword(uvPreviewMaterial, "_VERTEX_COLORS", showVertexColors && inspectedObjects[i].hasColors);

                            if(i == inspectedObjects.Count-1)
                            {
                                uvPreviewMaterial.SetColor("_Color", Styles.wireframeColor);
                            }
                            else
                            {
                                uvPreviewMaterial.SetColor("_Color", Styles.wireframeColor2);
                            }

                            uvPreviewMaterial.SetPass(0);

							if(inspectedObjects.Count == 1)
							{
								for(int j = 0; j < inspectedObjects[i].subMeshCount && j < 32; j++)
								{
									if(subMeshToggleField[j])
										Graphics.DrawMeshNow(inspectedObjects[i].mesh, viewMatrix, j);
								}
							}
							else {
	                            Graphics.DrawMeshNow(inspectedObjects[i].mesh, viewMatrix);
							}
						}
                    }

                    GL.PopMatrix();
					GL.wireframe = false;
					
					GUI.EndGroup();

                    // grid numbers
                    if(showGrid)
                    {
                        GUI.BeginGroup (rect);
                        Matrix4x4 MVPMatrix = (projectionMatrix * viewMatrix);
                        DrawLabel(new Vector3(0, 0, 0), rect, MVPMatrix, "0.0", EditorStyles.whiteMiniLabel, TextAnchor.MiddleLeft);
                        DrawLabel(new Vector3(0, 1, 0), rect, MVPMatrix, "1.0", EditorStyles.whiteMiniLabel, TextAnchor.MiddleLeft);
                        DrawLabel(new Vector3(0, -1, 0), rect, MVPMatrix, "-1.0", EditorStyles.whiteMiniLabel, TextAnchor.UpperLeft);
                        DrawLabel(new Vector3(1, 0, 0), rect, MVPMatrix, "1.0", EditorStyles.whiteMiniLabel, TextAnchor.MiddleLeft);
                        DrawLabel(new Vector3(-1, 0, 0), rect, MVPMatrix, "-1.0", EditorStyles.whiteMiniLabel, TextAnchor.MiddleRight);
						GUI.EndGroup();
                    }

                }
                break;
            }

            return;
        }




		void SubMeshToolbar(int buttonCount)
		{
			if(buttonCount == 0)
				return;
						 

			buttonCount = Mathf.Min (buttonCount, 32);

			Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(Styles.kSubMeshButtonWitdh * buttonCount), GUILayout.Height(20));
			rect.width = Styles.kSubMeshButtonWitdh;


			if(buttonCount == 1)
			{
				subMeshToggleField[0] = nGUI.Toggle(rect, subMeshToggleField[0], Styles.subMeshLabels[0], "button");
				return;
			}

			int i = 0;

			subMeshToggleField[i] = nGUI.Toggle(rect, subMeshToggleField[i], Styles.subMeshLabels[i], Styles.buttonLeft);
			rect.x += Styles.kSubMeshButtonWitdh;

			for(i = 1; i < buttonCount-1; i++)
			{
				subMeshToggleField[i] = nGUI.Toggle(rect, subMeshToggleField[i], Styles.subMeshLabels[i], Styles.buttonMid);
				rect.x += Styles.kSubMeshButtonWitdh;
			}

			subMeshToggleField[i] = nGUI.Toggle(rect, subMeshToggleField[i], Styles.subMeshLabels[i], Styles.buttonRight);
		}





		void PreviewTextureGUI()
		{
			if (inspectedObjects.Count != 1 || subMeshToggleField.NumberOfSetBits (inspectedObjects[0].subMeshCount) != 1)
			{
				goto DISPLAY_DIMMED_PROPERTY;
			}
			
			if(inspectedObjects[0].gameObject == null)
				goto DISPLAY_DIMMED_PROPERTY;
			
			Material[] objectMaterials = null;			
			Renderer renderer = inspectedObjects[0].gameObject.GetComponent (typeof(Renderer)) as Renderer;
			if (renderer != null) {
				objectMaterials = renderer.sharedMaterials;
			}


			if (objectMaterials == null)
				goto DISPLAY_DIMMED_PROPERTY;
			
			int preferredTexturePropertyIndex = 0;
			int currentSubmesh = Mathf.RoundToInt(Mathf.Log((float)(subMeshToggleField.bitfield & (Mathf.RoundToInt(Mathf.Pow(2, inspectedObjects[0].subMeshCount)) - 1)), 2f));

			if(currentSubmesh < 0 || currentSubmesh >= objectMaterials.Length)
				goto DISPLAY_DIMMED_PROPERTY;				
			
			if(objectMaterials[currentSubmesh] == null)
				goto DISPLAY_DIMMED_PROPERTY;
			
			Shader shader = objectMaterials[currentSubmesh].shader;
			if(shader == null)
				goto DISPLAY_DIMMED_PROPERTY;	
				
					
					
			List<string> propertyNames = new List<string>();
			int propertyCount = ShaderUtil.GetPropertyCount(shader);
			
			for(int i = 0; i < propertyCount; i++)
			{
				if(ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv &&
				   ShaderUtil.IsShaderPropertyHidden(shader, i) == false)
				{
					string propertyName = ShaderUtil.GetPropertyName(shader, i);
					propertyNames.Add(propertyName);
					if(propertyName == preferredTextureProperty)
						preferredTexturePropertyIndex = propertyNames.Count-1;
				}
			}
			
			string[] nicePropertyNames = propertyNames.ToArray ();
			for(int i = 0; i < nicePropertyNames.Length; i++)
			{
				Texture t = objectMaterials[currentSubmesh].GetTexture(propertyNames[i]);
				if(t != null)
					nicePropertyNames[i] += " (" + t.name + ")";
				else
					nicePropertyNames[i] += " (none)";
			}


			EditorGUI.BeginChangeCheck();
			preferredTexturePropertyIndex = EditorGUILayout.Popup ("Property", preferredTexturePropertyIndex, nicePropertyNames);
			if(EditorGUI.EndChangeCheck())
			{
				preferredTextureProperty = propertyNames[preferredTexturePropertyIndex];
			}
			
			
			Texture texture = objectMaterials[currentSubmesh].GetTexture(propertyNames[preferredTexturePropertyIndex]);
			
			if(texture is Texture2D)
				previewTexture = texture as Texture2D;
			else
				previewTexture = null;			
			
			
			return;
			
			
			DISPLAY_DIMMED_PROPERTY:
			
			{
				GUI.enabled = false;
				string[] empty = { "---" };
				EditorGUILayout.Popup ("Property", 0, empty);
				GUI.enabled = true;
				
				previewTexture = null;
			}
			
		}



        void OnGUI ()
        {
            if(uvPreviewMaterial == null || simpleMaterial == null)
            {
                if(!LoadMaterials())
                    return;                
            }
            
			for(int i = 0; i < inspectedObjects.Count; i++)
			{
				if(inspectedObjects[i].obj == null)
				{
					inspectedObjects.RemoveAt(i);
					i = 0;
				}
			}

			scroolPosition = EditorGUILayout.BeginScrollView(scroolPosition);

            EditorGUILayout.BeginVertical();

            Rect logoRect = EditorGUILayout.GetControlRect(GUILayout.Height(56));
            if(Event.current.type == EventType.Repaint)
                Styles.logoFont.Draw(logoRect, "nTools|UVInspector", false, false, false, false);
            


			// info box
			if(inspectedObjects.Count == 0)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField("Select object with mesh.");
				EditorGUILayout.EndVertical();
			}
			else if(inspectedObjects.Count == 1)
			{
				StringBuilder sb = new StringBuilder();
				InspectedObject obj = inspectedObjects[0];
				
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Object: " + obj.obj.name + ", Mesh: " + obj.mesh.name);				
				
				sb.AppendFormat("Submeshes: {0}", obj.subMeshCount);
				sb.AppendFormat(", Triangles: {0}", obj.triangleCount);
				sb.AppendFormat(", Vertices: {0}", obj.vertexCount);
				if(obj.hasColors) sb.Append(", Colors");
				if(obj.hasUV[0]) sb.Append(", UV1");
				if(obj.hasUV[1]) sb.Append(", UV2");
				if(obj.hasUV[2]) sb.Append(", UV3");
				if(obj.hasUV[3]) sb.Append(", UV4");
				
				EditorGUILayout.LabelField(sb.ToString());
				EditorGUILayout.EndVertical();
			}
			else
			{
				StringBuilder sb = new StringBuilder();
				
				for(int i = 0; i < inspectedObjects.Count; i++)
				{
					sb.Append(inspectedObjects[i].obj.name + "(" + inspectedObjects[i].mesh.name + "), ");
				}
				
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField("Multiple objects:");
				EditorGUILayout.LabelField(sb.ToString(), EditorStyles.wordWrappedMiniLabel);
				EditorGUILayout.EndVertical();
			}









            // UI Window
            float previewWindowHeight = Mathf.Min(EditorGUIUtility.currentViewWidth, this.position.height * 0.75f);
            Rect previewWindowRect = EditorGUILayout.GetControlRect(GUILayout.Height(previewWindowHeight));

            UVPreviewWindow(previewWindowRect);



            if (inspectedObjects.Count == 1 && !inspectedObjects[0].hasUV[(int)previewUVSet])
                EditorGUI.LabelField(previewWindowRect, "Unassigned UV Channel", Styles.hudFont);









            // Toolbar buttons
            EditorGUILayout.BeginHorizontal();

            showGrid = GUILayout.Toggle(showGrid, "Grid", EditorStyles.toolbarButton);
            showVertexColors = GUILayout.Toggle(showVertexColors, "Vertex Colors", EditorStyles.toolbarButton);
            tilePreviewTexture = GUILayout.Toggle(tilePreviewTexture, "Texture Tiles", EditorStyles.toolbarButton);
            GUI.enabled = IsCanFrameSelected();

            if (GUILayout.Toggle(false, "Frame View", EditorStyles.toolbarButton, GUILayout.MaxWidth(80)))
            {
                FrameSelected();
            }
            GUI.enabled = true;


            if (GUILayout.Toggle(false, "Reset View", EditorStyles.toolbarButton, GUILayout.MaxWidth(80)))
            {
                previewWindowPosition = new Vector2(-0.5f, -0.5f);

                float aspect = Mathf.Min(viewportSize.x, viewportSize.y) / Mathf.Max(viewportSize.x, viewportSize.y, 1);
                previewWindowScale = 1.8f * aspect;
            }

            EditorGUILayout.EndHorizontal();











            if(previewFoldout = nGUI.Foldout(previewFoldout, "Preview"))
			{
				++EditorGUI.indentLevel;

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel ("UV Channel");
				previewUVSet = (UVSet)GUILayout.Toolbar((int)previewUVSet, Styles.uvSetNames, GUILayout.MaxWidth(160), GUILayout.Height(20));
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel ("Sub Mesh");
				if (inspectedObjects.Count == 1)
				{
					SubMeshToolbar(inspectedObjects[0].mesh.subMeshCount);
				} else {
					GUI.enabled = false;
					GUI.Button(EditorGUILayout.GetControlRect(GUILayout.MaxWidth (160), GUILayout.Height(20)), inspectedObjects.Count > 1 ? "<Multyply Objects>" : "---");
					GUI.enabled = true;
				}
				EditorGUILayout.EndHorizontal();
				


				
				previewTextureSource = (PreviewTextureSource)EditorGUILayout.EnumPopup("Preview Texture", previewTextureSource);
				
				++EditorGUI.indentLevel;
				if(previewTextureSource == PreviewTextureSource.Custom)
				{					
					previewTextureTintColor = EditorGUILayout.ColorField("Image Tint", previewTextureTintColor);
					
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel ("Color Channel");
					previewTextureChannels = (ColorChannels)GUILayout.Toolbar((int)previewTextureChannels, Styles.colorChannelsNames, GUILayout.MaxWidth(160), GUILayout.Height(20));
					EditorGUILayout.EndHorizontal();
					
					customPreviewTexture = (Texture2D)EditorGUILayout.ObjectField("Image", customPreviewTexture, typeof(Texture2D), false);
				}
				else if(previewTextureSource == PreviewTextureSource.FromMaterial)
				{
					PreviewTextureGUI ();
					previewTextureTintColor = EditorGUILayout.ColorField("Image Tint", previewTextureTintColor);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel ("Color Channel");
					previewTextureChannels = (ColorChannels)GUILayout.Toolbar((int)previewTextureChannels, Styles.colorChannelsNames, GUILayout.MaxWidth(160), GUILayout.Height(20));
					EditorGUILayout.EndHorizontal();
				}
				--EditorGUI.indentLevel;
				
				--EditorGUI.indentLevel;
			}


           






            EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();
        }



        bool IsCanFrameSelected()
        {
            for(int i = 0; i < inspectedObjects.Count; i++)
            {
                if(inspectedObjects[i].hasUV[(int)previewUVSet])
                    return true;
            }
            return false;
        }


        void FrameSelected()
        {
            bool hasFirstPoint = true;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            for(int i = 0; i < inspectedObjects.Count; i++)
            {
                Vector2[] uvs = null;

                switch(previewUVSet)
                {
                case UVSet.UV1:
                    uvs = inspectedObjects[i].mesh.uv;
                    break;
                case UVSet.UV2:
                    uvs = inspectedObjects[i].mesh.uv2;
                    break;
                case UVSet.UV3:
                    uvs = inspectedObjects[i].mesh.uv3;
                    break;
                case UVSet.UV4:
                    uvs = inspectedObjects[i].mesh.uv4;
                    break;
                }

                if(uvs != null)
                {
                    for(int j = 0; j < uvs.Length; j++)
                    {
                        if(!hasFirstPoint)
                        {
                            bounds = new Bounds(uvs[j], Vector3.zero);
                            hasFirstPoint = true;
                        }
                        bounds.Encapsulate(uvs[j]);
                    }
                }
            }

            if(!hasFirstPoint) // no points
            {
                return;
            }

            previewWindowPosition = Vector3.zero - bounds.center;
            if(bounds.size.magnitude != 0.0f && viewportSize.x != 0 && viewportSize.y != 0)
            {
                float aspect = Mathf.Min(viewportSize.x, viewportSize.y) / Mathf.Max(viewportSize.x, viewportSize.y);
                previewWindowScale = aspect / bounds.extents.magnitude;
            }
        }

		static Rect AlignTextRect(Rect rect, TextAnchor anchor)
		{
			switch (anchor)
			{
			case TextAnchor.UpperCenter:
				rect.xMin -= rect.width  * 0.5f;
				break;
			case TextAnchor.UpperRight:
				rect.xMin -= rect.width;
				break;
			case TextAnchor.LowerLeft:
				rect.yMin -= rect.height * 0.5f;
				break;
			case TextAnchor.LowerCenter:
				rect.xMin -= rect.width  * 0.5f;
				rect.yMin -= rect.height;
				break;
			case TextAnchor.LowerRight:
				rect.xMin -= rect.width;
				rect.yMin -= rect.height;
				break;
			case TextAnchor.MiddleLeft:
				rect.yMin -= rect.height * 0.5f;
				break;
			case TextAnchor.MiddleCenter:
				rect.xMin -= rect.width  * 0.5f;
				rect.yMin -= rect.height * 0.5f;
				break;
			case TextAnchor.MiddleRight:
				rect.xMin -= rect.width;
				rect.yMin -= rect.height * 0.5f;
				break;
			}
			
			return rect;
		}


        public void DrawLabel(Vector3 worldPoint, Rect viewport, Matrix4x4 MVPMatrix, string text, GUIStyle style, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            Vector2 guiPoint = MVPMatrix.MultiplyPoint(worldPoint);

            guiPoint = new Vector2(guiPoint.x * 0.5f + 0.5f, 0.5f - guiPoint.y * 0.5f);
			guiPoint = new Vector2(guiPoint.x * viewport.width, guiPoint.y * viewport.height);

            s_TempContent.text = text;
            Vector2 size = style.CalcSize(s_TempContent);

            Rect labelRect = new Rect(guiPoint.x, guiPoint.y, size.x, size.y);

            labelRect = AlignTextRect(labelRect, alignment);

            labelRect = style.padding.Add(labelRect);


            GUI.Label(labelRect, s_TempContent, style);
            
        }

		static void SetMaterialKeyword(Material material, string keyword, bool state)
        {
            if (state)
                material.EnableKeyword (keyword);
            else
                material.DisableKeyword (keyword);
        }



		static Color32 IntToColor(int c)
        {
            return new Color32((byte)(c & 0xff), (byte)((c>>8) & 0xff), (byte)((c>>16) & 0xff), (byte)((c>>24) & 0xff));
        }

		static int ColorToInt(Color32 c)
        {
            return c.r | (c.g << 8) | (c.b << 16) | (c.a << 24);
        }

		static void ForAllInHierarchy(GameObject gameObject, Action<GameObject> action)
		{
			action(gameObject);
			
			for (int i = 0; i < gameObject.transform.childCount; i++)
				ForAllInHierarchy(gameObject.transform.GetChild(i).gameObject, action);
		}
    }
}
