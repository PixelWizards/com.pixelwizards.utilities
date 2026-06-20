using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{
    public class AnimationClipBatchSettingsWindow : EditorWindow
    {
        private enum TargetMode
        {
            Selection,
            Folder
        }

        private TargetMode _targetMode = TargetMode.Selection;
        private DefaultAsset _folder;

        private string _clipNameContains = "";

        private bool _setLoopTime = true;
        private bool _loopTime = true;

        private bool _setLoopPose = true;
        private bool _loopPose = true;

        private bool _setBakeRotation = true;
        private bool _bakeRotationIntoPose = true;

        private bool _setBakeY = true;
        private bool _bakeYIntoPose = true;

        private bool _setBakeXZ = true;
        private bool _bakeXZIntoPose = false;

        private bool _setMirror = false;
        private bool _mirror = false;

        private Vector2 _scroll;

        [MenuItem("Tools/Animation/Batch Edit Clip Import Settings")]
        private static void Open()
        {
            GetWindow<AnimationClipBatchSettingsWindow>("Batch Anim Settings");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawTargetSection();
            EditorGUILayout.Space();

            DrawPresetSection();
            EditorGUILayout.Space();

            DrawSettingsSection();
            EditorGUILayout.Space();

            DrawPreviewSection();
            EditorGUILayout.Space();

            DrawApplyButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

            _targetMode = (TargetMode)EditorGUILayout.EnumPopup("Mode", _targetMode);

            if (_targetMode == TargetMode.Folder)
            {
                _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folder, typeof(DefaultAsset), false);
            }

            _clipNameContains = EditorGUILayout.TextField("Clip Name Contains", _clipNameContains);
        }

        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Idle / Stationary"))
            {
                SetCommonPreset(
                    loopTime: true,
                    loopPose: true,
                    bakeRotation: true,
                    bakeY: true,
                    bakeXZ: true,
                    mirror: false);
            }

            if (GUILayout.Button("In-Place Locomotion"))
            {
                SetCommonPreset(
                    loopTime: true,
                    loopPose: true,
                    bakeRotation: true,
                    bakeY: true,
                    bakeXZ: true,
                    mirror: false);
            }

            if (GUILayout.Button("Root Motion Locomotion"))
            {
                SetCommonPreset(
                    loopTime: true,
                    loopPose: true,
                    bakeRotation: false,
                    bakeY: true,
                    bakeXZ: false,
                    mirror: false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            DrawToggleSetting("Loop Time", ref _setLoopTime, ref _loopTime);
            DrawToggleSetting("Loop Pose", ref _setLoopPose, ref _loopPose);

            EditorGUILayout.Space();

            DrawToggleSetting("Bake Rotation Into Pose", ref _setBakeRotation, ref _bakeRotationIntoPose);
            DrawToggleSetting("Bake Y Into Pose", ref _setBakeY, ref _bakeYIntoPose);
            DrawToggleSetting("Bake XZ Into Pose", ref _setBakeXZ, ref _bakeXZIntoPose);

            EditorGUILayout.Space();

            DrawToggleSetting("Mirror", ref _setMirror, ref _mirror);
        }

        private void DrawToggleSetting(string label, ref bool enabled, ref bool value)
        {
            EditorGUILayout.BeginHorizontal();

            enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(18));

            using (new EditorGUI.DisabledScope(!enabled))
            {
                value = EditorGUILayout.Toggle(label, value);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            List<ModelImporter> importers = GetTargetImporters();

            EditorGUILayout.HelpBox($"Will modify {importers.Count} model asset(s).", MessageType.Info);

            int shown = Mathf.Min(importers.Count, 20);

            for (int i = 0; i < shown; i++)
            {
                EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(importers[i]));
            }

            if (importers.Count > shown)
            {
                EditorGUILayout.LabelField($"...and {importers.Count - shown} more.");
            }
        }

        private void DrawApplyButton()
        {
            List<ModelImporter> importers = GetTargetImporters();

            using (new EditorGUI.DisabledScope(importers.Count == 0))
            {
                if (GUILayout.Button("Apply Settings", GUILayout.Height(32)))
                {
                    Apply(importers);
                }
            }
        }

        private void SetCommonPreset(
            bool loopTime,
            bool loopPose,
            bool bakeRotation,
            bool bakeY,
            bool bakeXZ,
            bool mirror)
        {
            _setLoopTime = true;
            _loopTime = loopTime;

            _setLoopPose = true;
            _loopPose = loopPose;

            _setBakeRotation = true;
            _bakeRotationIntoPose = bakeRotation;

            _setBakeY = true;
            _bakeYIntoPose = bakeY;

            _setBakeXZ = true;
            _bakeXZIntoPose = bakeXZ;

            _setMirror = true;
            _mirror = mirror;
        }

        private List<ModelImporter> GetTargetImporters()
        {
            HashSet<ModelImporter> importers = new HashSet<ModelImporter>();

            if (_targetMode == TargetMode.Selection)
            {
                foreach (Object obj in Selection.objects)
                {
                    AddImporterFromObject(obj, importers);
                }
            }
            else
            {
                if (_folder == null)
                    return importers.ToList();

                string folderPath = AssetDatabase.GetAssetPath(_folder);

                if (string.IsNullOrEmpty(folderPath))
                    return importers.ToList();

                string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AddImporterFromPath(path, importers);
                }
            }

            return importers
                .Where(ImporterHasMatchingClip)
                .OrderBy(i => AssetDatabase.GetAssetPath(i))
                .ToList();
        }

        private void AddImporterFromObject(Object obj, HashSet<ModelImporter> importers)
        {
            if (obj == null)
                return;

            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path))
                return;

            AddImporterFromPath(path, importers);
        }

        private void AddImporterFromPath(string path, HashSet<ModelImporter> importers)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer != null)
            {
                importers.Add(importer);
                return;
            }

            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);

            if (mainAsset == null)
                return;

            string mainAssetPath = AssetDatabase.GetAssetPath(mainAsset);
            importer = AssetImporter.GetAtPath(mainAssetPath) as ModelImporter;

            if (importer != null)
                importers.Add(importer);
        }

        private bool ImporterHasMatchingClip(ModelImporter importer)
        {
            if (importer == null)
                return false;

            ModelImporterClipAnimation[] clips = GetClips(importer);

            if (clips == null || clips.Length == 0)
                return false;

            if (string.IsNullOrWhiteSpace(_clipNameContains))
                return true;

            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (clip.name.IndexOf(_clipNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private void Apply(List<ModelImporter> importers)
        {
            int changedAssets = 0;
            int changedClips = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < importers.Count; i++)
                {
                    ModelImporter importer = importers[i];
                    string path = AssetDatabase.GetAssetPath(importer);

                    EditorUtility.DisplayProgressBar(
                        "Batch Animation Settings",
                        path,
                        i / (float)importers.Count);

                    if (ApplySettings(importer, out int clipCount))
                    {
                        importer.SaveAndReimport();
                        changedAssets++;
                        changedClips += clipCount;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[AnimationClipBatchSettingsWindow] Updated {changedClips} clip(s) across {changedAssets} model asset(s).");
        }

        private bool ApplySettings(ModelImporter importer, out int changedClipCount)
        {
            changedClipCount = 0;

            ModelImporterClipAnimation[] clips = GetClips(importer);

            if (clips == null || clips.Length == 0)
                return false;

            bool changed = false;

            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];

                if (!ClipMatchesFilter(clip))
                    continue;

                if (_setLoopTime && clip.loopTime != _loopTime)
                {
                    clip.loopTime = _loopTime;
                    changed = true;
                }
                
                if (_setLoopPose && clip.loopPose != _loopPose)
                {
                    clip.loopPose = _loopPose;
                    changed = true;
                }
                
                if (_setBakeRotation && clip.lockRootRotation != _bakeRotationIntoPose)
                {
                    clip.lockRootRotation = _bakeRotationIntoPose;
                    changed = true;
                }
                
                if (_setBakeY && clip.lockRootHeightY != _bakeYIntoPose)
                {
                    clip.lockRootHeightY = _bakeYIntoPose;
                    changed = true;
                }
                
                if (_setBakeXZ && clip.lockRootPositionXZ != _bakeXZIntoPose)
                {
                    clip.lockRootPositionXZ = _bakeXZIntoPose;
                    changed = true;
                }
                
                if (_setMirror && clip.mirror != _mirror)
                {
                    clip.mirror = _mirror;
                    changed = true;
                }

                clips[i] = clip;
                changedClipCount++;
            }

            if (!changed)
                return false;

            importer.clipAnimations = clips;
            return true;
        }

        private bool ClipMatchesFilter(ModelImporterClipAnimation clip)
        {
            if (string.IsNullOrWhiteSpace(_clipNameContains))
                return true;

            return clip.name.IndexOf(_clipNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private ModelImporterClipAnimation[] GetClips(ModelImporter importer)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations;

            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            return clips;
        }
    }
}