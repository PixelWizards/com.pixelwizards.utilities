using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{
	public class PrefabMaterialSwapWindow : EditorWindow
	{
		private const float PrefabListMaxHeight = 260f;

		private Material _replacementMaterial;
		private bool _includeInactive = true;
		private bool _includeNestedPrefabs = true;
		private bool _dryRun = true;

		private Vector2 _prefabListScroll;

		private readonly List<GameObject> _prefabs = new();

		[MenuItem("Assets/Materials/Prefab Material Swapper")]
		private static void Open()
		{
			GetWindow<PrefabMaterialSwapWindow>("Prefab Material Swapper");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Prefab Material Swapper", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"Select prefab assets or folders in the Project window, then use this tool to replace every material slot on every child Renderer with one shared material.",
				MessageType.Info);

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				_replacementMaterial = (Material)EditorGUILayout.ObjectField(
					"Replacement Material",
					_replacementMaterial,
					typeof(Material),
					false);

				_includeInactive = EditorGUILayout.Toggle("Include Inactive Children", _includeInactive);
				_includeNestedPrefabs = EditorGUILayout.Toggle("Include Nested Prefab Children", _includeNestedPrefabs);
				_dryRun = EditorGUILayout.Toggle("Dry Run", _dryRun);
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Refresh From Selection"))
			{
				RefreshFromSelection();
			}

			EditorGUILayout.LabelField($"Prefab Assets Found: {_prefabs.Count}");

			using (new EditorGUI.DisabledScope(_prefabs.Count == 0 || !_replacementMaterial))
			{
				if (GUILayout.Button(_dryRun ? "Analyze Selected Prefabs" : "Swap Materials On Selected Prefabs"))
				{
					ProcessPrefabs();
				}
			}

			DrawPrefabList();
		}

		private void DrawPrefabList()
		{
			if (_prefabs.Count == 0)
				return;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				float listHeight = Mathf.Min(
					PrefabListMaxHeight,
					_prefabs.Count * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) + 8f);

				_prefabListScroll = EditorGUILayout.BeginScrollView(
					_prefabListScroll,
					GUILayout.Height(listHeight));

				foreach (GameObject prefab in _prefabs)
				{
					EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
				}

				EditorGUILayout.EndScrollView();
			}
		}

		private void RefreshFromSelection()
		{
			_prefabs.Clear();
			_prefabListScroll = Vector2.zero;

			Object[] selectedObjects = Selection.objects;
			HashSet<string> prefabPaths = new();

			foreach (Object selectedObject in selectedObjects)
			{
				string path = AssetDatabase.GetAssetPath(selectedObject);

				if (string.IsNullOrWhiteSpace(path))
					continue;

				if (Directory.Exists(path))
				{
					string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });

					foreach (string guid in guids)
					{
						string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
						AddPrefabPath(prefabPath, prefabPaths);
					}

					continue;
				}

				AddPrefabPath(path, prefabPaths);
			}

			foreach (string prefabPath in prefabPaths)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

				if (prefab)
					_prefabs.Add(prefab);
			}

			Debug.Log($"Prefab material swapper found {_prefabs.Count} prefab asset(s).");
		}

		private static void AddPrefabPath(string path, HashSet<string> prefabPaths)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			if (!path.EndsWith(".prefab"))
				return;

			PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(path));

			if (assetType == PrefabAssetType.Model)
			{
				Debug.LogWarning($"Skipping model prefab because it cannot be saved directly: {path}");
				return;
			}

			prefabPaths.Add(path);
		}

		private void ProcessPrefabs()
		{
			if (!_replacementMaterial)
			{
				Debug.LogError("No replacement material assigned.");
				return;
			}

			int totalPrefabs = 0;
			int totalRenderers = 0;
			int totalMaterialSlots = 0;
			int totalChangedSlots = 0;

			try
			{
				AssetDatabase.StartAssetEditing();

				foreach (GameObject prefabAsset in _prefabs)
				{
					string path = AssetDatabase.GetAssetPath(prefabAsset);

					if (string.IsNullOrWhiteSpace(path))
						continue;

					GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

					try
					{
						PrefabProcessResult result = ProcessPrefabRoot(prefabRoot);

						totalPrefabs++;
						totalRenderers += result.RendererCount;
						totalMaterialSlots += result.MaterialSlotCount;
						totalChangedSlots += result.ChangedSlotCount;

						if (!_dryRun && result.ChangedSlotCount > 0)
						{
							PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
						}

						Debug.Log(
							$"{(_dryRun ? "Analyzed" : "Processed")} prefab: {path}\n" +
							$"Renderers: {result.RendererCount}, Material Slots: {result.MaterialSlotCount}, Changed Slots: {result.ChangedSlotCount}");
					}
					finally
					{
						PrefabUtility.UnloadPrefabContents(prefabRoot);
					}
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();

				if (!_dryRun)
				{
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
			}

			Debug.Log(
				$"Infestation material swap complete.\n" +
				$"Dry Run: {_dryRun}\n" +
				$"Prefabs: {totalPrefabs}\n" +
				$"Renderers: {totalRenderers}\n" +
				$"Material Slots: {totalMaterialSlots}\n" +
				$"Changed Slots: {totalChangedSlots}\n" +
				$"Replacement Material: {_replacementMaterial.name}");
		}

		private PrefabProcessResult ProcessPrefabRoot(GameObject prefabRoot)
		{
			PrefabProcessResult result = new();

			Renderer[] renderers = prefabRoot.GetComponentsInChildren<Renderer>(_includeInactive);

			foreach (Renderer renderer in renderers)
			{
				if (!renderer)
					continue;

				if (!_includeNestedPrefabs && IsPartOfNestedPrefabInstance(renderer.gameObject, prefabRoot))
					continue;

				Material[] materials = renderer.sharedMaterials;

				if (materials == null || materials.Length == 0)
					continue;

				result.RendererCount++;
				result.MaterialSlotCount += materials.Length;

				bool changed = false;

				for (int i = 0; i < materials.Length; i++)
				{
					if (materials[i] == _replacementMaterial)
						continue;

					materials[i] = _replacementMaterial;
					result.ChangedSlotCount++;
					changed = true;
				}

				if (!_dryRun && changed)
				{
					renderer.sharedMaterials = materials;
					EditorUtility.SetDirty(renderer);
				}
			}

			return result;
		}

		private static bool IsPartOfNestedPrefabInstance(GameObject gameObject, GameObject prefabRoot)
		{
			if (!gameObject || !prefabRoot)
				return false;

			GameObject nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gameObject);

			return nearestPrefabRoot && nearestPrefabRoot != prefabRoot;
		}

		private class PrefabProcessResult
		{
			public int RendererCount;
			public int MaterialSlotCount;
			public int ChangedSlotCount;
		}
	}
}
