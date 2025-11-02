using System;
using System.Collections; 
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using Debug = UnityEngine.Debug;

namespace PixelWizards.Utilities
{
    public class BatchImportAssetPackagesWindow : EditorWindow
    {
       
        private string packagePath = "";
        private bool includeSubdirectories = false;
        private bool enableLogging = true;

        private Queue<string> importQueue = new Queue<string>();
        private List<FileInfo> previewPackages = new List<FileInfo>();
        private Vector2 previewScrollPos;

        private bool isImporting = false;
        private bool isPaused = false;
        private float timePerMB = 0.1f; // Seconds per MB for estimated duration

        private const string EditorPrefsKey_PackagePath = "BatchImportAssetPackages_LastPath";
        private const string EditorPrefsKey_RecentFolders = "BatchImportAssetPackages_RecentFolders";
        private const string EditorPrefsKey_IncludeSubdirectories = "BatchImportAssetPackages_IncludeSubdirectories";

        private List<string> recentFolders = new List<string>();
        private const int MaxRecentFolders = 5;
        
        private EditorCoroutine importCoroutine;

        [MenuItem("Assets/Batch Import Packages")]
        public static void ShowWindow()
        {
            var window = GetWindow<BatchImportAssetPackagesWindow>("Batch Import Packages");
            window.minSize = new Vector2(500, 320);
        }

        private void OnEnable()
        {
            if (EditorPrefs.HasKey(EditorPrefsKey_PackagePath))
            {
                packagePath = EditorPrefs.GetString(EditorPrefsKey_PackagePath);
            }
            
            if (EditorPrefs.HasKey(EditorPrefsKey_RecentFolders))
            {
                string saved = EditorPrefs.GetString(EditorPrefsKey_RecentFolders);
                recentFolders = saved.Split('|').Distinct().Where(Directory.Exists).ToList();
            }
            
            includeSubdirectories = EditorPrefs.GetBool(EditorPrefsKey_IncludeSubdirectories, false);
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(packagePath))
                EditorPrefs.SetString(EditorPrefsKey_PackagePath, packagePath);
        }

        private void OnGUI()
        {
            GUILayout.Label("Batch Import .unitypackage Files", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (recentFolders.Count > 0)
            {
                GUILayout.BeginHorizontal();
                {

                    GUILayout.Label("Recent Folders:", GUILayout.Width(100));

                    // Dropdown for selecting recent folder
                    int selectedIndex = -1;
                    string[] folderNames = recentFolders.Select(f => Path.GetFileName(f)).ToArray();
                    selectedIndex = EditorGUILayout.Popup(-1, folderNames);

                    if (selectedIndex >= 0 && selectedIndex < recentFolders.Count)
                    {
                        packagePath = recentFolders[selectedIndex];
                        ScanForPackages();
                        GUI.FocusControl(null);
                    }

                    // "Clear" button
                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("Clear Recent Folders",
                                "Are you sure you want to clear the recent folders list?", "Yes", "Cancel"))
                        {
                            recentFolders.Clear();
                            EditorPrefs.DeleteKey(EditorPrefsKey_RecentFolders);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.LabelField("Package Folder Path", EditorStyles.label);
            GUILayout.BeginHorizontal();
            {
                packagePath = EditorGUILayout.TextField(packagePath);
                if (GUILayout.Button("Browse", GUILayout.Width(70)))
                {
                    string selectedPath =
                        EditorUtility.OpenFolderPanel("Select Folder Containing Packages", packagePath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        packagePath = selectedPath;
                        ScanForPackages();
                        GUI.FocusControl(null);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            bool newIncludeSubdirectories = EditorGUILayout.Toggle("Include Subdirectories", includeSubdirectories);
            if (newIncludeSubdirectories != includeSubdirectories)
            {
                includeSubdirectories = newIncludeSubdirectories;
                EditorPrefs.SetBool(EditorPrefsKey_IncludeSubdirectories, includeSubdirectories);
            }

            enableLogging = EditorGUILayout.Toggle("Enable Import Logging", enableLogging);
            EditorGUILayout.Space();

            // Package Preview List
            if (previewPackages.Count > 0)
            {
                GUILayout.Label($"Found {previewPackages.Count} package(s):", EditorStyles.miniBoldLabel);

                float totalSizeMB = previewPackages.Sum(f => f.Length) / (1024f * 1024f);
                float estimatedTime = totalSizeMB * timePerMB;

                EditorGUILayout.HelpBox($"Estimated Total Size: {totalSizeMB:F2} MB\nEstimated Time: {estimatedTime:F1} seconds", MessageType.Info);

                previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, GUILayout.Height(150));
                for (int i = previewPackages.Count - 1; i >= 0; i--)
                {
                    var file = previewPackages[i];
                    GUILayout.BeginHorizontal();
                    {
                        string name = Path.GetFileName(file.FullName);
                        float sizeMB = file.Length / (1024f * 1024f);
                        GUILayout.Label($"{name} ({sizeMB:F2} MB)", GUILayout.MaxWidth(position.width - 180));

                        if (!isImporting && GUILayout.Button("Import This", GUILayout.Width(80)))
                        {
                            ImportSinglePackage(file.FullName);
                        }

                        if (!isImporting && GUILayout.Button("🗑", GUILayout.Width(25)))
                        {
                            if (EditorUtility.DisplayDialog("Remove Package", $"Remove {name} from the list?", "Yes", "Cancel"))
                            {
                                previewPackages.RemoveAt(i);

                                // Also remove from the queue if it was already enqueued
                                string normalizedPath = file.FullName.Replace("\\", "/");
                                importQueue = new Queue<string>(importQueue.Where(p => p != normalizedPath));
                                
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                if (previewPackages.Count == 0)
                {
                    EditorGUILayout.HelpBox("No .unitypackage files found. Try enabling 'Include Subdirectories'.", MessageType.Info);
                }
                if (isImporting)
                {
                    EditorGUILayout.HelpBox($"Remaining in queue: {importQueue.Count}", MessageType.None);
                }
            }

            // Action Buttons
            if (!isImporting)
            {
                GUI.enabled = previewPackages.Count > 0;
                if (GUILayout.Button("Import Packages from Folder", GUILayout.Height(35)))
                {
                    EnqueuePackagesAndStart();
                }
                GUI.enabled = true;
            }

            if (isImporting)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(isPaused ? "Resume" : "Pause", GUILayout.Height(35)))
                {
                    isPaused = !isPaused;
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(35)))
                {
                    CancelImport();
                }

                if (GUILayout.Button("Clear Queue", GUILayout.Height(35)))
                {
                    ClearQueue();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void ScanForPackages()
        {
            previewPackages.Clear();

            if (string.IsNullOrEmpty(packagePath) || !Directory.Exists(packagePath))
            {
                Debug.LogWarning($"Invalid path: {packagePath}");
                return;
            }

            var foundFiles = Directory.GetFiles(packagePath, "*.unitypackage",
                includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            if (foundFiles.Length == 0 && !includeSubdirectories)
            {
                if (EditorUtility.DisplayDialog(
                        "No packages found",
                        "No .unitypackage files were found in the selected folder.\n\nWould you like to include subdirectories and try again?",
                        "Yes", "No"))
                {
                    includeSubdirectories = true;
                    Repaint(); // 🔁 Immediately update the UI toggle
                    EditorPrefs.SetBool(EditorPrefsKey_IncludeSubdirectories, true);
                    ScanForPackages();
                    return;
                }
            }

            Debug.Log($"[BatchImport] Found {foundFiles.Length} unitypackage files in {packagePath}");

            foreach (var path in foundFiles)
            {
                previewPackages.Add(new FileInfo(path));
            }

            // Save recent folder
            if (!recentFolders.Contains(packagePath))
            {
                recentFolders.Insert(0, packagePath);
                if (recentFolders.Count > MaxRecentFolders)
                    recentFolders = recentFolders.Take(MaxRecentFolders).ToList();

                string save = string.Join("|", recentFolders);
                EditorPrefs.SetString(EditorPrefsKey_RecentFolders, save);
            }
        }
        
        private void ImportSinglePackage(string path)
        {
            if (importCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(importCoroutine);

            importQueue.Clear();
            importQueue.Enqueue(path.Replace("\\", "/"));

            isImporting = true;
            isPaused = false;

            importCoroutine = EditorCoroutineUtility.StartCoroutine(ImportPackagesCoroutine(), this);
        }


        private void EnqueuePackagesAndStart()
        {
            importQueue.Clear();
            foreach (var file in previewPackages)
            {
                importQueue.Enqueue(file.FullName.Replace("\\", "/"));
            }

            if (importQueue.Count > 0)
            {
                EditorPrefs.SetString(EditorPrefsKey_PackagePath, packagePath);
                StartImportQueue();
            }
        }
        
        private void ClearQueue()
        {
            if (importCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(importCoroutine);

            importQueue.Clear();
            importCoroutine = null;
            isImporting = false;
            isPaused = false;

            EditorUtility.ClearProgressBar();
            Debug.Log("🧹 Import queue cleared.");
        }

        private void StartImportQueue()
        {
            if (importCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(importCoroutine);

            isImporting = true;
            isPaused = false;

            importCoroutine = EditorCoroutineUtility.StartCoroutine(ImportPackagesCoroutine(), this);
        }
        
        private void CancelImport()
        {
            if (importCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(importCoroutine);

            importQueue.Clear();
            importCoroutine = null;
            isImporting = false;
            isPaused = false;

            EditorUtility.ClearProgressBar();
            Debug.LogWarning("Import cancelled by user.");
        }
        
        private IEnumerator ImportPackagesCoroutine()
        {
            int total = previewPackages.Count;

            while (importQueue.Count > 0)
            {
                while (isPaused) yield return null;

                string currentPath = importQueue.Dequeue();
                string fileName = Path.GetFileName(currentPath);
                float sizeMB = new FileInfo(currentPath).Length / (1024f * 1024f);
                float simulatedDuration = sizeMB * timePerMB;

                float progress = 1f - (importQueue.Count / (float)total);
                EditorUtility.DisplayProgressBar("Importing Packages", $"Importing {fileName} ({sizeMB:F2} MB)...", progress);

                var start = Time.realtimeSinceStartup;

                try
                {
                    AssetDatabase.ImportPackage(currentPath, false);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ Failed to import {fileName}: {ex.Message}");
                }

                var end = Time.realtimeSinceStartup;
                float actualDuration = end - start;

                if (enableLogging)
                {
                    Debug.Log($"✔ Imported: {fileName} ({sizeMB:F2} MB) in {actualDuration:F2}s");
                }

                yield return new EditorWaitForSeconds(Mathf.Max(simulatedDuration, 0.1f));
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("✅ All packages imported.");
            isImporting = false;
            importCoroutine = null;
        }
    }
}
