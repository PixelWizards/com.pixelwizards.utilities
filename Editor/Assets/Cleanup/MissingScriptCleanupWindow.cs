using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelWizards.Utilities
{
    /// <summary>
    /// Batch cleanup tool to remove "Missing (Mono Script)" components from prefabs and/or scenes.
    /// Safe defaults: dry-run first, optional prefab backup, and explicit scope filters.
    /// </summary>
    public sealed class MissingScriptCleanupWindow : EditorWindow
    {
        private enum Scope
        {
            PrefabsInProject,
            PrefabsInFolder,
            OpenScenes,
            ScenesInProject,
            ScenesInFolder,
        }

        [SerializeField] private Scope scope = Scope.PrefabsInProject;

        [SerializeField] private DefaultAsset folder; // optional folder scope
        [SerializeField] private bool includeInactive = true;

        [Header("Safety")]
        [SerializeField] private bool dryRun = true;
        [SerializeField] private bool makePrefabBackups = true;
        [SerializeField] private string backupFolderName = "_MissingScriptBackups";
        [SerializeField] private bool stopAfterFirstError = false;

        [Header("Reporting")]
        [SerializeField] private bool logEachAsset = false;

        private Vector2 _scroll;
        private string _lastReport = "";

        [MenuItem("Assets/Cleanup/Missing Script Cleanup")]
        public static void Open()
        {
            var w = GetWindow<MissingScriptCleanupWindow>();
            w.titleContent = new GUIContent("Missing Script Cleanup");
            w.minSize = new Vector2(520, 420);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Missing Script Cleanup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Removes Missing (Mono Script) components from prefabs and/or scenes.\n" +
                "Recommended flow: Dry Run → review report → disable Dry Run → run again.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            scope = (Scope)EditorGUILayout.EnumPopup("Scope", scope);

            using (new EditorGUI.IndentLevelScope())
            {
                bool needsFolder =
                    scope == Scope.PrefabsInFolder ||
                    scope == Scope.ScenesInFolder;

                using (new EditorGUI.DisabledScope(!needsFolder))
                {
                    folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", folder, typeof(DefaultAsset), false);
                }

                includeInactive = EditorGUILayout.ToggleLeft("Include inactive children", includeInactive);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Safety", EditorStyles.boldLabel);
            dryRun = EditorGUILayout.ToggleLeft("Dry Run (no changes)", dryRun);

            using (new EditorGUI.DisabledScope(dryRun))
            {
                makePrefabBackups = EditorGUILayout.ToggleLeft("Make prefab backups", makePrefabBackups);
                using (new EditorGUI.DisabledScope(!makePrefabBackups))
                {
                    backupFolderName = EditorGUILayout.TextField("Backup folder", backupFolderName);
                }
            }

            stopAfterFirstError = EditorGUILayout.ToggleLeft("Stop after first error", stopAfterFirstError);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Reporting", EditorStyles.boldLabel);
            logEachAsset = EditorGUILayout.ToggleLeft("Log each processed asset", logEachAsset);

            EditorGUILayout.Space(10);

            if (GUILayout.Button(dryRun ? "Run Dry-Run Scan" : "Run Cleanup (Apply Changes)", GUILayout.Height(34)))
            {
                EditorApplication.delayCall += () =>
                {
                    Run();
                };
            }

            if (!CanRun())
            {
                EditorGUILayout.HelpBox("Folder scope selected: please assign a Folder.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Last Report", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_lastReport, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private bool CanRun()
        {
            if (scope == Scope.PrefabsInFolder || scope == Scope.ScenesInFolder)
                return folder != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
            return true;
        }

        private void Run()
        {
            try
            {
                _lastReport = string.Empty;

                var assets = GatherTargets(scope, folder);
                if (assets.Count == 0)
                {
                    _lastReport = "No targets found for the selected scope.";
                    Debug.Log(_lastReport);
                    return;
                }

                int totalTargets = assets.Count;
                int totalMissing = 0;
                int totalObjectsTouched = 0;
                int totalAssetsChanged = 0;
                int totalErrors = 0;

                var reportLines = new List<string>(2048);
                reportLines.Add($"Missing Script Cleanup Report");
                reportLines.Add($"Time: {DateTime.Now}");
                reportLines.Add($"Scope: {scope}");
                reportLines.Add($"DryRun: {dryRun}");
                reportLines.Add($"Targets: {totalTargets}");
                reportLines.Add("");

                // Backup folder (prefabs only)
                string backupRoot = null;
                if (!dryRun && makePrefabBackups && (scope == Scope.PrefabsInProject || scope == Scope.PrefabsInFolder))
                {
                    backupRoot = EnsureBackupFolder(backupFolderName);
                    reportLines.Add($"Prefab backups: ENABLED → {backupRoot}");
                    reportLines.Add("");
                }

                // Make sure we don't accidentally save scenes while iterating unless we mean to.
                AssetDatabase.SaveAssets();

                for (int i = 0; i < assets.Count; i++)
                {
                    string path = assets[i];
                    bool changed = false;

                    try
                    {
                        EditorUtility.DisplayProgressBar(
                            "Missing Script Cleanup",
                            $"{i + 1}/{assets.Count}: {path}",
                            (float)(i + 1) / assets.Count);

                        if (logEachAsset)
                            Debug.Log($"[MissingScriptCleanup] Processing: {path}");

                        if (IsPrefabPath(path))
                        {
                            changed = ProcessPrefab(path, backupRoot, reportLines, ref totalMissing, ref totalObjectsTouched);
                        }
                        else if (IsScenePath(path))
                        {
                            changed = ProcessScene(path, reportLines, ref totalMissing, ref totalObjectsTouched);
                        }

                        if (changed)
                            totalAssetsChanged++;
                    }
                    catch (Exception e)
                    {
                        totalErrors++;
                        reportLines.Add($"ERROR: {path}");
                        reportLines.Add(e.ToString());
                        reportLines.Add("");

                        Debug.LogError($"[MissingScriptCleanup] Error processing '{path}':\n{e}");

                        if (stopAfterFirstError)
                            break;
                    }
                }

                if (!dryRun)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                reportLines.Add("");
                reportLines.Add("Summary");
                reportLines.Add($"  Assets processed: {totalTargets}");
                reportLines.Add($"  Assets changed:   {totalAssetsChanged}");
                reportLines.Add($"  Missing removed:  {totalMissing}");
                reportLines.Add($"  Objects touched:  {totalObjectsTouched}");
                reportLines.Add($"  Errors:           {totalErrors}");

                _lastReport = string.Join("\n", reportLines);
                Repaint();
                Debug.Log(_lastReport);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static List<string> GatherTargets(Scope s, DefaultAsset folderAsset)
        {
            var results = new List<string>(1024);

            string folderPath = null;
            if (folderAsset != null)
                folderPath = AssetDatabase.GetAssetPath(folderAsset);

            switch (s)
            {
                case Scope.PrefabsInProject:
                {
                    foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
                        results.Add(AssetDatabase.GUIDToAssetPath(guid));
                    break;
                }
                case Scope.PrefabsInFolder:
                {
                    foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folderPath }))
                        results.Add(AssetDatabase.GUIDToAssetPath(guid));
                    break;
                }
                case Scope.OpenScenes:
                {
                    // Only scenes currently open in editor
                    int count = EditorSceneManager.sceneCount;
                    for (int i = 0; i < count; i++)
                    {
                        var sc = EditorSceneManager.GetSceneAt(i);
                        if (!sc.IsValid() || string.IsNullOrEmpty(sc.path)) continue;
                        results.Add(sc.path);
                    }
                    break;
                }
                case Scope.ScenesInProject:
                {
                    foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
                        results.Add(AssetDatabase.GUIDToAssetPath(guid));
                    break;
                }
                case Scope.ScenesInFolder:
                {
                    foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { folderPath }))
                        results.Add(AssetDatabase.GUIDToAssetPath(guid));
                    break;
                }
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        private bool ProcessPrefab(
            string prefabPath,
            string backupRoot,
            List<string> reportLines,
            ref int totalMissing,
            ref int totalObjectsTouched)
        {
            // Load contents (isolated editing)
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            bool changed = false;

            try
            {
                int removedOnPrefab = 0;
                int objectsTouched = 0;

                RemoveMissingScriptsRecursive(
                    root,
                    includeInactive,
                    onRemoved: (go, removed) =>
                    {
                        removedOnPrefab += removed;
                        objectsTouched++;
                    });

                if (removedOnPrefab > 0)
                {
                    changed = true;
                    totalMissing += removedOnPrefab;
                    totalObjectsTouched += objectsTouched;

                    reportLines.Add($"{prefabPath}");
                    reportLines.Add($"  Missing removed: {removedOnPrefab}");
                    reportLines.Add($"  Objects touched: {objectsTouched}");
                    reportLines.Add("");

                    if (!dryRun)
                    {
                        if (!string.IsNullOrEmpty(backupRoot))
                            BackupPrefabAsset(prefabPath, backupRoot);

                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return changed;
        }

        private bool ProcessScene(
            string scenePath,
            List<string> reportLines,
            ref int totalMissing,
            ref int totalObjectsTouched)
        {
            // Open scene additively so we can process it safely and then close if not already open.
            var wasOpen = IsSceneAlreadyOpen(scenePath);
            var scene = wasOpen
                ? EditorSceneManager.GetSceneByPath(scenePath)
                : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            if (!scene.IsValid())
                return false;

            bool changed = false;

            try
            {
                int removedInScene = 0;
                int objectsTouched = 0;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (!root) continue;

                    RemoveMissingScriptsRecursive(
                        root,
                        includeInactive,
                        onRemoved: (go, removed) =>
                        {
                            removedInScene += removed;
                            objectsTouched++;
                        });
                }

                if (removedInScene > 0)
                {
                    changed = true;
                    totalMissing += removedInScene;
                    totalObjectsTouched += objectsTouched;

                    reportLines.Add($"{scenePath}");
                    reportLines.Add($"  Missing removed: {removedInScene}");
                    reportLines.Add($"  Objects touched: {objectsTouched}");
                    reportLines.Add("");

                    if (!dryRun)
                        EditorSceneManager.MarkSceneDirty(scene);
                }

                if (!dryRun && scene.isDirty)
                    EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (!wasOpen && scene.IsValid())
                    EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            return changed;
        }

        private static void RemoveMissingScriptsRecursive(
            GameObject root,
            bool includeInactiveChildren,
            Action<GameObject, int> onRemoved)
        {
            if (!root) return;

            // Traverse transforms (includes inactive if requested)
            var transforms = root.GetComponentsInChildren<Transform>(includeInactiveChildren);
            for (int i = 0; i < transforms.Length; i++)
            {
                var t = transforms[i];
                if (!t) continue;

                var go = t.gameObject;
                int removed = RemoveMissingScriptsOnGameObject(go);
                if (removed > 0)
                    onRemoved?.Invoke(go, removed);
            }
        }

        private static int RemoveMissingScriptsOnGameObject(GameObject go)
        {
            if (!go) return 0;

            // UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript exists and is ideal.
            // It returns the number removed.
            Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            return removed;
        }

        private static bool IsPrefabPath(string path)
            => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

        private static bool IsScenePath(string path)
            => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);

        private static bool IsSceneAlreadyOpen(string scenePath)
        {
            int count = EditorSceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                var sc = EditorSceneManager.GetSceneAt(i);
                if (!sc.IsValid() || string.IsNullOrEmpty(sc.path)) continue;
                if (string.Equals(sc.path, scenePath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string EnsureBackupFolder(string folderName)
        {
            // Create under Assets/
            string root = "Assets";
            string full = Path.Combine(root, folderName).Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(root, folderName);

            return full;
        }

        private static void BackupPrefabAsset(string prefabPath, string backupRoot)
        {
            // Put backups in a mirrored folder structure so name collisions don't clobber each other.
            string fileName = Path.GetFileName(prefabPath);
            string dir = Path.GetDirectoryName(prefabPath)?.Replace("\\", "/") ?? "Assets";
            dir = dir.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) ? dir.Substring("Assets".Length).TrimStart('/') : dir;

            string targetDir = (backupRoot + "/" + dir).Replace("//", "/");
            EnsureFolderRecursive(targetDir);

            string dest = (targetDir + "/" + fileName).Replace("//", "/");

            // Overwrite if exists; that’s fine for iterative cleanups.
            AssetDatabase.CopyAsset(prefabPath, dest);
        }

        private static void EnsureFolderRecursive(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            // Build folder hierarchy under Assets
            string normalized = folderPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Backup folder must be under Assets/");

            string[] parts = normalized.Split('/');
            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}