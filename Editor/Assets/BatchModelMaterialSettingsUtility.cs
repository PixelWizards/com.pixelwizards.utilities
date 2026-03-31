using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class BatchModelMaterialSettingsUtility : EditorWindow
{
    [SerializeField] private DefaultAsset[] targetFolders = Array.Empty<DefaultAsset>();
    [SerializeField] private UnityEngine.Object[] targetAssets = Array.Empty<UnityEngine.Object>();

    [SerializeField] private bool includeSubfolders = true;
    [SerializeField] private bool reimportAfterChange = true;
    [SerializeField] private bool onlyFBX = true;

    private Vector2 scroll;

    [MenuItem("Assets/Cleanup/Batch Convert Materials To Embedded + Material Description")]
    public static void ShowWindow()
    {
        var window = GetWindow<BatchModelMaterialSettingsUtility>("Batch Model Materials");
        window.minSize = new Vector2(640f, 420f);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Batch Model Material Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Batch converts model import settings to:\n" +
            "- Use Embedded Materials\n" +
            "- Import via Material Description\n\n" +
            "Use either the target lists below, or select folders / FBX assets in the Project window and use Convert Current Selection.",
            MessageType.Info);

        EditorGUILayout.Space();

        DrawDefaultAssetArray("Target Folders", ref targetFolders);
        EditorGUILayout.Space();
        DrawObjectArray("Target Assets", ref targetAssets);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        includeSubfolders = EditorGUILayout.ToggleLeft("Include Subfolders", includeSubfolders);
        reimportAfterChange = EditorGUILayout.ToggleLeft("Reimport After Change", reimportAfterChange);
        onlyFBX = EditorGUILayout.ToggleLeft("Only FBX Files", onlyFBX);

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Convert Targets", GUILayout.Height(28)))
            {
                ConvertTargets();
            }

            if (GUILayout.Button("Convert Current Selection", GUILayout.Height(28)))
            {
                ConvertCurrentSelection();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Recommended workflow: select one or more folders in the Project window, then click Convert Current Selection.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawDefaultAssetArray(string label, ref DefaultAsset[] array)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        int currentSize = array != null ? array.Length : 0;
        int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", currentSize));

        if (array == null || array.Length != newSize)
        {
            Array.Resize(ref array, newSize);
        }

        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (DefaultAsset)EditorGUILayout.ObjectField(
                $"{label} {i}",
                array[i],
                typeof(DefaultAsset),
                false);
        }
    }

    private void DrawObjectArray(string label, ref UnityEngine.Object[] array)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        int currentSize = array != null ? array.Length : 0;
        int newSize = Mathf.Max(0, EditorGUILayout.IntField("Size", currentSize));

        if (array == null || array.Length != newSize)
        {
            Array.Resize(ref array, newSize);
        }

        for (int i = 0; i < array.Length; i++)
        {
            array[i] = EditorGUILayout.ObjectField(
                $"{label} {i}",
                array[i],
                typeof(UnityEngine.Object),
                false);
        }
    }

    private void ConvertTargets()
    {
        var paths = new HashSet<string>();
        CollectPathsFromObjects(targetFolders, paths);
        CollectPathsFromObjects(targetAssets, paths);

        if (paths.Count == 0)
        {
            Debug.LogWarning("BatchModelMaterialSettingsUtility: No target folders or assets assigned.");
            return;
        }

        ProcessPaths(paths);
    }

    private void ConvertCurrentSelection()
    {
        var paths = new HashSet<string>();
        CollectPathsFromObjects(Selection.objects, paths);

        if (paths.Count == 0)
        {
            Debug.LogWarning("BatchModelMaterialSettingsUtility: No valid assets selected.");
            return;
        }

        ProcessPaths(paths);
    }

    private void CollectPathsFromObjects(UnityEngine.Object[] objects, HashSet<string> results)
    {
        if (objects == null)
            return;

        foreach (var obj in objects)
        {
            if (obj == null)
                continue;

            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { path });

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (onlyFBX && !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!includeSubfolders)
                    {
                        string parentFolder = NormalizePath(System.IO.Path.GetDirectoryName(assetPath));
                        string selectedFolder = NormalizePath(path);

                        if (!string.Equals(parentFolder, selectedFolder, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    results.Add(assetPath);
                }
            }
            else
            {
                if (onlyFBX && !path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                results.Add(path);
            }
        }
    }

    private void ProcessPaths(HashSet<string> paths)
    {
        var report = new BatchReport();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in paths)
            {
                if (TryConvertModel(path, out FileChangeResult result))
                {
                    if (result.Changed)
                        report.Changed.Add(result);
                    else
                        report.Skipped.Add(result);
                }
                else
                {
                    report.Failed.Add(new FileFailureResult
                    {
                        Path = path,
                        Reason = "Not a valid ModelImporter asset."
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"BatchModelMaterialSettingsUtility: Batch process failed.\n{ex}");
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log(report.BuildSummary());
    }

    private bool TryConvertModel(string path, out FileChangeResult result)
    {
        result = new FileChangeResult
        {
            Path = path,
            Changed = false
        };

        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return false;

        var changes = new List<string>();

        if (importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
        {
            changes.Add($"Material Location: {importer.materialLocation} -> {ModelImporterMaterialLocation.InPrefab}");
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            result.Changed = true;
        }

        if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
        {
            changes.Add($"Material Import Mode: {importer.materialImportMode} -> {ModelImporterMaterialImportMode.ImportViaMaterialDescription}");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            result.Changed = true;
        }

        result.ChangeDescriptions = changes;

        if (!result.Changed)
            return true;

        try
        {
            if (reimportAfterChange)
            {
                importer.SaveAndReimport();
            }
            else
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
            }

            return true;
        }
        catch (Exception ex)
        {
            result.Changed = false;
            result.ChangeDescriptions = new List<string>();

            Debug.LogError($"Failed to update importer settings for '{path}'.\n{ex}");
            return false;
        }
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace("\\", "/");
    }

    private sealed class FileChangeResult
    {
        public string Path;
        public bool Changed;
        public List<string> ChangeDescriptions = new();
    }

    private sealed class FileFailureResult
    {
        public string Path;
        public string Reason;
    }

    private sealed class BatchReport
    {
        public readonly List<FileChangeResult> Changed = new();
        public readonly List<FileChangeResult> Skipped = new();
        public readonly List<FileFailureResult> Failed = new();

        public string BuildSummary()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Batch Model Material Conversion Complete");
            sb.AppendLine("--------------------------------------");
            sb.AppendLine($"Changed: {Changed.Count}");
            sb.AppendLine($"Skipped: {Skipped.Count}");
            sb.AppendLine($"Failed:  {Failed.Count}");

            if (Changed.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Changed Files:");
                foreach (var entry in Changed)
                {
                    sb.AppendLine($"- {entry.Path}");
                    for (int i = 0; i < entry.ChangeDescriptions.Count; i++)
                    {
                        sb.AppendLine($"    • {entry.ChangeDescriptions[i]}");
                    }
                }
            }

            if (Skipped.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Skipped Files (already correct):");
                foreach (var entry in Skipped)
                {
                    sb.AppendLine($"- {entry.Path}");
                }
            }

            if (Failed.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Failed Files:");
                foreach (var entry in Failed)
                {
                    sb.AppendLine($"- {entry.Path}");
                    sb.AppendLine($"    • {entry.Reason}");
                }
            }

            return sb.ToString();
        }
    }
}