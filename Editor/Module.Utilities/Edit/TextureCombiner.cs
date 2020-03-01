/*

by Jean Moreno
from https://gist.github.com/jean-moreno/724c5d04d619c55f0bcda433b053df5d

*/
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PixelWizards.Utilities
{
	public class TextureCombiner : EditorWindow
	{
		[MenuItem("Tools/Texture Combiner...")]
		static void Open()
		{
			var w = GetWindow<TextureCombiner>(true, "Texture Combiner");
			w.minSize = new Vector2(870, 420);
			w.maxSize = new Vector2(870, 420);
		}

		public enum SaveFormat { PNG, EXR }
		public enum Channel { R, G, B, A, RGBLuminance }

		Texture2D textureR, textureG, textureB, textureA;
		Channel sourceR, sourceG, sourceB, sourceA;
		RenderTexture textureCombined;
		RenderTexture textureCombinedAlpha;
		Material blitMaterial;
		Material blitMaterialAlpha;
		SaveFormat saveFormat;
		bool removeCompression = true;
		bool removeCompressionPreview = false;
		int textureSize;
		string[] textureSizes = new string[] { "128", "256", "512", "1024", "2048", "4096", "Custom" };
		int textureWidth = 128;
		int textureHeight = 128;
		Texture2D textureSaved;

		void OnGUI()
		{
			GUILayout.Label("TEXTURE COMBINER", EditorStyles.boldLabel);
			GUILayout.Space(8);

			EditorGUI.BeginChangeCheck();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Combined texture: ", GUILayout.ExpandWidth(false));
			var newTexture = (Texture2D)EditorGUILayout.ObjectField(textureSaved, typeof(Texture2D), false);
			GUILayout.EndHorizontal();
			if (EditorGUI.EndChangeCheck() && newTexture != textureSaved)
			{
				if (Load(newTexture))
				{
					textureSaved = newTexture;
				}
			}
			GUILayout.Space(8f);

			float space = 8f;
			var r = EditorGUILayout.GetControlRect(false, (64f + space) * 4f);
			var texRect = r;
			texRect.x += 24f;
			texRect.height = 64f;
			texRect.width = 64f;
			textureR = (Texture2D)EditorGUI.ObjectField(texRect, textureR, typeof(Texture2D), false);
			texRect.y += 64f + space;
			textureG = (Texture2D)EditorGUI.ObjectField(texRect, textureG, typeof(Texture2D), false);
			texRect.y += 64f + space;
			textureB = (Texture2D)EditorGUI.ObjectField(texRect, textureB, typeof(Texture2D), false);
			texRect.y += 64f + space;
			textureA = (Texture2D)EditorGUI.ObjectField(texRect, textureA, typeof(Texture2D), false);

			var lblRect = r;
			lblRect.width = 20f;
			lblRect.x += 4f;
			lblRect.y += 22f;
			GUI.Label(lblRect, "R", EditorStyles.largeLabel);
			lblRect.y += 64f + space;
			GUI.Label(lblRect, "G", EditorStyles.largeLabel);
			lblRect.y += 64f + space;
			GUI.Label(lblRect, "B", EditorStyles.largeLabel);
			lblRect.y += 64f + space;
			GUI.Label(lblRect, "A", EditorStyles.largeLabel);

			var chanRect = r;
			chanRect.width = 120f;
			chanRect.x += texRect.x + texRect.width + space;
			chanRect.height = 64f;
			sourceR = (Channel)GUI_SourceChannel(chanRect, sourceR);
			chanRect.y += 64f + space;
			sourceG = (Channel)GUI_SourceChannel(chanRect, sourceG);
			chanRect.y += 64f + space;
			sourceB = (Channel)GUI_SourceChannel(chanRect, sourceB);
			chanRect.y += 64f + space;
			sourceA = (Channel)GUI_SourceChannel(chanRect, sourceA);

			var resultRect = r;
			resultRect.height = (64f + space) * 4f;
			resultRect.x += lblRect.x + lblRect.width + texRect.width + chanRect.width + 64f;
			resultRect.width = resultRect.height;

			if (textureCombined != null || textureSaved != null)
			{
				var alphaRect = resultRect;
				alphaRect.x += resultRect.width + space;

				//handy way to highlight the saved texture when clicking on the big preview
				if (textureSaved != null)
				{
					EditorGUI.ObjectField(resultRect, textureSaved, typeof(Texture2D), false);
					EditorGUI.ObjectField(alphaRect, textureSaved, typeof(Texture2D), false);
				}

				if (textureCombined != null)
				{
					GUI.Box(resultRect, GUIContent.none);
					GUI.Box(alphaRect, GUIContent.none);
					//rgb
					GUI.DrawTexture(resultRect, textureCombined, ScaleMode.StretchToFill, false, 0);
					//alpha
					GUI.DrawTexture(alphaRect, textureCombinedAlpha, ScaleMode.StretchToFill, false, 0);
				}
			}
			else
			{
				resultRect.width += resultRect.width + space;
				EditorGUI.HelpBox(resultRect, "texture not generated yet", MessageType.Warning);
			}

			GUILayout.Space(8f);
			GUILayout.BeginHorizontal();

			//Texture size
			EditorGUI.BeginChangeCheck();
			textureSize = EditorGUILayout.Popup(textureSize, textureSizes, GUILayout.Width(60f));
			using (new EditorGUI.DisabledScope(textureSize != textureSizes.Length - 1))
				textureWidth = EditorGUILayout.IntField(textureWidth, GUILayout.Width(60f));
			GUILayout.Label("x");
			using (new EditorGUI.DisabledScope(textureSize != textureSizes.Length - 1))
				textureHeight = EditorGUILayout.IntField(textureHeight, GUILayout.Width(60f));
			if (EditorGUI.EndChangeCheck())
			{
				textureWidth = Mathf.Clamp(textureWidth, 1, 16384);
				textureHeight = Mathf.Clamp(textureHeight, 1, 16384);
				TextureSizeUpdated();
			}

			GUILayout.FlexibleSpace();

			//Save button
			if (GUILayout.Button("SAVE AS...", GUILayout.Width(120f)))
			{
				SaveAs(saveFormat);
			}
			GUILayout.EndHorizontal();

			//Options
			GUILayout.BeginHorizontal();
			saveFormat = (SaveFormat)EditorGUILayout.EnumPopup(saveFormat, GUILayout.Width(60f));
			removeCompression = GUILayout.Toggle(removeCompression, new GUIContent("Remove compression (saved texture)", "Remove compression from input textures for the saved texture"), EditorStyles.miniButton);
			removeCompressionPreview = GUILayout.Toggle(removeCompressionPreview, new GUIContent("Remove compression (preview)", "Remove compression from input textures for the preview image.\n\nThis is a separate setting because disabling/enabling back compression takes a few seconds and that can be annoying when regularly changing the inputs."), EditorStyles.miniButton);

			GUILayout.FlexibleSpace();

			//Reset button
			if (GUILayout.Button("RESET", GUILayout.Width(120f)))
			{
				Reset();
			}
			GUILayout.EndHorizontal();

			if (GUI.changed)
			{
				RefreshCombinedTexture(true);
			}
		}

		void TextureSizeUpdated()
		{
			if (textureSize != textureSizes.Length - 1)
			{
				textureWidth = int.Parse(textureSizes[textureSize]);
				textureHeight = textureWidth;
			}
			UpdateRenderTextures(true);
		}

		void Reset()
		{
			OnDestroy();
			sourceR = Channel.R;
			sourceG = Channel.R;
			sourceB = Channel.R;
			sourceA = Channel.R;
			textureR = null;
			textureG = null;
			textureB = null;
			textureA = null;
			textureSaved = null;
		}

		void UpdateRenderTextures(bool delete)
		{
			if (delete && textureCombined != null)
				ClearRenderTexture(textureCombined);
			if (delete && textureCombinedAlpha != null)
				ClearRenderTexture(textureCombinedAlpha);

			if (textureCombined == null)
			{
				textureCombined = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
				textureCombined.hideFlags = HideFlags.HideAndDontSave;
			}

			if (textureCombinedAlpha == null || (textureCombinedAlpha.width != textureCombined.width || textureCombinedAlpha.height != textureCombined.height))
			{
				textureCombinedAlpha = new RenderTexture(textureCombined);
				textureCombinedAlpha.hideFlags = HideFlags.HideAndDontSave;
			}
		}

		void RefreshCombinedTexture(bool preview)
		{
			UpdateRenderTextures(false);

			if (blitMaterial == null)
			{
				blitMaterial = new Material(Shader.Find("Hidden/TextureCombiner"));
				blitMaterial.name = "Texture Combine";
				blitMaterial.hideFlags = HideFlags.HideAndDontSave;
			}

			if ((!preview && removeCompression) || (preview && removeCompressionPreview))
			{
				//remove compression of source textures
				RemoveCompression();
			}

			blitMaterial.SetTexture("_TexR", textureR);
			blitMaterial.SetTexture("_TexG", textureG);
			blitMaterial.SetTexture("_TexB", textureB);
			blitMaterial.SetTexture("_TexA", textureA);

			blitMaterial.SetFloat("_SrcR", (int)sourceR);
			blitMaterial.SetFloat("_SrcG", (int)sourceG);
			blitMaterial.SetFloat("_SrcB", (int)sourceB);
			blitMaterial.SetFloat("_SrcA", (int)sourceA);

			Graphics.Blit(null, textureCombined, blitMaterial, 0);
			Graphics.Blit(textureCombined, textureCombinedAlpha, blitMaterial, 1);

			//restore compression if necessary
			RestoreCompression();
		}

		void OnDestroy()
		{
			if (textureCombined != null)
				ClearRenderTexture(textureCombined);
			if (textureCombinedAlpha != null)
				ClearRenderTexture(textureCombinedAlpha);
			if (blitMaterial != null)
				DestroyImmediate(blitMaterial);
		}

		void ClearRenderTexture(RenderTexture rt)
		{
			rt.Release();
			DestroyImmediate(rt);
		}

		int GUI_SourceChannel(Rect position, Channel channel)
		{
			var names = System.Enum.GetNames(typeof(Channel));
			var r = position;
			r.height /= names.Length;
			for (int i = 0; i < names.Length; i++)
			{
				if (GUI.Toggle(r, (int)channel == i, names[i], EditorStyles.miniButton)) channel = (Channel)i;
				r.y += r.height;
			}
			return (int)channel;
		}

		void SaveAs(SaveFormat format)
		{
			var path = EditorUtility.SaveFilePanelInProject("Save combined texture", "CombinedTexture", (format == SaveFormat.PNG) ? "png" : "exr", "Save combined texture as...");
			if (!string.IsNullOrEmpty(path))
			{
				//save to file
				var osPath = (Application.dataPath + path.Substring(6)).Replace('/', System.IO.Path.DirectorySeparatorChar);
				//blit to render texture
				RefreshCombinedTexture(false);
				//set active render texture and read pixels
				RenderTexture.active = textureCombined;
				Texture2D texture2D = new Texture2D(textureCombined.width, textureCombined.height, (format == SaveFormat.PNG) ? TextureFormat.ARGB32 : TextureFormat.RGBAHalf, false);
				texture2D.ReadPixels(new Rect(0, 0, textureCombined.width, textureCombined.height), 0, 0);
				RenderTexture.active = null;
				//save file to disk
				byte[] data = (format == SaveFormat.PNG) ? texture2D.EncodeToPNG() : texture2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
				System.IO.File.WriteAllBytes(osPath, data);
				//import new file in Unity
				AssetDatabase.ImportAsset(path);
				//set metadata
				var importer = AssetImporter.GetAtPath(path);
				importer.userData = GetUserData();
				importer.SaveAndReimport();
				//load in UI and select in Project view
				textureSaved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				Selection.objects = new Object[] { textureSaved };
			}
		}

		Dictionary<TextureImporter, TextureImporterCompression> compressionSettings;
		void RemoveCompression()
		{
			compressionSettings = new Dictionary<TextureImporter, TextureImporterCompression>();

			CheckTextureCompression(textureR);
			CheckTextureCompression(textureG);
			CheckTextureCompression(textureB);
			CheckTextureCompression(textureA);
		}

		void CheckTextureCompression(Texture2D texture)
		{
			if (texture != null)
			{
				var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
				if (importer != null && importer.textureCompression != TextureImporterCompression.Uncompressed)
				{
					compressionSettings.Add(importer, importer.textureCompression);
					importer.textureCompression = TextureImporterCompression.Uncompressed;
					importer.SaveAndReimport();
				}
			}
		}

		void RestoreCompression()
		{
			if (compressionSettings != null && compressionSettings.Count > 0)
			{
				foreach (var kvp in compressionSettings)
				{
					kvp.Key.textureCompression = kvp.Value;
					kvp.Key.SaveAndReimport();
				}
			}
			compressionSettings = null;
		}

		bool Load(Texture2D texture)
		{
			if (texture == null)
				return true;

			var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
			if (importer != null)
			{
				if (importer.userData.StartsWith("texture_combiner"))
				{
					//no error check here!
					//may break with different userData
					var userDataSplit = importer.userData.Split(' ');
					var rGuid = userDataSplit[1].Split(':')[1];
					var gGuid = userDataSplit[2].Split(':')[1];
					var bGuid = userDataSplit[3].Split(':')[1];
					var aGuid = userDataSplit[4].Split(':')[1];

					textureR = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(rGuid));
					textureG = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(gGuid));
					textureB = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(bGuid));
					textureA = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(aGuid));

					string errorGUID = "";
					if (!string.IsNullOrEmpty(rGuid) && textureR == null)
					{
						errorGUID += "Red  ";
					}
					if (!string.IsNullOrEmpty(gGuid) && textureG == null)
					{
						errorGUID += "Green  ";
					}
					if (!string.IsNullOrEmpty(bGuid) && textureB == null)
					{
						errorGUID += "Blue  ";
					}
					if (!string.IsNullOrEmpty(aGuid) && textureA == null)
					{
						errorGUID += "Alpha";
					}

					sourceR = (Channel)System.Enum.Parse(typeof(Channel), userDataSplit[5].Split(':')[1]);
					sourceG = (Channel)System.Enum.Parse(typeof(Channel), userDataSplit[6].Split(':')[1]);
					sourceB = (Channel)System.Enum.Parse(typeof(Channel), userDataSplit[7].Split(':')[1]);
					sourceA = (Channel)System.Enum.Parse(typeof(Channel), userDataSplit[8].Split(':')[1]);

					textureSaved = texture;
					if (textureCombined != null)
					{
						textureCombined.Release();
						DestroyImmediate(textureCombined);
					}
					if (textureCombinedAlpha != null)
					{
						textureCombinedAlpha.Release();
						DestroyImmediate(textureCombinedAlpha);
					}

					if (!string.IsNullOrEmpty(errorGUID))
					{
						EditorUtility.DisplayDialog("Error", "Source texture(s) couldn't be found in the project:\n\n" + errorGUID + "\n\nMaybe they have been deleted, or they GUID has been updated?", "Ok");
					}

					return true;
				}
				else
				{
					ShowNotification(new GUIContent("This texture doesn't seem to have been generated with the Texture Combiner"));
				}
			}

			return false;
		}

		string GetUserData()
		{
			return string.Format("texture_combiner r:{0} g:{1} b:{2} a:{3} rc:{4} gc:{5} bc:{6} ac:{7}",
				AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureR)),
				AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureG)),
				AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureB)),
				AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(textureA)),
				sourceR,
				sourceG,
				sourceB,
				sourceA
				);
		}
	}
}