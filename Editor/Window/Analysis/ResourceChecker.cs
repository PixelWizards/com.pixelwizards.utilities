// Resource Checker
// (c) 2020 Gekido https://www.gitub.com/gekidoslair
// (c) 2012 Simon Oliver / HandCircus / hello@handcircus.com
// (c) 2015 Brice Clocher / Mangatome / hello@mangatome.net
// Public domain, do with whatever you like, commercial or not
// This comes with no warranty, use at your own risk!
// https://github.com/handcircus/Unity-Resource-Checker

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

namespace PixelWizards.Utilities
{
	public class TextureDetails : IEquatable<TextureDetails>
	{
		public bool isCubeMap;
		public int memSizeKB;
		public Texture texture;
		public TextureFormat format;
		public int mipMapCount;
		public List<Object> FoundInMaterials = new List<Object>();
		public List<Object> FoundInRenderers = new List<Object>();
		public List<Object> FoundInAnimators = new List<Object>();
		public List<Object> FoundInScripts = new List<Object>();
		public List<Object> FoundInGraphics = new List<Object>();
		public bool isSky;
		public bool instance;
		public bool isgui;
		public TextureDetails()
		{

		}

		public bool Equals(TextureDetails other)
		{
			return texture != null && other.texture != null &&
				texture.GetNativeTexturePtr() == other.texture.GetNativeTexturePtr();
		}

		public override int GetHashCode()
		{
			return (int)texture.GetNativeTexturePtr();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TextureDetails);
		}
	};

	public class MaterialDetails
	{

		public Material material;

		public List<Renderer> FoundInRenderers = new List<Renderer>();
		public List<Graphic> FoundInGraphics = new List<Graphic>();
		public bool instance;
		public bool isgui;
		public bool isSky;

		public MaterialDetails()
		{
			instance = false;
			isgui = false;
			isSky = false;
		}
	};

	public class MeshDetails
	{

		public Mesh mesh;

		public List<MeshFilter> FoundInMeshFilters = new List<MeshFilter>();
		public List<SkinnedMeshRenderer> FoundInSkinnedMeshRenderer = new List<SkinnedMeshRenderer>();
		public bool instance;

		public MeshDetails()
		{
			instance = false;
		}
	};

	public class MissingGraphic
	{
		public Transform Object;
		public string type;
		public string name;
	}

	public class ResourceChecker : EditorWindow
	{
		string[] inspectToolbarStrings = { "Textures", "Materials", "Meshes" };
		string[] inspectToolbarStrings2 = { "Textures", "Materials", "Meshes", "Missing" };

		enum InspectType
		{
			Textures, Materials, Meshes, Missing
		};

		bool IncludeDisabledObjects = true;
		bool IncludeSpriteAnimations = true;
		bool IncludeScriptReferences = true;
		bool IncludeGuiElements = true;
		bool thingsMissing = false;

		InspectType ActiveInspectType = InspectType.Textures;

		float ThumbnailWidth = 40;
		float ThumbnailHeight = 40;

		List<TextureDetails> ActiveTextures = new List<TextureDetails>();
		List<MaterialDetails> ActiveMaterials = new List<MaterialDetails>();
		List<MeshDetails> ActiveMeshDetails = new List<MeshDetails>();
		List<MissingGraphic> MissingObjects = new List<MissingGraphic>();

		Vector2 textureListScrollPos = new Vector2(0, 0);
		Vector2 materialListScrollPos = new Vector2(0, 0);
		Vector2 meshListScrollPos = new Vector2(0, 0);
		Vector2 missingListScrollPos = new Vector2(0, 0);

		int TotalTextureMemory = 0;
		int TotalMeshVertices = 0;

		bool ctrlPressed = false;

		static int MinWidth = 475;
		Color defColor;

		bool collectedInPlayingMode;

		[MenuItem("Window/Analysis/Resource Checker")]
		private static void Init()
		{
			var window = (ResourceChecker)GetWindow(typeof(ResourceChecker));
			window.CheckResources();
			window.minSize = new Vector2(MinWidth, 475);
			window.titleContent = new GUIContent("Resource Checker");
		}

		private void OnGUI()
		{
			defColor = GUI.color;
			IncludeDisabledObjects = GUILayout.Toggle(IncludeDisabledObjects, "Include disabled objects", GUILayout.Width(300));
			IncludeSpriteAnimations = GUILayout.Toggle(IncludeSpriteAnimations, "Look in sprite animations", GUILayout.Width(300));
			GUI.color = new Color(0.8f, 0.8f, 1.0f, 1.0f);
			IncludeScriptReferences = GUILayout.Toggle(IncludeScriptReferences, "Look in behavior fields", GUILayout.Width(300));
			GUI.color = new Color(1.0f, 0.95f, 0.8f, 1.0f);
			IncludeGuiElements = GUILayout.Toggle(IncludeGuiElements, "Look in GUI elements", GUILayout.Width(300));
			GUI.color = defColor;
			GUILayout.BeginArea(new Rect(position.width - 85, 5, 100, 65));
			if (GUILayout.Button("Calculate", GUILayout.Width(80), GUILayout.Height(40)))
			{
				CheckResources();
			}

			if (GUILayout.Button("CleanUp", GUILayout.Width(80), GUILayout.Height(20)))
			{
				Resources.UnloadUnusedAssets();
			}
			GUILayout.EndArea();
			RemoveDestroyedResources();

			GUILayout.Space(30);
			if (thingsMissing == true)
			{
				EditorGUI.HelpBox(new Rect(8, 75, 300, 25), "Some GameObjects are missing graphical elements.", MessageType.Error);
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label("Textures " + ActiveTextures.Count + " - " + FormatSizeString(TotalTextureMemory));
			GUILayout.Label("Materials " + ActiveMaterials.Count);
			GUILayout.Label("Meshes " + ActiveMeshDetails.Count + " - " + TotalMeshVertices + " verts");
			GUILayout.EndHorizontal();
			if (thingsMissing == true)
			{
				ActiveInspectType = (InspectType)GUILayout.Toolbar((int)ActiveInspectType, inspectToolbarStrings2);
			}
			else
			{
				ActiveInspectType = (InspectType)GUILayout.Toolbar((int)ActiveInspectType, inspectToolbarStrings);
			}

			ctrlPressed = Event.current.control || Event.current.command;

			switch (ActiveInspectType)
			{
				case InspectType.Textures:
					ListTextures();
					break;
				case InspectType.Materials:
					ListMaterials();
					break;
				case InspectType.Meshes:
					ListMeshes();
					break;
				case InspectType.Missing:
					ListMissing();
					break;
			}
		}

		private void RemoveDestroyedResources()
		{
			if (collectedInPlayingMode != Application.isPlaying)
			{
				ActiveTextures.Clear();
				ActiveMaterials.Clear();
				ActiveMeshDetails.Clear();
				MissingObjects.Clear();
				thingsMissing = false;
				collectedInPlayingMode = Application.isPlaying;
			}

			ActiveTextures.RemoveAll(x => !x.texture);
			ActiveTextures.ForEach(delegate (TextureDetails obj)
			{
				obj.FoundInAnimators.RemoveAll(x => !x);
				obj.FoundInMaterials.RemoveAll(x => !x);
				obj.FoundInRenderers.RemoveAll(x => !x);
				obj.FoundInScripts.RemoveAll(x => !x);
				obj.FoundInGraphics.RemoveAll(x => !x);
			});

			ActiveMaterials.RemoveAll(x => !x.material);
			ActiveMaterials.ForEach(delegate (MaterialDetails obj)
			{
				obj.FoundInRenderers.RemoveAll(x => !x);
				obj.FoundInGraphics.RemoveAll(x => !x);
			});

			ActiveMeshDetails.RemoveAll(x => !x.mesh);
			ActiveMeshDetails.ForEach(delegate (MeshDetails obj)
			{
				obj.FoundInMeshFilters.RemoveAll(x => !x);
				obj.FoundInSkinnedMeshRenderer.RemoveAll(x => !x);
			});

			TotalTextureMemory = 0;
			foreach (TextureDetails tTextureDetails in ActiveTextures) TotalTextureMemory += tTextureDetails.memSizeKB;

			TotalMeshVertices = 0;
			foreach (MeshDetails tMeshDetails in ActiveMeshDetails) TotalMeshVertices += tMeshDetails.mesh.vertexCount;
		}

		private int GetBitsPerPixel(TextureFormat format)
		{
			switch (format)
			{
				case TextureFormat.Alpha8: //	 Alpha-only texture format.
					return 8;
				case TextureFormat.ARGB4444: //	 A 16 bits/pixel texture format. Texture stores color with an alpha channel.
					return 16;
				case TextureFormat.RGBA4444: //	 A 16 bits/pixel texture format.
					return 16;
				case TextureFormat.RGB24:   // A color texture format.
					return 24;
				case TextureFormat.RGBA32:  //Color with an alpha channel texture format.
					return 32;
				case TextureFormat.ARGB32:  //Color with an alpha channel texture format.
					return 32;
				case TextureFormat.RGB565:  //	 A 16 bit color texture format.
					return 16;
				case TextureFormat.DXT1:    // Compressed color texture format.
					return 4;
				case TextureFormat.DXT5:    // Compressed color with alpha channel texture format.
					return 8;
				// deprecated
				// case TextureFormat.PVRTC_RGB2://	 PowerVR (iOS) 2 bits/pixel compressed color texture format.
				// 	return 2;
				// case TextureFormat.PVRTC_RGBA2://	 PowerVR (iOS) 2 bits/pixel compressed with alpha channel texture format
				// 	return 2;
				// case TextureFormat.PVRTC_RGB4://	 PowerVR (iOS) 4 bits/pixel compressed color texture format.
				// 	return 4;
				// case TextureFormat.PVRTC_RGBA4://	 PowerVR (iOS) 4 bits/pixel compressed with alpha channel texture format
				// 	return 4;
				case TextureFormat.ETC_RGB4://	 ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
					return 4;
				case TextureFormat.ETC2_RGBA8://	 ATC (ATITC) 8 bits/pixel compressed RGB texture format.
					return 8;
				case TextureFormat.BGRA32://	 Format returned by iPhone camera
					return 32;
			}
			return 0;
		}

		private int CalculateTextureSizeBytes(Texture tTexture)
		{
			var tWidth = tTexture.width;
			var tHeight = tTexture.height;
			if (tTexture is Texture2D)
			{
				Texture2D tTex2D = tTexture as Texture2D;
				int bitsPerPixel = GetBitsPerPixel(tTex2D.format);
				int mipMapCount = tTex2D.mipmapCount;
				int mipLevel = 1;
				int tSize = 0;
				while (mipLevel <= mipMapCount)
				{
					tSize += tWidth * tHeight * bitsPerPixel / 8;
					tWidth = tWidth / 2;
					tHeight = tHeight / 2;
					mipLevel++;
				}
				return tSize;
			}
			if (tTexture is Texture2DArray)
			{
				var tTex2D = tTexture as Texture2DArray;
				var bitsPerPixel = GetBitsPerPixel(tTex2D.format);
				var mipMapCount = 10;
				var mipLevel = 1;
				var tSize = 0;
				while (mipLevel <= mipMapCount)
				{
					tSize += tWidth * tHeight * bitsPerPixel / 8;
					tWidth = tWidth / 2;
					tHeight = tHeight / 2;
					mipLevel++;
				}
				return tSize * ((Texture2DArray)tTex2D).depth;
			}
			if (tTexture is Cubemap)
			{
				Cubemap tCubemap = tTexture as Cubemap;
				int bitsPerPixel = GetBitsPerPixel(tCubemap.format);
				return tWidth * tHeight * 6 * bitsPerPixel / 8;
			}
			return 0;
		}


		private void SelectObject(Object selectedObject, bool append)
		{
			if (append)
			{
				var currentSelection = new List<Object>(Selection.objects);
				// Allow toggle selection
				if (currentSelection.Contains(selectedObject)) currentSelection.Remove(selectedObject);
				else currentSelection.Add(selectedObject);

				Selection.objects = currentSelection.ToArray();
			}
			else Selection.activeObject = selectedObject;
		}

		private void SelectObjects(List<Object> selectedObjects, bool append)
		{
			if (append)
			{
				var currentSelection = new List<Object>(Selection.objects);
				currentSelection.AddRange(selectedObjects);
				Selection.objects = currentSelection.ToArray();
			}
			else Selection.objects = selectedObjects.ToArray();
		}

		private void ListTextures()
		{
			textureListScrollPos = EditorGUILayout.BeginScrollView(textureListScrollPos);

			foreach (var tDetails in ActiveTextures)
			{
				GUILayout.BeginHorizontal();
				var tex = tDetails.texture;
				if (tDetails.texture is Texture2DArray || tDetails.texture is Cubemap)
				{
					tex = AssetPreview.GetMiniThumbnail(tDetails.texture);
				}
				GUILayout.Box(tex, GUILayout.Width(ThumbnailWidth), GUILayout.Height(ThumbnailHeight));

				if (tDetails.instance == true)
					GUI.color = new Color(0.8f, 0.8f, defColor.b, 1.0f);
				if (tDetails.isgui == true)
					GUI.color = new Color(defColor.r, 0.95f, 0.8f, 1.0f);
				if (tDetails.isSky)
					GUI.color = new Color(0.9f, defColor.g, defColor.b, 1.0f);
				if (GUILayout.Button(tDetails.texture.name, GUILayout.Width(150)))
				{
					SelectObject(tDetails.texture, ctrlPressed);
				}
				GUI.color = defColor;

				var sizeLabel = "" + tDetails.texture.width + "x" + tDetails.texture.height;
				if (tDetails.isCubeMap) sizeLabel += "x6";
				if (tDetails.texture.GetType() == typeof(Texture2DArray))
				{
					sizeLabel += "[]\n" + ((Texture2DArray)tDetails.texture).depth + "depths";
				}
				sizeLabel += " - " + tDetails.mipMapCount + "mip\n" + FormatSizeString(tDetails.memSizeKB) + " - " + tDetails.format;

				GUILayout.Label(sizeLabel, GUILayout.Width(120));

				if (GUILayout.Button(tDetails.FoundInMaterials.Count + " Mat", GUILayout.Width(50)))
				{
					SelectObjects(tDetails.FoundInMaterials, ctrlPressed);
				}

				var FoundObjects = new HashSet<Object>();
				foreach (Renderer renderer in tDetails.FoundInRenderers) FoundObjects.Add(renderer.gameObject);
				foreach (Animator animator in tDetails.FoundInAnimators) FoundObjects.Add(animator.gameObject);
				foreach (Graphic graphic in tDetails.FoundInGraphics) FoundObjects.Add(graphic.gameObject);
				foreach (MonoBehaviour script in tDetails.FoundInScripts) FoundObjects.Add(script.gameObject);
				if (GUILayout.Button(FoundObjects.Count + " GO", GUILayout.Width(50)))
				{
					SelectObjects(new List<Object>(FoundObjects), ctrlPressed);
				}

				GUILayout.EndHorizontal();
			}
			if (ActiveTextures.Count > 0)
			{
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				//GUILayout.Box(" ",GUILayout.Width(ThumbnailWidth),GUILayout.Height(ThumbnailHeight));
				if (GUILayout.Button("Select \n All", GUILayout.Width(ThumbnailWidth * 2)))
				{
					var AllTextures = new List<Object>();
					foreach (var tDetails in ActiveTextures)
					{
						AllTextures.Add(tDetails.texture);
					}
					SelectObjects(AllTextures, ctrlPressed);
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
		}

		void ListMaterials()
		{
			materialListScrollPos = EditorGUILayout.BeginScrollView(materialListScrollPos);

			foreach (var tDetails in ActiveMaterials)
			{
				if (tDetails.material == null) continue;
				
				GUILayout.BeginHorizontal();

				GUILayout.Box(AssetPreview.GetAssetPreview(tDetails.material), GUILayout.Width(ThumbnailWidth), GUILayout.Height(ThumbnailHeight));

				if (tDetails.instance == true)
					GUI.color = new Color(0.8f, 0.8f, defColor.b, 1.0f);
				if (tDetails.isgui == true)
					GUI.color = new Color(defColor.r, 0.95f, 0.8f, 1.0f);
				if (tDetails.isSky)
					GUI.color = new Color(0.9f, defColor.g, defColor.b, 1.0f);
					
				if (GUILayout.Button(tDetails.material.name, GUILayout.Width(150)))
				{
					SelectObject(tDetails.material, ctrlPressed);
				}
				GUI.color = defColor;

				var shaderLabel = tDetails.material.shader != null ? tDetails.material.shader.name : "no shader";
				GUILayout.Label(shaderLabel, GUILayout.Width(200));

				if (GUILayout.Button((tDetails.FoundInRenderers.Count + tDetails.FoundInGraphics.Count) + " GO", GUILayout.Width(50)))
				{
					var FoundObjects = new List<Object>();
					foreach (var renderer in tDetails.FoundInRenderers)
					{
						FoundObjects.Add(renderer.gameObject);
					}

					foreach (var graphic in tDetails.FoundInGraphics)
					{
						FoundObjects.Add(graphic.gameObject);
					}
					SelectObjects(FoundObjects, ctrlPressed);
				}

				GUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
		}

		void ListMeshes()
		{
			meshListScrollPos = EditorGUILayout.BeginScrollView(meshListScrollPos);

			foreach (var tDetails in ActiveMeshDetails)
			{
				if (tDetails.mesh == null) continue;
				
				GUILayout.BeginHorizontal();
				string name = tDetails.mesh.name;
				if (!name.Any())
				{
					name = tDetails.FoundInMeshFilters[0].gameObject.name;
				}
				if (tDetails.instance == true)
				{
					GUI.color = new Color(0.8f, 0.8f, defColor.b, 1.0f);
				}
					
				if (GUILayout.Button(name, GUILayout.Width(150)))
				{
					SelectObject(tDetails.mesh, ctrlPressed);
				}
					
				GUI.color = defColor;
				string sizeLabel = "" + tDetails.mesh.vertexCount + " vert";

				GUILayout.Label(sizeLabel, GUILayout.Width(100));

				if (GUILayout.Button(tDetails.FoundInMeshFilters.Count + " GO", GUILayout.Width(50)))
				{
					var FoundObjects = new List<Object>();
					foreach (var meshFilter in tDetails.FoundInMeshFilters)
					{
						FoundObjects.Add(meshFilter.gameObject);
					}
					SelectObjects(FoundObjects, ctrlPressed);
				}
				if (tDetails.FoundInSkinnedMeshRenderer.Count > 0)
				{
					if (GUILayout.Button(tDetails.FoundInSkinnedMeshRenderer.Count + " skinned mesh GO", GUILayout.Width(140)))
					{
						var FoundObjects = new List<Object>();
						foreach (var skinnedMeshRenderer in tDetails.FoundInSkinnedMeshRenderer)
						{
							FoundObjects.Add(skinnedMeshRenderer.gameObject);
						}
						SelectObjects(FoundObjects, ctrlPressed);
					}
				}
				else
				{
					GUI.color = new Color(defColor.r, defColor.g, defColor.b, 0.5f);
					GUILayout.Label("   0 skinned mesh");
					GUI.color = defColor;
				}

				GUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
		}

		private void ListMissing()
		{
			missingListScrollPos = EditorGUILayout.BeginScrollView(missingListScrollPos);
			foreach (var dMissing in MissingObjects)
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button(dMissing.name, GUILayout.Width(150)))
				{
					SelectObject(dMissing.Object, ctrlPressed);
				}
				GUILayout.Label("missing ", GUILayout.Width(48));
				switch (dMissing.type)
				{
					case "mesh":
						GUI.color = new Color(0.8f, 0.8f, defColor.b, 1.0f);
						break;
					case "sprite":
						GUI.color = new Color(defColor.r, 0.8f, 0.8f, 1.0f);
						break;
					case "material":
						GUI.color = new Color(0.8f, defColor.g, 0.8f, 1.0f);
						break;
				}
				GUILayout.Label(dMissing.type);
				GUI.color = defColor;
				GUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
		}

		string FormatSizeString(int memSizeKB)
		{
			if (memSizeKB < 1024)
			{
				return "" + memSizeKB + "k";
			}
			
			var memSizeMB = ((float)memSizeKB) / 1024.0f;
			return memSizeMB.ToString("0.00") + "Mb";
		}


		TextureDetails FindTextureDetails(Texture tTexture)
		{
			return ActiveTextures.FirstOrDefault(tTextureDetails => tTextureDetails.texture == tTexture);
		}

		MaterialDetails FindMaterialDetails(Material tMaterial)
		{
			return ActiveMaterials.FirstOrDefault(tMaterialDetails => tMaterialDetails.material == tMaterial);
		}

		MeshDetails FindMeshDetails(Mesh tMesh)
		{
			return ActiveMeshDetails.FirstOrDefault(tMeshDetails => tMeshDetails.mesh == tMesh);
		}


		private void CheckResources()
		{
			ActiveTextures.Clear();
			ActiveMaterials.Clear();
			ActiveMeshDetails.Clear();
			MissingObjects.Clear();
			thingsMissing = false;

			var renderers = FindObjects<Renderer>();

			var skyMat = new MaterialDetails
			{
				material = RenderSettings.skybox,
				isSky = true
			};
			ActiveMaterials.Add(skyMat);

			//Debug.Log("Total renderers "+renderers.Length);
			foreach (var renderer in renderers)
			{
				//Debug.Log("Renderer is "+renderer.name);
				foreach (var material in renderer.sharedMaterials)
				{

					var tMaterialDetails = FindMaterialDetails(material);
					if (tMaterialDetails == null)
					{
						tMaterialDetails = new MaterialDetails
						{
							material = material
						};
						ActiveMaterials.Add(tMaterialDetails);
					}
					tMaterialDetails.FoundInRenderers.Add(renderer);
				}

				if (renderer is not SpriteRenderer) continue;
				
				var tSpriteRenderer = (SpriteRenderer)renderer;

				if (tSpriteRenderer.sprite != null)
				{
					var tSpriteTextureDetail = GetTextureDetail(tSpriteRenderer.sprite.texture, renderer);
					if (!ActiveTextures.Contains(tSpriteTextureDetail))
					{
						ActiveTextures.Add(tSpriteTextureDetail);
					}
				}
				else if (tSpriteRenderer.sprite == null)
				{
					var tMissing = new MissingGraphic
					{
						Object = tSpriteRenderer.transform,
						type = "sprite",
						name = tSpriteRenderer.transform.name
					};
					MissingObjects.Add(tMissing);
					thingsMissing = true;
				}
			}

			if (IncludeGuiElements)
			{
				var graphics = FindObjects<Graphic>();

				foreach (var graphic in graphics)
				{
					if (graphic.mainTexture)
					{
						var tSpriteTextureDetail = GetTextureDetail(graphic.mainTexture, graphic);
						if (!ActiveTextures.Contains(tSpriteTextureDetail))
						{
							ActiveTextures.Add(tSpriteTextureDetail);
						}
					}

					if (graphic.materialForRendering)
					{
						var tMaterialDetails = FindMaterialDetails(graphic.materialForRendering);
						if (tMaterialDetails == null)
						{
							tMaterialDetails = new MaterialDetails
							{
								material = graphic.materialForRendering,
								isgui = true
							};
							ActiveMaterials.Add(tMaterialDetails);
						}
						tMaterialDetails.FoundInGraphics.Add(graphic);
					}
				}
			}

			foreach (MaterialDetails tMaterialDetails in ActiveMaterials)
			{
				var tMaterial = tMaterialDetails.material;
				if (tMaterial == null) continue;
				
				var dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { tMaterial });
				foreach (var obj in dependencies)
				{
					if (obj is not Texture) continue;
					
					var tTexture = obj as Texture;
					var tTextureDetail = GetTextureDetail(tTexture, tMaterial, tMaterialDetails);
					tTextureDetail.isSky = tMaterialDetails.isSky;
					tTextureDetail.instance = tMaterialDetails.instance;
					tTextureDetail.isgui = tMaterialDetails.isgui;
					ActiveTextures.Add(tTextureDetail);
				}

				//if the texture was downloaded, it won't be included in the editor dependencies
				if (tMaterial.HasProperty("_MainTex"))
				{
					if (tMaterial.mainTexture == null || dependencies.Contains(tMaterial.mainTexture)) continue;
						
					var tTextureDetail = GetTextureDetail(tMaterial.mainTexture, tMaterial, tMaterialDetails);
					ActiveTextures.Add(tTextureDetail);
				}
			}


			var meshFilters = FindObjects<MeshFilter>();

			foreach (var tMeshFilter in meshFilters)
			{
				var tMesh = tMeshFilter.sharedMesh;
				if (tMesh != null)
				{
					var tMeshDetails = FindMeshDetails(tMesh);
					if (tMeshDetails == null)
					{
						tMeshDetails = new MeshDetails
						{
							mesh = tMesh
						};
						ActiveMeshDetails.Add(tMeshDetails);
					}
					tMeshDetails.FoundInMeshFilters.Add(tMeshFilter);
				}
				else if (tMesh == null && tMeshFilter.transform.GetComponent("TextContainer") == null)
				{
					var tMissing = new MissingGraphic
					{
						Object = tMeshFilter.transform,
						type = "mesh",
						name = tMeshFilter.transform.name
					};
					MissingObjects.Add(tMissing);
					thingsMissing = true;
				}

				var meshRenderer = tMeshFilter.transform.GetComponent<MeshRenderer>();

				if (meshRenderer == null || meshRenderer.sharedMaterial == null)
				{
					var tMissing = new MissingGraphic
					{
						Object = tMeshFilter.transform,
						type = "material",
						name = tMeshFilter.transform.name
					};
					MissingObjects.Add(tMissing);
					thingsMissing = true;
				}
			}

			var skinnedMeshRenderers = FindObjects<SkinnedMeshRenderer>();

			foreach (var tSkinnedMeshRenderer in skinnedMeshRenderers)
			{
				var tMesh = tSkinnedMeshRenderer.sharedMesh;
				if (tMesh != null)
				{
					var tMeshDetails = FindMeshDetails(tMesh);
					if (tMeshDetails == null)
					{
						tMeshDetails = new MeshDetails
						{
							mesh = tMesh
						};
						ActiveMeshDetails.Add(tMeshDetails);
					}
					tMeshDetails.FoundInSkinnedMeshRenderer.Add(tSkinnedMeshRenderer);
				}
				else if (tMesh == null)
				{
					var tMissing = new MissingGraphic
					{
						Object = tSkinnedMeshRenderer.transform,
						type = "mesh",
						name = tSkinnedMeshRenderer.transform.name
					};
					MissingObjects.Add(tMissing);
					thingsMissing = true;
				}
				if (tSkinnedMeshRenderer.sharedMaterial == null)
				{
					var tMissing = new MissingGraphic
					{
						Object = tSkinnedMeshRenderer.transform,
						type = "material",
						name = tSkinnedMeshRenderer.transform.name
					};
					MissingObjects.Add(tMissing);
					thingsMissing = true;
				}
			}

			if (IncludeSpriteAnimations)
			{
				var animators = FindObjects<Animator>();
				foreach (var anim in animators)
				{
					var ac = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

					//Skip animators without layers, this can happen if they don't have an animator controller.
					if (!ac || ac.layers == null || ac.layers.Length == 0)
						continue;

					for (int x = 0; x < anim.layerCount; x++)
					{
						var sm = ac.layers[x].stateMachine;
						var cnt = sm.states.Length;

						for (int i = 0; i < cnt; i++)
						{
							var state = sm.states[i].state;
							var m = state.motion;
							if (m == null) continue;
							var clip = m as AnimationClip;

							if (clip == null) continue;
							
							var ecbs = AnimationUtility.GetObjectReferenceCurveBindings(clip);

							foreach (var ecb in ecbs)
							{
								if (ecb.propertyName != "m_Sprite") continue;
								
								foreach (var keyframe in AnimationUtility.GetObjectReferenceCurve(clip, ecb))
								{
									var tSprite = keyframe.value as Sprite;

									if (tSprite == null) continue;
												
									var tTextureDetail = GetTextureDetail(tSprite.texture, anim);
									if (!ActiveTextures.Contains(tTextureDetail))
									{
										ActiveTextures.Add(tTextureDetail);
									}
								}
							}
						}
					}

				}
			}

			if (IncludeScriptReferences)
			{
				var scripts = FindObjects<MonoBehaviour>();
				foreach (var script in scripts)
				{
					var flags = BindingFlags.Public | BindingFlags.Instance; // only public non-static fields are bound to by Unity.
					var fields = script.GetType().GetFields(flags);

					foreach (var field in fields)
					{
						Type fieldType = field.FieldType;
						if (fieldType == typeof(Sprite))
						{
							Sprite tSprite = field.GetValue(script) as Sprite;
							if (tSprite != null)
							{
								var tSpriteTextureDetail = GetTextureDetail(tSprite.texture, script);
								if (!ActiveTextures.Contains(tSpriteTextureDetail))
								{
									ActiveTextures.Add(tSpriteTextureDetail);
								}
							}
						}
						if (fieldType == typeof(Mesh))
						{
							Mesh tMesh = field.GetValue(script) as Mesh;
							if (tMesh != null)
							{
								MeshDetails tMeshDetails = FindMeshDetails(tMesh);
								if (tMeshDetails == null)
								{
									tMeshDetails = new MeshDetails();
									tMeshDetails.mesh = tMesh;
									tMeshDetails.instance = true;
									ActiveMeshDetails.Add(tMeshDetails);
								}
							}
						}
						if (fieldType == typeof(Material))
						{
							var tMaterial = field.GetValue(script) as Material;
							if (tMaterial == null) continue;
							
							var tMatDetails = FindMaterialDetails(tMaterial);
							if (tMatDetails == null)
							{
								tMatDetails = new MaterialDetails
								{
									instance = true,
									material = tMaterial
								};
								if (!ActiveMaterials.Contains(tMatDetails))
									ActiveMaterials.Add(tMatDetails);
							}
							if (tMaterial.mainTexture)
							{
								var tSpriteTextureDetail = GetTextureDetail(tMaterial.mainTexture);
								if (!ActiveTextures.Contains(tSpriteTextureDetail))
								{
									ActiveTextures.Add(tSpriteTextureDetail);
								}
							}
							var dependencies = EditorUtility.CollectDependencies(new Object[] { tMaterial });
							foreach (var obj in dependencies)
							{
								if (obj is not Texture) continue;
								
								Texture tTexture = obj as Texture;
								var tTextureDetail = GetTextureDetail(tTexture, tMaterial, tMatDetails);
								if (!ActiveTextures.Contains(tTextureDetail))
									ActiveTextures.Add(tTextureDetail);
							}
						}
					}
				}
			}

			TotalTextureMemory = 0;
			foreach (var tTextureDetails in ActiveTextures) TotalTextureMemory += tTextureDetails.memSizeKB;

			TotalMeshVertices = 0;
			foreach (var tMeshDetails in ActiveMeshDetails) TotalMeshVertices += tMeshDetails.mesh.vertexCount;

			// Sort by size, descending
			ActiveTextures.Sort((details1, details2) => details2.memSizeKB - details1.memSizeKB);
			ActiveTextures = ActiveTextures.Distinct().ToList();
			ActiveMeshDetails.Sort((details1, details2) => details2.mesh.vertexCount - details1.mesh.vertexCount);

			collectedInPlayingMode = Application.isPlaying;
		}

		private static GameObject[] GetAllRootGameObjects()
		{
			var allGo = new List<GameObject>();
			for (var sceneIdx = 0; sceneIdx < UnityEngine.SceneManagement.SceneManager.sceneCount; ++sceneIdx)
			{
				allGo.AddRange(UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIdx).GetRootGameObjects().ToArray());
			}
			return allGo.ToArray();
		}

		private T[] FindObjects<T>() where T : Object
		{
			if (IncludeDisabledObjects)
			{
				var meshfilters = new List<T>();
				var allGo = GetAllRootGameObjects();
				foreach (var go in allGo)
				{
					var tgo = go.GetComponentsInChildren<Transform>(true).ToArray();
					meshfilters.AddRange(from tr in tgo where tr.GetComponent<T>() select tr.GetComponent<T>());
				}
				return (T[])meshfilters.ToArray();
			}
			else
				return (T[])FindObjectsByType(typeof(T), FindObjectsSortMode.None);
		}

		private TextureDetails GetTextureDetail(Texture tTexture, Material tMaterial, MaterialDetails tMaterialDetails)
		{
			TextureDetails tTextureDetails = GetTextureDetail(tTexture);

			tTextureDetails.FoundInMaterials.Add(tMaterial);
			foreach (var renderer in tMaterialDetails.FoundInRenderers.Where(renderer => !tTextureDetails.FoundInRenderers.Contains(renderer)))
			{
				tTextureDetails.FoundInRenderers.Add(renderer);
			}
			return tTextureDetails;
		}

		private TextureDetails GetTextureDetail(Texture tTexture, Renderer renderer)
		{
			var tTextureDetails = GetTextureDetail(tTexture);

			tTextureDetails.FoundInRenderers.Add(renderer);
			return tTextureDetails;
		}

		private TextureDetails GetTextureDetail(Texture tTexture, Animator animator)
		{
			var tTextureDetails = GetTextureDetail(tTexture);

			tTextureDetails.FoundInAnimators.Add(animator);
			return tTextureDetails;
		}

		private TextureDetails GetTextureDetail(Texture tTexture, Graphic graphic)
		{
			TextureDetails tTextureDetails = GetTextureDetail(tTexture);

			tTextureDetails.FoundInGraphics.Add(graphic);
			return tTextureDetails;
		}

		private TextureDetails GetTextureDetail(Texture tTexture, MonoBehaviour script)
		{
			TextureDetails tTextureDetails = GetTextureDetail(tTexture);

			tTextureDetails.FoundInScripts.Add(script);
			return tTextureDetails;
		}

		private TextureDetails GetTextureDetail(Texture tTexture)
		{
			TextureDetails tTextureDetails = FindTextureDetails(tTexture);
			if (tTextureDetails == null)
			{
				tTextureDetails = new TextureDetails();
				tTextureDetails.texture = tTexture;
				tTextureDetails.isCubeMap = tTexture is Cubemap;

				int memSize = CalculateTextureSizeBytes(tTexture);

				TextureFormat tFormat = TextureFormat.RGBA32;
				int tMipMapCount = 1;
				if (tTexture is Texture2D)
				{
					tFormat = (tTexture as Texture2D).format;
					tMipMapCount = (tTexture as Texture2D).mipmapCount;
				}
				if (tTexture is Cubemap)
				{
					tFormat = (tTexture as Cubemap).format;
					memSize = 8 * tTexture.height * tTexture.width;
				}
				if (tTexture is Texture2DArray)
				{
					tFormat = (tTexture as Texture2DArray).format;
					tMipMapCount = 10;
				}

				tTextureDetails.memSizeKB = memSize / 1024;
				tTextureDetails.format = tFormat;
				tTextureDetails.mipMapCount = tMipMapCount;

			}

			return tTextureDetails;
		}

	}
}