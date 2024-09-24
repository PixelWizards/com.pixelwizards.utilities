using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace PixelWizards.Utilities
{
	public class BatchExtractMaterials : EditorWindow
	{
		private enum ExtractMode { Extract = 0, Remap = 1, Ignore = 2 };

		[System.Serializable]
		private class ExtractData
		{
			public GameObject model;

			public List<string> materialNames = new List<string>();
			public List<Material> originalMaterials = new List<Material>();
			public List<Material> remappedMaterials = new List<Material>();
			public List<ExtractMode> materialExtractModes = new List<ExtractMode>();

			public ExtractData() { }
			public ExtractData( GameObject model ) { this.model = model; }
		}

		private class RemapAllPopup : EditorWindow
		{
			private List<Material> remapFrom = new List<Material>( 2 );
			private Material remapTo;
			private bool skipIgnoredMaterials;

			private Vector2 scrollPos;

			private System.Action<List<Material>, Material, bool> onRemapConfirmed;

			public static void ShowAt( Rect buttonRect, Vector2 size, System.Action<List<Material>, Material, bool> onRemapConfirmed )
			{
				buttonRect.position = GUIUtility.GUIToScreenPoint( buttonRect.position );

				remapAllPopup = GetWindow<RemapAllPopup>( true );
				remapAllPopup.position = new Rect( buttonRect.position + new Vector2( ( buttonRect.width - size.x ) * 0.5f, buttonRect.height ), size );
				remapAllPopup.minSize = size;
				remapAllPopup.titleContent = new GUIContent( "Remap All..." );
				remapAllPopup.skipIgnoredMaterials = EditorPrefs.GetBool( "BEM_SkipIgnoredMats", true );
				remapAllPopup.onRemapConfirmed = onRemapConfirmed;
				remapAllPopup.scrollPos = Vector2.zero;
				remapAllPopup.Show();
			}

			public static void Hide()
			{
				if( remapAllPopup )
				{
					remapAllPopup.Close();
					remapAllPopup = null;
				}
			}

			private void OnDestroy()
			{
				remapAllPopup = null;
			}

			private void OnGUI()
			{
				if( !remapAllPopup )
				{
					Close();
					GUIUtility.ExitGUI();
				}

				Event ev = Event.current;

				EditorGUILayout.LabelField( "This will find all materials that point to 'Remap From' and remap them to 'Remap To'. If 'Remap From' is empty, all materials will be remapped to 'Remap To'.", EditorStyles.wordWrappedLabel );

				scrollPos = EditorGUILayout.BeginScrollView( scrollPos );

				GUILayout.BeginHorizontal();
				GUILayout.Label( "Remap From (drag & drop here)" );

				if( remapFrom.Count == 0 )
					remapFrom.Add( null );

				// Allow drag & dropping materials to array
				// Credit: https://answers.unity.com/answers/657877/view.html
				if( ( ev.type == EventType.DragPerform || ev.type == EventType.DragUpdated ) && GUILayoutUtility.GetLastRect().Contains( ev.mousePosition ) )
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					if( ev.type == EventType.DragPerform )
					{
						DragAndDrop.AcceptDrag();

						Object[] draggedObjects = DragAndDrop.objectReferences;
						for( int i = 0; i < draggedObjects.Length; i++ )
						{
							Material material = draggedObjects[i] as Material;
							if( !material )
								continue;

							if( !remapFrom.Contains( material ) )
							{
								bool replacedNullElement = false;
								for( int j = 0; j < remapFrom.Count; j++ )
								{
									if( !remapFrom[j] )
									{
										remapFrom[j] = material;
										replacedNullElement = true;
										break;
									}
								}

								if( !replacedNullElement )
									remapFrom.Add( material );
							}
						}
					}

					ev.Use();
				}

				if( GUILayout.Button( "+", GL_WIDTH_25 ) )
					remapFrom.Insert( 0, null );

				GUILayout.EndHorizontal();

				for( int i = 0; i < remapFrom.Count; i++ )
				{
					GUILayout.BeginHorizontal();

					remapFrom[i] = EditorGUILayout.ObjectField( GUIContent.none, remapFrom[i], typeof( Material ), false ) as Material;

					if( GUILayout.Button( "+", GL_WIDTH_25 ) )
						remapFrom.Insert( i + 1, null );

					if( GUILayout.Button( "-", GL_WIDTH_25 ) )
					{
						// Lists with no elements look ugly, always keep a dummy null variable
						if( remapFrom.Count > 1 )
							remapFrom.RemoveAt( i-- );
						else
							remapFrom[0] = null;
					}

					GUILayout.EndHorizontal();
				}

				EditorGUILayout.EndScrollView();

				remapTo = EditorGUILayout.ObjectField( "Remap To", remapTo, typeof( Material ), false ) as Material;

				EditorGUI.BeginChangeCheck();
				skipIgnoredMaterials = EditorGUILayout.Toggle( "Skip Ignored Materials", skipIgnoredMaterials );
				if( EditorGUI.EndChangeCheck() )
					EditorPrefs.SetBool( "BEM_SkipIgnoredMats", skipIgnoredMaterials );

				EditorGUILayout.Space();

				GUILayout.BeginHorizontal();
				if( GUILayout.Button( "Cancel" ) )
					Close();
				if( GUILayout.Button( "Apply" ) )
				{
					if( remapTo && onRemapConfirmed != null )
					{
						bool remapFromIsFilled = false;
						for( int i = 0; i < remapFrom.Count; i++ )
						{
							if( remapFrom[i] )
							{
								remapFromIsFilled = true;
								break;
							}
						}

						onRemapConfirmed( remapFromIsFilled ? remapFrom : null, remapTo, skipIgnoredMaterials );
					}

					Close();
				}
				GUILayout.EndHorizontal();

				GUILayout.Space( 5f );
			}
		}

		private const string HELP_TEXT =
			"- Extract: material will be extracted to the destination folder\n" +
			"- Remap: material will be remapped to an existing material asset" +
#if UNITY_2019_1_OR_NEWER
			" (when Remap is the default value, then it means that a material that satisfies 'Default Material Remap Conditions' was found)" +
#endif
			". If Remap points to an embedded material, then that embedded material will first be extracted\n" +
			"- Ignore: material's current value will stay intact (when Ignore is the default value, either the material couldn't be found " +
			"or it was already extracted)";

		private static readonly GUILayoutOption GL_WIDTH_25 = GUILayout.Width( 25f );
		private readonly GUILayoutOption GL_WIDTH_75 = GUILayout.Width( 75f );
		private readonly GUILayoutOption GL_MIN_WIDTH_50 = GUILayout.MinWidth( 50f );

		private string materialsFolder = "Assets/Materials";
		private List<ExtractData> modelData = new List<ExtractData>( 16 );

#if UNITY_2019_1_OR_NEWER
		private bool remappedMaterialNamesMustMatch = false;
		private bool remappedMaterialPropertiesMustMatch = true;
		private bool dontRemapExtractedMaterials = true;
		private bool dontRemapMaterialsAcrossDifferentModels = false;
#endif

		private bool inModelSelectionPhase = true;

		private Rect remapAllButtonRect;
		private static RemapAllPopup remapAllPopup;

		private Vector2 scrollPos;

		[MenuItem( "Assets/Batch Extract Materials" )]
		private static void Init()
		{
			BatchExtractMaterials window = GetWindow<BatchExtractMaterials>();
			window.titleContent = new GUIContent( "Extract Materials" );
			window.minSize = new Vector2( 300f, 120f );
			window.Show();
		}

		private void OnDestroy()
		{
			// Close RemapAllPopup with this window
			RemapAllPopup.Hide();
		}

		private void OnFocus()
		{
			// Don't let RemapAllPopup be obstructed by this window
			// We are using delayCall because otherwise clicking an ObjectField in this window doesn't highlight that material in the Project window
			EditorApplication.delayCall += () =>
			{
				if( remapAllPopup )
					remapAllPopup.Focus();
			};
		}

		private void OnGUI()
		{
			scrollPos = EditorGUILayout.BeginScrollView( scrollPos );

			GUI.enabled = inModelSelectionPhase;
			DrawDestinationPathField();
			DrawModelsToProcessList();
			DrawMaterialRemapConditionsField();
			GUI.enabled = true;

			bool modelsToProcessListIsFilled = modelData.Find( ( data ) => data.model ) != null;

			if( inModelSelectionPhase )
			{
				GUI.enabled = modelsToProcessListIsFilled && !string.IsNullOrEmpty( materialsFolder ) && materialsFolder.StartsWith( "Assets" );
				if( GUILayout.Button( "Next" ) )
				{
					inModelSelectionPhase = false;
					CalculateRemappedMaterials();

					GUIUtility.ExitGUI();
				}
			}
			else
			{
				DrawMaterialRemapList();

				GUILayout.BeginHorizontal();

				if( GUILayout.Button( "Back" ) )
				{
					inModelSelectionPhase = true;
					RemapAllPopup.Hide();

					GUIUtility.ExitGUI();
				}

				Color c = GUI.backgroundColor;
				GUI.backgroundColor = Color.green;

				GUI.enabled = modelsToProcessListIsFilled;
				if( GUILayout.Button( "Extract!" ) )
				{
					inModelSelectionPhase = true;
					RemapAllPopup.Hide();

					ExtractMaterials();
					GUIUtility.ExitGUI();
				}

				GUI.backgroundColor = c;
				GUILayout.EndHorizontal();
			}

			GUI.enabled = true;

			EditorGUILayout.Space();
			EditorGUILayout.EndScrollView();
		}

		private void DrawDestinationPathField()
		{
			Event ev = Event.current;

			GUILayout.BeginHorizontal();

			materialsFolder = EditorGUILayout.TextField( "Extract Materials To", materialsFolder );

			// Allow drag & dropping a folder to the text field
			// Credit: https://answers.unity.com/answers/657877/view.html
			if( ( ev.type == EventType.DragPerform || ev.type == EventType.DragUpdated ) && GUILayoutUtility.GetLastRect().Contains( ev.mousePosition ) )
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				if( ev.type == EventType.DragPerform )
				{
					DragAndDrop.AcceptDrag();

					string[] draggedFiles = DragAndDrop.paths;
					for( int i = 0; i < draggedFiles.Length; i++ )
					{
						if( !string.IsNullOrEmpty( draggedFiles[i] ) && AssetDatabase.IsValidFolder( draggedFiles[i] ) )
						{
							materialsFolder = draggedFiles[i];
							break;
						}
					}
				}

				ev.Use();
			}

			if( GUILayout.Button( "o", GL_WIDTH_25 ) )
			{
				string selectedPath = EditorUtility.OpenFolderPanel( "Choose output directory", "Assets", "" );
				if( !string.IsNullOrEmpty( selectedPath ) )
				{
					selectedPath = selectedPath.Replace( '\\', '/' ) + "/";

					int relativePathIndex = selectedPath.IndexOf( "/Assets/" ) + 1;
					if( relativePathIndex > 0 )
						materialsFolder = selectedPath.Substring( relativePathIndex, selectedPath.Length - relativePathIndex - 1 );
				}

				GUIUtility.keyboardControl = 0; // Remove focus from active text field
			}

			GUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}

		private void DrawModelsToProcessList()
		{
			Event ev = Event.current;

			GUILayout.BeginHorizontal();
			GUILayout.Label( "Models To Process (drag & drop here)" );

			if( modelData.Count == 0 )
				modelData.Add( new ExtractData() );

			// Allow drag & dropping models to array
			// Credit: https://answers.unity.com/answers/657877/view.html
			if( ( ev.type == EventType.DragPerform || ev.type == EventType.DragUpdated ) && GUILayoutUtility.GetLastRect().Contains( ev.mousePosition ) )
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				if( ev.type == EventType.DragPerform )
				{
					DragAndDrop.AcceptDrag();

					Object[] draggedObjects = DragAndDrop.objectReferences;
					for( int i = 0; i < draggedObjects.Length; i++ )
					{
						if( !( draggedObjects[i] as GameObject ) || PrefabUtility.GetPrefabAssetType( draggedObjects[i] ) != PrefabAssetType.Model )
							continue;

						bool modelAlreadyExists = false;
						for( int j = 0; j < modelData.Count; j++ )
						{
							if( modelData[j].model == draggedObjects[i] )
							{
								modelAlreadyExists = true;
								break;
							}
						}

						if( !modelAlreadyExists )
						{
							bool replacedNullElement = false;
							for( int j = 0; j < modelData.Count; j++ )
							{
								if( !modelData[j].model )
								{
									modelData[j] = new ExtractData( draggedObjects[i] as GameObject );
									replacedNullElement = true;
									break;
								}
							}

							if( !replacedNullElement )
								modelData.Add( new ExtractData( draggedObjects[i] as GameObject ) );
						}
					}
				}

				ev.Use();
			}

			if( GUILayout.Button( "+", GL_WIDTH_25 ) )
				modelData.Insert( 0, new ExtractData() );

			GUILayout.EndHorizontal();

			for( int i = 0; i < modelData.Count; i++ )
			{
				ExtractData element = modelData[i];

				GUI.changed = false;
				GUILayout.BeginHorizontal();

				GameObject prevObject = element.model;
				GameObject newObject = EditorGUILayout.ObjectField( GUIContent.none, prevObject, typeof( GameObject ), false ) as GameObject;
				if( newObject && PrefabUtility.GetPrefabAssetType( newObject ) != PrefabAssetType.Model )
					newObject = prevObject;

				modelData[i].model = newObject;

				if( GUILayout.Button( "+", GL_WIDTH_25 ) )
					modelData.Insert( i + 1, new ExtractData() );

				if( GUILayout.Button( "-", GL_WIDTH_25 ) )
				{
					// Lists with no elements look ugly, always keep a dummy null variable
					if( modelData.Count > 1 )
						modelData.RemoveAt( i-- );
					else
						modelData[0] = new ExtractData();
				}

				GUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
		}

		private void DrawMaterialRemapConditionsField()
		{
#if UNITY_2019_1_OR_NEWER
			EditorGUILayout.LabelField( "Default Material Remap Conditions" );
			EditorGUI.indentLevel++;

			remappedMaterialNamesMustMatch = EditorGUILayout.ToggleLeft( "Material names must match", remappedMaterialNamesMustMatch );
			remappedMaterialPropertiesMustMatch = EditorGUILayout.ToggleLeft( "Material properties must match", remappedMaterialPropertiesMustMatch );
			dontRemapExtractedMaterials = EditorGUILayout.ToggleLeft( "Don't remap already extracted materials", dontRemapExtractedMaterials );
			dontRemapMaterialsAcrossDifferentModels = EditorGUILayout.ToggleLeft( "Don't remap Model A's materials to Model B (i.e. different models won't share the same materials)", dontRemapMaterialsAcrossDifferentModels );

			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
#endif
		}

		private void DrawMaterialRemapList()
		{
			EditorGUILayout.HelpBox( HELP_TEXT, MessageType.Info );

			GUILayout.BeginHorizontal();

			if( GUILayout.Button( "Extract All" ) )
			{
				for( int i = 0; i < modelData.Count; i++ )
				{
					for( int j = 0; j < modelData[i].materialExtractModes.Count; j++ )
						modelData[i].materialExtractModes[j] = ExtractMode.Extract;
				}
			}

			if( GUILayout.Button( "Remap All..." ) )
			{
				RemapAllPopup.ShowAt( remapAllButtonRect, new Vector2( 325f, 250f ), ( List<Material> remapFrom, Material remapTo, bool skipIgnoredMaterials ) =>
				{
					for( int i = 0; i < modelData.Count; i++ )
					{
						ExtractData data = modelData[i];
						for( int j = 0; j < data.remappedMaterials.Count; j++ )
						{
							switch( data.materialExtractModes[j] )
							{
								case ExtractMode.Extract:
								{
									if( remapFrom == null || ( data.originalMaterials[j] && remapFrom.Contains( data.originalMaterials[j] ) ) )
									{
										data.materialExtractModes[j] = ExtractMode.Remap;
										data.remappedMaterials[j] = remapTo;
									}

									break;
								}
								case ExtractMode.Remap:
								{
									if( remapFrom == null || ( data.remappedMaterials[j] && remapFrom.Contains( data.remappedMaterials[j] ) ) )
										data.remappedMaterials[j] = remapTo;

									break;
								}
								case ExtractMode.Ignore:
								{
									if( !skipIgnoredMaterials && ( remapFrom == null || ( data.originalMaterials[j] && remapFrom.Contains( data.originalMaterials[j] ) ) ) )
									{
										data.materialExtractModes[j] = ExtractMode.Remap;
										data.remappedMaterials[j] = remapTo;
									}

									break;
								}
							}
						}
					}

					Repaint();
				} );
			}

			if( Event.current.type == EventType.Repaint )
				remapAllButtonRect = GUILayoutUtility.GetLastRect();

			if( GUILayout.Button( "Ignore All" ) )
			{
				for( int i = 0; i < modelData.Count; i++ )
				{
					for( int j = 0; j < modelData[i].materialExtractModes.Count; j++ )
						modelData[i].materialExtractModes[j] = ExtractMode.Ignore;
				}
			}

			GUILayout.EndHorizontal();

			EditorGUILayout.Space();

			for( int i = 0; i < modelData.Count; i++ )
			{
				ExtractData data = modelData[i];
				if( !data.model )
					continue;

				GUI.enabled = false;
				EditorGUILayout.ObjectField( GUIContent.none, data.model, typeof( GameObject ), false );
				GUI.enabled = true;

				if( data.originalMaterials.Count == 0 )
					EditorGUILayout.LabelField( "This model has no materials..." );

				for( int j = 0; j < data.originalMaterials.Count; j++ )
				{
					GUILayout.BeginHorizontal();

					EditorGUILayout.PrefixLabel( data.materialNames[j] );

					data.materialExtractModes[j] = (ExtractMode) EditorGUILayout.EnumPopup( GUIContent.none, data.materialExtractModes[j], GL_WIDTH_75 );
					if( data.materialExtractModes[j] == ExtractMode.Remap )
					{
						EditorGUI.BeginChangeCheck();
						data.remappedMaterials[j] = EditorGUILayout.ObjectField( GUIContent.none, data.remappedMaterials[j], typeof( Material ), false, GL_MIN_WIDTH_50 ) as Material;
						if( EditorGUI.EndChangeCheck() && ( !data.remappedMaterials[j] || data.remappedMaterials[j] == data.originalMaterials[j] ) )
							data.materialExtractModes[j] = ExtractMode.Ignore;
					}
					else
					{
						GUI.enabled = false;
						EditorGUILayout.ObjectField( GUIContent.none, data.originalMaterials[j], typeof( Material ), false, GL_MIN_WIDTH_50 );
						GUI.enabled = true;
					}

					GUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
			}
		}

		private void CalculateRemappedMaterials()
		{
#if UNITY_2019_1_OR_NEWER
			// Key: Material CRC (material.ComputeCRC)
			// Value: All materials sharing that CRC
			Dictionary<int, HashSet<Material>> duplicateMaterialsLookup = new Dictionary<int, HashSet<Material>>( modelData.Count * 8 );

			// Add all existing materials at materialsFolder to the lookup table
			if( !dontRemapMaterialsAcrossDifferentModels && Directory.Exists( materialsFolder ) )
			{
				string[] existingMaterialPaths = Directory.GetFiles( materialsFolder, "*.mat", SearchOption.TopDirectoryOnly );
				for( int i = 0; i < existingMaterialPaths.Length; i++ )
				{
					Material material = AssetDatabase.LoadMainAssetAtPath( existingMaterialPaths[i] ) as Material;
					if( material )
						GetMaterialsWithCRC( duplicateMaterialsLookup, material ).Add( material );
				}
			}
#endif

			for( int i = 0; i < modelData.Count; i++ )
			{
				ExtractData data = modelData[i];
				if( !data.model )
				{
					modelData.RemoveAt( i-- );
					continue;
				}

				string modelPath = AssetDatabase.GetAssetPath( data.model );
				ModelImporter modelImporter = AssetImporter.GetAtPath( modelPath ) as ModelImporter;
				if( !modelImporter )
				{
					Debug.LogWarning( "Couldn't get ModelImporter from asset: " + AssetDatabase.GetAssetPath( data.model ), data.model );
					modelData.RemoveAt( i-- );
					continue;
				}

				// Reset previously assigned values to this entry (if any)
				data = modelData[i] = new ExtractData( data.model );

				Object[] embeddedAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath( modelPath );
				List<Material> embeddedMaterials = new List<Material>( embeddedAssets.Length );
				for( int j = 0; j < embeddedAssets.Length; j++ )
				{
					Material embeddedMaterial = embeddedAssets[j] as Material;
					if( embeddedMaterial )
						embeddedMaterials.Add( embeddedMaterial );
				}

				// Get the model's current material remapping
				// Credit: https://forum.unity.com/threads/batch-change-all-fbx-default-materials-help.626341/#post-6530939
				using( SerializedObject so = new SerializedObject( modelImporter ) )
				{
					SerializedProperty materials = so.FindProperty( "m_Materials" );
					SerializedProperty externalObjects = so.FindProperty( "m_ExternalObjects" );

					for( int materialIndex = 0; materialIndex < materials.arraySize; materialIndex++ )
					{
						SerializedProperty id = materials.GetArrayElementAtIndex( materialIndex );
						string name = id.FindPropertyRelative( "name" ).stringValue;
						string type = id.FindPropertyRelative( "type" ).stringValue;

						Material material = null;
						for( int externalObjectIndex = 0; externalObjectIndex < externalObjects.arraySize; externalObjectIndex++ )
						{
							SerializedProperty pair = externalObjects.GetArrayElementAtIndex( externalObjectIndex );
							string externalName = pair.FindPropertyRelative( "first.name" ).stringValue;
							string externalType = pair.FindPropertyRelative( "first.type" ).stringValue;

							if( externalType == type && externalName == name && ( pair = pair.FindPropertyRelative( "second" ) ) != null )
							{
								material = pair.objectReferenceValue as Material;
								break;
							}
						}

						if( !material )
							material = embeddedMaterials.Find( ( m ) => m.name == name );

						data.materialNames.Add( name );
						data.originalMaterials.Add( material );

						if( !material )
						{
							data.materialExtractModes.Add( ExtractMode.Ignore );
							data.remappedMaterials.Add( null );
						}
						else
						{
							bool materialAlreadyExtracted = AssetDatabase.IsMainAsset( material );
#if UNITY_2019_1_OR_NEWER
							HashSet<Material> duplicateMaterials = GetMaterialsWithCRC( duplicateMaterialsLookup, material );
							Material remappedMaterial = null;

							// - Material was already extracted: remap the material only if 'dontRemapExtractedMaterials' is false
							// - 'dontRemapMaterialsAcrossDifferentModels' is true: only remap with a material from the same model
							// - 'remappedMaterialPropertiesMustMatch' is true: only remap with a material whose properties match
							//   the current material's properties
							// - Only 'remappedMaterialNamesMustMatch' is true: remap with a material with the same name; properties
							//   of the two materials may not match
							if( !materialAlreadyExtracted || !dontRemapExtractedMaterials )
							{
								if( remappedMaterialPropertiesMustMatch )
								{
									foreach( Material _material in duplicateMaterials )
									{
										if( _material.name == name || ( !remappedMaterial && !remappedMaterialNamesMustMatch ) )
											remappedMaterial = _material;
									}
								}
								else if( remappedMaterialNamesMustMatch )
									remappedMaterial = GetMaterialWithName( duplicateMaterialsLookup, name );
							}

							if( remappedMaterial && remappedMaterial != material )
							{
								data.materialExtractModes.Add( ExtractMode.Remap );
								data.remappedMaterials.Add( remappedMaterial );
							}
							else
#endif
							{
								data.materialExtractModes.Add( materialAlreadyExtracted ? ExtractMode.Ignore : ExtractMode.Extract );
								data.remappedMaterials.Add( null );

#if UNITY_2019_1_OR_NEWER
								duplicateMaterials.Add( material );
#endif
							}
						}
					}
				}

#if UNITY_2019_1_OR_NEWER
				if( dontRemapMaterialsAcrossDifferentModels )
					duplicateMaterialsLookup.Clear();
#endif
			}
		}

#if UNITY_2019_1_OR_NEWER
		private HashSet<Material> GetMaterialsWithCRC( Dictionary<int, HashSet<Material>> lookup, Material material )
		{
			int crcHash = material.ComputeCRC();
			HashSet<Material> result;
			if( !lookup.TryGetValue( crcHash, out result ) )
				lookup[crcHash] = result = new HashSet<Material>();

			return result;
		}

		private Material GetMaterialWithName( Dictionary<int, HashSet<Material>> lookup, string name )
		{
			foreach( HashSet<Material> allMaterials in lookup.Values )
			{
				foreach( Material material in allMaterials )
				{
					if( material.name == name )
						return material;
				}
			}

			return null;
		}
#endif

		private void ExtractMaterials()
		{
			if( materialsFolder.EndsWith( "/" ) )
				materialsFolder = materialsFolder.Substring( 0, materialsFolder.Length - 1 );

			if( !Directory.Exists( materialsFolder ) )
			{
				Directory.CreateDirectory( materialsFolder );
				AssetDatabase.ImportAsset( materialsFolder, ImportAssetOptions.ForceUpdate );
			}

			List<AssetImporter> dirtyModelImporters = new List<AssetImporter>( modelData.Count );
			Dictionary<Material, Material> extractedMaterials = new Dictionary<Material, Material>( modelData.Count * 8 );

			for( int i = 0; i < modelData.Count; i++ )
			{
				ExtractData data = modelData[i];
				if( !data.model )
					continue;

				AssetImporter modelImporter = AssetImporter.GetAtPath( AssetDatabase.GetAssetPath( data.model ) );

				// Remap/extract the model's materials
				// Credit: https://forum.unity.com/threads/batch-change-all-fbx-default-materials-help.626341/#post-6530939
				using( SerializedObject so = new SerializedObject( modelImporter ) )
				{
					SerializedProperty materials = so.FindProperty( "m_Materials" );
					SerializedProperty externalObjects = so.FindProperty( "m_ExternalObjects" );

					for( int materialIndex = 0; materialIndex < materials.arraySize; materialIndex++ )
					{
						SerializedProperty id = materials.GetArrayElementAtIndex( materialIndex );
						string name = id.FindPropertyRelative( "name" ).stringValue;
						string type = id.FindPropertyRelative( "type" ).stringValue;

						// j: index of the target material in data's lists
						int j = ( materialIndex < data.materialNames.Count && data.materialNames[materialIndex] == name ) ? materialIndex : data.materialNames.IndexOf( name );
						if( j < 0 )
						{
							// This can only occur if user reimports the model with more materials when 'inModelSelectionPhase' is false
							Debug.LogWarning( data.model.name + "." + name + " material has no matching data, skipped", data.model );
							continue;
						}

						Material targetMaterial = null;
						switch( data.materialExtractModes[j] )
						{
							case ExtractMode.Extract:
							{
								if( data.originalMaterials[j] && !AssetDatabase.IsMainAsset( data.originalMaterials[j] ) )
									targetMaterial = data.originalMaterials[j];
								else
									Debug.LogWarning( data.model.name + "." + name + " isn't extracted because either the material doesn't exist or it is already extracted", data.model );

								break;
							}
							case ExtractMode.Remap:
							{
								if( data.remappedMaterials[j] && ( data.originalMaterials[j] != data.remappedMaterials[j] || !AssetDatabase.IsMainAsset( data.remappedMaterials[j] ) ) )
									targetMaterial = data.remappedMaterials[j];
								else
									Debug.LogWarning( data.model.name + "." + name + " isn't remapped because either the material doesn't exist or it is already extracted", data.model );

								break;
							}
						}

						if( !targetMaterial )
							continue;
						else if( !AssetDatabase.IsMainAsset( targetMaterial ) )
						{
							Material extractedMaterial;
							if( !extractedMaterials.TryGetValue( targetMaterial, out extractedMaterial ) )
							{
								extractedMaterials[targetMaterial] = extractedMaterial = new Material( targetMaterial );
								AssetDatabase.CreateAsset( extractedMaterial, AssetDatabase.GenerateUniqueAssetPath( materialsFolder + "/" + targetMaterial.name + ".mat" ) );
							}

							targetMaterial = extractedMaterial;
						}

						SerializedProperty materialProperty = null;
						for( int externalObjectIndex = 0; externalObjectIndex < externalObjects.arraySize; externalObjectIndex++ )
						{
							SerializedProperty pair = externalObjects.GetArrayElementAtIndex( externalObjectIndex );
							string externalName = pair.FindPropertyRelative( "first.name" ).stringValue;
							string externalType = pair.FindPropertyRelative( "first.type" ).stringValue;

							if( externalType == type && externalName == name )
							{
								materialProperty = pair.FindPropertyRelative( "second" );
								break;
							}
						}

						if( materialProperty == null )
						{
							SerializedProperty currentSerializedProperty = externalObjects.GetArrayElementAtIndex( externalObjects.arraySize++ );
							currentSerializedProperty.FindPropertyRelative( "first.name" ).stringValue = name;
							currentSerializedProperty.FindPropertyRelative( "first.type" ).stringValue = type;
							currentSerializedProperty.FindPropertyRelative( "first.assembly" ).stringValue = id.FindPropertyRelative( "assembly" ).stringValue;
							currentSerializedProperty.FindPropertyRelative( "second" ).objectReferenceValue = targetMaterial;
						}
						else
							materialProperty.objectReferenceValue = targetMaterial;
					}

					if( so.hasModifiedProperties )
					{
						dirtyModelImporters.Add( modelImporter );
						so.ApplyModifiedPropertiesWithoutUndo();
					}
				}
			}

			for( int i = 0; i < dirtyModelImporters.Count; i++ )
				dirtyModelImporters[i].SaveAndReimport();
		}
	}
}