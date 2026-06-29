using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CloudheadGames.Darkspace.EditorTools
{
	public class EmbeddedMaterialBatchRemapperWindow : EditorWindow
	{
		private enum RemapMode
		{
			MatchByNameFromFolder,
			SingleMaterialOverride
		}

		private enum DuplicateMaterialHandling
		{
			SkipDuplicates,
			UseFirstMatch
		}

		private sealed class MaterialMatch
		{
			public string materialName;
			public string modelPath;
			public Material matchedMaterial;
			public List<Material> duplicateMatches = new List<Material>();
			public bool alreadyMapped;
			public string status;
		}

		private RemapMode _remapMode = RemapMode.MatchByNameFromFolder;

		private DefaultAsset _materialSearchFolder;
		private Material _singleOverrideMaterial;

		private bool _processFoldersRecursively = true;
		private bool _caseInsensitiveMatching = true;
		private bool _dryRunOnly = true;

		private DuplicateMaterialHandling _duplicateMaterialHandling = DuplicateMaterialHandling.SkipDuplicates;

		private Vector2 _scroll;
		private Vector2 _resultsScroll;

		private readonly List<MaterialMatch> _previewResults = new List<MaterialMatch>();

		[MenuItem("Assets/Materials/Embedded Material Batch Remapper")]
		private static void Open()
		{
			GetWindow<EmbeddedMaterialBatchRemapperWindow>("Material Remapper");
		}

		private void OnGUI()
		{
			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			EditorGUILayout.LabelField("Embedded Material Batch Remapper", EditorStyles.boldLabel);

			EditorGUILayout.Space();

			EditorGUILayout.HelpBox(
				"Select FBX/model assets and/or folders in the Project window. " +
				"The tool will read embedded/imported material names from those models and remap them to project materials.",
				MessageType.Info);

			EditorGUILayout.Space();

			_remapMode = (RemapMode)EditorGUILayout.EnumPopup("Remap Mode", _remapMode);

			EditorGUILayout.Space();

			if (_remapMode == RemapMode.MatchByNameFromFolder)
			{
				_materialSearchFolder = (DefaultAsset)EditorGUILayout.ObjectField(
					"Material Search Folder",
					_materialSearchFolder,
					typeof(DefaultAsset),
					false);

				_caseInsensitiveMatching = EditorGUILayout.Toggle(
					"Case Insensitive Matching",
					_caseInsensitiveMatching);

				_duplicateMaterialHandling = (DuplicateMaterialHandling)EditorGUILayout.EnumPopup(
					"Duplicate Material Handling",
					_duplicateMaterialHandling);
			}
			else
			{
				_singleOverrideMaterial = (Material)EditorGUILayout.ObjectField(
					"Override Material",
					_singleOverrideMaterial,
					typeof(Material),
					false);
			}

			EditorGUILayout.Space();

			_processFoldersRecursively = EditorGUILayout.Toggle(
				"Process Folders Recursively",
				_processFoldersRecursively);

			_dryRunOnly = EditorGUILayout.Toggle(
				"Preview Only",
				_dryRunOnly);

			EditorGUILayout.Space();

			using (new EditorGUI.DisabledScope(!CanPreviewOrApply()))
			{
				if (GUILayout.Button(_dryRunOnly ? "Preview Remaps" : "Apply Remaps", GUILayout.Height(32)))
				{
					Run();
				}
			}

			EditorGUILayout.Space();

			DrawResults();

			EditorGUILayout.Space(24);
			EditorGUILayout.EndScrollView();
		}

		private bool CanPreviewOrApply()
		{
			if (_remapMode == RemapMode.MatchByNameFromFolder)
			{
				return _materialSearchFolder != null && IsFolderAsset(_materialSearchFolder);
			}

			return _singleOverrideMaterial != null;
		}

		private void DrawResults()
		{
			if (_previewResults.Count == 0)
			{
				return;
			}

			EditorGUILayout.LabelField("Last Results", EditorStyles.boldLabel);

			int matched = _previewResults.Count(r => r.matchedMaterial != null && !r.alreadyMapped);
			int alreadyMapped = _previewResults.Count(r => r.alreadyMapped);
			int missing = _previewResults.Count(r => r.matchedMaterial == null && r.duplicateMatches.Count == 0);
			int duplicates = _previewResults.Count(r => r.duplicateMatches.Count > 1);

			EditorGUILayout.LabelField("Ready To Remap", matched.ToString());
			EditorGUILayout.LabelField("Already Mapped", alreadyMapped.ToString());
			EditorGUILayout.LabelField("Missing Matches", missing.ToString());
			EditorGUILayout.LabelField("Duplicate Matches", duplicates.ToString());

			EditorGUILayout.Space();

			_resultsScroll = EditorGUILayout.BeginScrollView(_resultsScroll, GUILayout.MinHeight(180));

			foreach (MaterialMatch result in _previewResults)
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					EditorGUILayout.LabelField(result.materialName, EditorStyles.boldLabel);
					EditorGUILayout.LabelField("Model", result.modelPath);

					if (result.matchedMaterial != null)
					{
						EditorGUILayout.ObjectField("Matched Material", result.matchedMaterial, typeof(Material), false);
					}

					if (result.duplicateMatches.Count > 1)
					{
						EditorGUILayout.LabelField("Duplicates", result.duplicateMatches.Count.ToString());

						foreach (Material duplicate in result.duplicateMatches)
						{
							EditorGUILayout.ObjectField(duplicate, typeof(Material), false);
						}
					}

					EditorGUILayout.LabelField("Status", result.status);
				}
			}

			EditorGUILayout.EndScrollView();
		}

		private void Run()
		{
			_previewResults.Clear();

			string[] selectedGuids = Selection.assetGUIDs;

			if (selectedGuids == null || selectedGuids.Length == 0)
			{
				EditorUtility.DisplayDialog(
					"No Selection",
					"Select one or more FBX/model assets or folders in the Project window.",
					"OK");
				return;
			}

			List<string> modelPaths = CollectModelPaths(selectedGuids);

			if (modelPaths.Count == 0)
			{
				EditorUtility.DisplayDialog(
					"No Models Found",
					"No supported model assets were found in the selected assets/folders.",
					"OK");
				return;
			}

			Dictionary<string, List<Material>> materialLookup = null;

			if (_remapMode == RemapMode.MatchByNameFromFolder)
			{
				string materialFolderPath = AssetDatabase.GetAssetPath(_materialSearchFolder);

				if (!AssetDatabase.IsValidFolder(materialFolderPath))
				{
					EditorUtility.DisplayDialog(
						"Invalid Material Folder",
						"Please assign a valid Project folder containing your materials.",
						"OK");
					return;
				}

				materialLookup = BuildMaterialLookup(materialFolderPath);
			}

			int changedModelCount = 0;

			try
			{
				if (!_dryRunOnly)
				{
					AssetDatabase.StartAssetEditing();
				}

				for (int i = 0; i < modelPaths.Count; i++)
				{
					string modelPath = modelPaths[i];

					EditorUtility.DisplayProgressBar(
						_dryRunOnly ? "Previewing Material Remaps" : "Applying Material Remaps",
						modelPath,
						(float)i / modelPaths.Count);

					bool changed = ProcessModel(modelPath, materialLookup);

					if (changed)
					{
						changedModelCount++;
					}
				}
			}
			finally
			{
				if (!_dryRunOnly)
				{
					AssetDatabase.StopAssetEditing();
					AssetDatabase.Refresh();
				}

				EditorUtility.ClearProgressBar();
			}

			string mode = _dryRunOnly ? "Preview complete" : "Remap complete";

			EditorUtility.DisplayDialog(
				mode,
				$"Processed {modelPaths.Count} model asset(s).\nChanged {changedModelCount} model asset(s).",
				"OK");
		}

		private bool ProcessModel(string modelPath, Dictionary<string, List<Material>> materialLookup)
		{
			ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;

			if (importer == null)
			{
				return false;
			}

			Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
			Object mainAsset = AssetDatabase.LoadMainAssetAtPath(modelPath);

			bool changed = false;
			Dictionary<AssetImporter.SourceAssetIdentifier, Object> existingMap = importer.GetExternalObjectMap();

			foreach (Object subAsset in subAssets)
			{
				if (subAsset == null)
				{
					continue;
				}

				if (subAsset == mainAsset)
				{
					continue;
				}

				if (subAsset is not Material embeddedMaterial)
				{
					continue;
				}

				string embeddedMaterialName = embeddedMaterial.name;

				if (string.IsNullOrWhiteSpace(embeddedMaterialName))
				{
					continue;
				}

				Material targetMaterial = ResolveTargetMaterial(embeddedMaterialName, materialLookup, out List<Material> duplicates, out string status);

				AssetImporter.SourceAssetIdentifier identifier = new AssetImporter.SourceAssetIdentifier(
					typeof(Material),
					embeddedMaterialName);

				bool alreadyMapped = false;

				if (targetMaterial != null &&
				    existingMap.TryGetValue(identifier, out Object existingTarget) &&
				    existingTarget == targetMaterial)
				{
					alreadyMapped = true;
					status = "Already mapped.";
				}

				_previewResults.Add(new MaterialMatch
				{
					modelPath = modelPath,
					materialName = embeddedMaterialName,
					matchedMaterial = targetMaterial,
					duplicateMatches = duplicates,
					alreadyMapped = alreadyMapped,
					status = status
				});

				if (_dryRunOnly)
				{
					continue;
				}

				if (targetMaterial == null || alreadyMapped)
				{
					continue;
				}

				importer.AddRemap(identifier, targetMaterial);
				changed = true;
			}

			if (changed && !_dryRunOnly)
			{
				importer.SaveAndReimport();
			}

			return changed;
		}

		private Material ResolveTargetMaterial(
			string embeddedMaterialName,
			Dictionary<string, List<Material>> materialLookup,
			out List<Material> duplicates,
			out string status)
		{
			duplicates = new List<Material>();

			if (_remapMode == RemapMode.SingleMaterialOverride)
			{
				status = _singleOverrideMaterial != null
					? "Will remap to override material."
					: "No override material assigned.";

				return _singleOverrideMaterial;
			}

			string lookupKey = GetLookupKey(embeddedMaterialName);

			if (materialLookup == null || !materialLookup.TryGetValue(lookupKey, out List<Material> matches) || matches.Count == 0)
			{
				status = "No matching material found.";
				return null;
			}

			duplicates = matches;

			if (matches.Count > 1)
			{
				if (_duplicateMaterialHandling == DuplicateMaterialHandling.SkipDuplicates)
				{
					status = "Skipped because multiple materials share this name.";
					return null;
				}

				status = "Multiple matches found. Using first match.";
				return matches[0];
			}

			status = "Matched by name.";
			return matches[0];
		}

		private Dictionary<string, List<Material>> BuildMaterialLookup(string materialFolderPath)
		{
			Dictionary<string, List<Material>> lookup = new Dictionary<string, List<Material>>();

			string[] materialGuids = AssetDatabase.FindAssets(
				"t:Material",
				new[] { materialFolderPath });

			foreach (string guid in materialGuids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);

				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}

				Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

				if (material == null)
				{
					continue;
				}

				AddMaterialLookupEntry(lookup, material.name, material);

				string fileName = Path.GetFileNameWithoutExtension(path);
				AddMaterialLookupEntry(lookup, fileName, material);
			}

			return lookup;
		}

		private void AddMaterialLookupEntry(
			Dictionary<string, List<Material>> lookup,
			string materialName,
			Material material)
		{
			if (string.IsNullOrWhiteSpace(materialName) || material == null)
			{
				return;
			}

			string key = GetLookupKey(materialName);

			if (!lookup.TryGetValue(key, out List<Material> materials))
			{
				materials = new List<Material>();
				lookup.Add(key, materials);
			}

			if (!materials.Contains(material))
			{
				materials.Add(material);
			}
		}

		private string GetLookupKey(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return string.Empty;
			}

			string trimmed = name.Trim();

			return _caseInsensitiveMatching
				? trimmed.ToLowerInvariant()
				: trimmed;
		}

		private List<string> CollectModelPaths(string[] selectedGuids)
		{
			HashSet<string> results = new HashSet<string>();

			foreach (string guid in selectedGuids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);

				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}

				if (AssetDatabase.IsValidFolder(path))
				{
					CollectModelPathsFromFolder(path, results);
				}
				else if (IsModelAsset(path))
				{
					results.Add(path);
				}
			}

			return results.OrderBy(path => path).ToList();
		}

		private void CollectModelPathsFromFolder(string folderPath, HashSet<string> results)
		{
			string[] modelGuids = AssetDatabase.FindAssets(
				"t:Model",
				new[] { folderPath });

			foreach (string guid in modelGuids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);

				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}

				if (!_processFoldersRecursively && !IsDirectChildOfFolder(path, folderPath))
				{
					continue;
				}

				if (IsModelAsset(path))
				{
					results.Add(path);
				}
			}
		}

		private static bool IsFolderAsset(DefaultAsset asset)
		{
			if (asset == null)
			{
				return false;
			}

			string path = AssetDatabase.GetAssetPath(asset);
			return AssetDatabase.IsValidFolder(path);
		}

		private static bool IsDirectChildOfFolder(string assetPath, string folderPath)
		{
			string directory = Path.GetDirectoryName(assetPath);

			if (string.IsNullOrWhiteSpace(directory))
			{
				return false;
			}

			directory = directory.Replace("\\", "/");
			folderPath = folderPath.Replace("\\", "/");

			return directory == folderPath;
		}

		private static bool IsModelAsset(string path)
		{
			string extension = Path.GetExtension(path).ToLowerInvariant();

			return extension == ".fbx"
			       || extension == ".obj"
			       || extension == ".dae"
			       || extension == ".3ds"
			       || extension == ".blend";
		}
	}
}