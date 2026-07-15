using UnityEditor;
using UnityEngine;

public class TextureSettingsConfigurator : EditorWindow
{
    private DefaultAsset targetFolder;

    private bool setStreamingMipMaps = true;
    private bool streamingMipMaps = true;

    private bool setGenerateMipMaps = true;
    private bool generateMipMaps = true;

    private bool setMaxTextureSize = true;
    private int maxTextureSize = 2048;

    private bool setCompression = true;
    private TextureImporterCompression compression = TextureImporterCompression.Compressed;

    private bool setCrunchCompression = false;
    private bool useCrunchCompression = false;

    private bool includeSubfolders = true;

    [MenuItem("Tools/Optimization/Configure Texture Settings")]
    private static void Open()
    {
        GetWindow<TextureSettingsConfigurator>("Texture Settings");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Configure Texture Settings", EditorStyles.boldLabel);

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Target Folder",
            targetFolder,
            typeof(DefaultAsset),
            false);

        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);

        EditorGUILayout.Space();

        setStreamingMipMaps = EditorGUILayout.ToggleLeft("Set Streaming Mip Maps", setStreamingMipMaps);
        using (new EditorGUI.DisabledScope(!setStreamingMipMaps))
        {
            streamingMipMaps = EditorGUILayout.Toggle("Streaming Mip Maps", streamingMipMaps);
        }

        setGenerateMipMaps = EditorGUILayout.ToggleLeft("Set Generate Mip Maps", setGenerateMipMaps);
        using (new EditorGUI.DisabledScope(!setGenerateMipMaps))
        {
            generateMipMaps = EditorGUILayout.Toggle("Generate Mip Maps", generateMipMaps);
        }

        setMaxTextureSize = EditorGUILayout.ToggleLeft("Set Max Size", setMaxTextureSize);
        using (new EditorGUI.DisabledScope(!setMaxTextureSize))
        {
            maxTextureSize = EditorGUILayout.IntPopup(
                "Max Size",
                maxTextureSize,
                new[] { "256", "512", "1024", "2048", "4096", "8192" },
                new[] { 256, 512, 1024, 2048, 4096, 8192 });
        }

        setCompression = EditorGUILayout.ToggleLeft("Set Compression", setCompression);
        using (new EditorGUI.DisabledScope(!setCompression))
        {
            compression = (TextureImporterCompression)EditorGUILayout.EnumPopup(
                "Compression",
                compression);
        }

        setCrunchCompression = EditorGUILayout.ToggleLeft("Set Crunch Compression", setCrunchCompression);
        using (new EditorGUI.DisabledScope(!setCrunchCompression))
        {
            useCrunchCompression = EditorGUILayout.Toggle("Use Crunch Compression", useCrunchCompression);
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(targetFolder == null))
        {
            if (GUILayout.Button("Apply To Textures"))
            {
                Apply();
            }
        }
    }

    private void Apply()
    {
        string folderPath = AssetDatabase.GetAssetPath(targetFolder);

        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError("Selected object is not a valid folder.");
            return;
        }

        string[] searchFolders = includeSubfolders
            ? new[] { folderPath }
            : GetImmediateChildSearchFolderOnly(folderPath);

        string[] guids = AssetDatabase.FindAssets("t:Texture", searchFolders);

        int checkedCount = 0;
        int changedCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!includeSubfolders && !IsDirectChildOfFolder(assetPath, folderPath))
                    continue;

                checkedCount++;

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer == null)
                    continue;

                bool changed = false;

                if (setStreamingMipMaps && importer.streamingMipmaps != streamingMipMaps)
                {
                    importer.streamingMipmaps = streamingMipMaps;
                    changed = true;
                }

                if (setGenerateMipMaps && importer.mipmapEnabled != generateMipMaps)
                {
                    importer.mipmapEnabled = generateMipMaps;
                    changed = true;
                }

                if (setMaxTextureSize && importer.maxTextureSize != maxTextureSize)
                {
                    importer.maxTextureSize = maxTextureSize;
                    changed = true;
                }

                if (setCompression && importer.textureCompression != compression)
                {
                    importer.textureCompression = compression;
                    changed = true;
                }

                if (setCrunchCompression && importer.crunchedCompression != useCrunchCompression)
                {
                    importer.crunchedCompression = useCrunchCompression;
                    changed = true;
                }

                if (!changed)
                    continue;

                importer.SaveAndReimport();
                changedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"Configured {changedCount} texture(s). Checked {checkedCount} texture(s) in {folderPath}.");
    }

    private static string[] GetImmediateChildSearchFolderOnly(string folderPath)
    {
        return new[] { folderPath };
    }

    private static bool IsDirectChildOfFolder(string assetPath, string folderPath)
    {
        string directory = System.IO.Path.GetDirectoryName(assetPath);
        directory = directory?.Replace("\\", "/");

        return directory == folderPath;
    }
}