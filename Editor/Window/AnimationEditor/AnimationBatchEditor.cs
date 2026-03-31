#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class AnimationBatchEditor : EditorWindow
{
    private AnimationImportBatchPreset _preset;
    private readonly List<UnityEngine.Object> _targets = new List<UnityEngine.Object>();
    private Vector2 _scroll;
    private bool _dryRun;

    [MenuItem("Window/Animation/Animation Batch Editor")]
    public static void Open() => GetWindow<AnimationBatchEditor>("Animation Batch Editor");

    private void OnGUI()
    {
        EditorGUILayout.Space();

        _preset = (AnimationImportBatchPreset)EditorGUILayout.ObjectField(
            "Preset", _preset, typeof(AnimationImportBatchPreset), false);

        _dryRun = EditorGUILayout.ToggleLeft("Dry Run (log changes only, no reimport)", _dryRun);

        EditorGUILayout.Space();
        DrawDropArea();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selection"))
                AddFromSelection();

            if (GUILayout.Button("Clear List"))
                _targets.Clear();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("FBX Targets", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        for (int i = 0; i < _targets.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _targets[i] = EditorGUILayout.ObjectField(_targets[i], typeof(UnityEngine.Object), false);
                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _targets.RemoveAt(i);
                    i--;
                }
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_preset == null || _targets.Count == 0))
        {
            if (GUILayout.Button(_dryRun ? "Preview Changes (Dry Run)" : "Apply + Reimport"))
                ApplyToAll();
        }

        if (_preset == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a preset ScriptableObject to control the checkbox settings.",
                MessageType.Info);
        }
    }

    private void DrawDropArea()
    {
        var rect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(rect, "Drag & Drop FBX files or folders here", EditorStyles.helpBox);

        var evt = Event.current;
        if (!rect.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                    AddObject(obj);
            }

            evt.Use();
        }
    }

    private void AddFromSelection()
    {
        foreach (var obj in Selection.objects)
            AddObject(obj);
    }

    private void AddObject(UnityEngine.Object obj)
    {
        if (obj == null) return;

        var path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;

        if (AssetDatabase.IsValidFolder(path))
        {
            // Add all FBXs in folder (recursive)
            var guids = AssetDatabase.FindAssets("t:Model", new[] { path });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (!p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                if (asset != null && !_targets.Contains(asset))
                    _targets.Add(asset);
            }
        }
        else
        {
            if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                if (!_targets.Contains(obj))
                    _targets.Add(obj);
            }
        }
    }

    private void ApplyToAll()
    {
        var paths = _targets
            .Where(t => t != null)
            .Select(AssetDatabase.GetAssetPath)
            .Where(p => !string.IsNullOrEmpty(p) && p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        if (paths.Count == 0) return;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                if (EditorUtility.DisplayCancelableProgressBar("FBX Batch Apply", path, (float)i / paths.Count))
                    break;

                ApplyToOne(path, _preset, _dryRun);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }

    private static void ApplyToOne(string assetPath, AnimationImportBatchPreset preset, bool dryRun)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (!importer) return;

        if (preset.forceImportAnimationOn)
            importer.importAnimation = true;

        // Pull authored clips (or default)
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        bool changedAny = false;

        // Precompute file base (used for renaming)
        var fileBase = Path.GetFileNameWithoutExtension(assetPath);

        if (dryRun)
        {
            Debug.Log($"[FBX Batch Apply] Inspecting: {assetPath} :: clips={clips.Length} :: fileBase='{fileBase}'");
            for (int i = 0; i < clips.Length; i++)
                Debug.Log($"[FBX Batch Apply]   Clip[{i}] name='{clips[i].name}' loopTime={clips[i].loopTime} loopPose={clips[i].loopPose} bakeY={clips[i].lockRootHeightY}");
        }
        
        for (int i = 0; i < clips.Length; i++)
        {
            var c = clips[i];
            var original = c;

            // Decide loop overrides by name (use ORIGINAL name before rename for rule matching)
            bool nameAlwaysLoop = preset.MatchesAnyToken(c.name, preset.alwaysLoopNameTokens);
            bool nameNeverLoop = preset.MatchesAnyToken(c.name, preset.neverLoopNameTokens);

            // ------------------------
            // Bulk apply (checkboxes)
            // ------------------------
            if (!preset.overridesOnly)
            {
                if (preset.setLoopTime) c.loopTime = preset.loopTime;
                if (preset.setLoopPose) c.loopPose = preset.loopPose;

                if (preset.setBakeIntoPoseY)
                {
                    // "Root Transform Position (Y) -> Bake Into Pose"
                    c.lockRootHeightY = preset.bakeIntoPoseY;
                }

                if (preset.setRootRotation)
                {
                    c.keepOriginalOrientation =
                        (preset.rootRotation == AnimationImportBatchPreset.RootTransformRotation.Original);
                }

                if (preset.setRootPositionXZ)
                {
                    c.keepOriginalPositionXZ =
                        (preset.rootPositionXZ == AnimationImportBatchPreset.RootTransformPositionXZ.Original);
                }
            }

            // ------------------------
            // Renaming (single-clip default)
            // NOTE: Keep ONE renaming path. If you still have older rename fields,
            // remove them from the preset to avoid confusion.
            // ------------------------
            // Renaming (single-clip default)
            bool renamedThisClip = false;
            var beforeName = c.name;

            if (preset.renameSingleClipToFileName && clips.Length == 1)
            {
                var desired = fileBase;
                if (!string.IsNullOrWhiteSpace(desired) && c.name != desired)
                {
                    c.name = desired;
                    renamedThisClip = true;
                }
            }
            else if (preset.renameMultiClipToo && clips.Length > 1 &&
                     preset.multiClipRenameMode != AnimationImportBatchPreset.MultiClipRenameMode.KeepOriginal)
            {
                string candidate = preset.multiClipRenameMode switch
                {
                    AnimationImportBatchPreset.MultiClipRenameMode.PrefixWithFileName => $"{fileBase}_{c.name}",
                    AnimationImportBatchPreset.MultiClipRenameMode.FileNamePlusIndex => $"{fileBase}_{i:00}",
                    _ => c.name
                };

                candidate = MakeUnique(candidate, clips, i);
                if (!string.IsNullOrWhiteSpace(candidate) && c.name != candidate)
                {
                    c.name = candidate;
                    renamedThisClip = true;
                }
            }
            
            // If we renamed, we MUST mark as changed (even if ClipEquals is wrong / stale compile)
            if (renamedThisClip)
            {
                clips[i] = c;     // ensure element reassign
                changedAny = true;

                if (dryRun)
                    Debug.Log($"[FBX Batch Apply] {assetPath} :: Clip rename '{beforeName}' -> '{c.name}'");
            }

            // ------------------------
            // Name overrides (after bulk)
            // ------------------------
            if (nameAlwaysLoop)
            {
                c.loopTime = true;
                c.loopPose = true;
            }

            if (nameNeverLoop)
            {
                c.loopTime = false;
                c.loopPose = false;
            }

            // Detect changes (INCLUDES NAME!)
            if (!renamedThisClip && !ClipEquals(original, c))
            {
                clips[i] = c;
                changedAny = true;

                if (dryRun)
                    LogClipDiff(assetPath, original, c);
            }
        }

        if (changedAny)
        {
            importer.clipAnimations = clips; // reassign array (important)

            if (dryRun)
            {
                Debug.Log($"[FBX Batch Apply] Would apply preset '{preset.name}' to: {assetPath}");
            }
            else
            {
                importer.SaveAndReimport();
                Debug.Log($"[FBX Batch Apply] Applied preset '{preset.name}' to: {assetPath}");
            }
        }
        else
        {
            if (dryRun)
                Debug.Log($"[FBX Batch Apply] No changes needed: {assetPath}");
        }
    }

    private static bool ClipEquals(ModelImporterClipAnimation a, ModelImporterClipAnimation b)
    {
        // Include name so rename-only changes are detected
        if (a.name != b.name) return false;

        // Compare only fields we modify (keep minimal to avoid version quirks)
        if (a.loopTime != b.loopTime) return false;
        if (a.loopPose != b.loopPose) return false;
        if (a.lockRootHeightY != b.lockRootHeightY) return false;
        if (a.keepOriginalOrientation != b.keepOriginalOrientation) return false;
        if (a.keepOriginalPositionXZ != b.keepOriginalPositionXZ) return false;

        return true;
    }

    private static void LogClipDiff(string assetPath, ModelImporterClipAnimation from, ModelImporterClipAnimation to)
    {
        // Keep logs readable: only show what changed.
        if (from.name != to.name)
            Debug.Log($"[FBX Batch Apply] {assetPath} :: Clip rename '{from.name}' -> '{to.name}'");

        if (from.loopTime != to.loopTime)
            Debug.Log($"[FBX Batch Apply] {assetPath} :: '{to.name}' loopTime {from.loopTime} -> {to.loopTime}");

        if (from.loopPose != to.loopPose)
            Debug.Log($"[FBX Batch Apply] {assetPath} :: '{to.name}' loopPose {from.loopPose} -> {to.loopPose}");

        if (from.lockRootHeightY != to.lockRootHeightY)
            Debug.Log($"[FBX Batch Apply] {assetPath} :: '{to.name}' bakeIntoPoseY {from.lockRootHeightY} -> {to.lockRootHeightY}");

        if (from.keepOriginalOrientation != to.keepOriginalOrientation)
            Debug.Log($"[FBX Batch Apply] {assetPath} :: '{to.name}' keepOriginalOrientation {from.keepOriginalOrientation} -> {to.keepOriginalOrientation}");

        if (from.keepOriginalPositionXZ != to.keepOriginalPositionXZ)
            Debug.Log($"[FBX Batch Apply] {assetPath} :: '{to.name}' keepOriginalPositionXZ {from.keepOriginalPositionXZ} -> {to.keepOriginalPositionXZ}");
    }

    private static string MakeUnique(string candidate, ModelImporterClipAnimation[] allClips, int selfIndex)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return candidate;

        var used = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < allClips.Length; i++)
        {
            if (i == selfIndex) continue;
            used.Add(allClips[i].name);
        }

        if (!used.Contains(candidate))
            return candidate;

        int k = 1;
        string test;
        do
        {
            test = $"{candidate}_{k:00}";
            k++;
        } while (used.Contains(test));

        return test;
    }
}
#endif
