using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Animation/Animation Import Preset", fileName = "BatchAnimPreset")]
public sealed class AnimationImportBatchPreset : ScriptableObject
{
    [Header("Apply Scope")]
    public bool applyToAllClips = true;

    [Header("Bulk Clip Settings")]
    public bool setLoopTime = true;
    public bool loopTime = true;

    public bool setLoopPose = true;
    public bool loopPose = true;

    [Tooltip("Equivalent to 'Root Transform Position (Y) -> Bake Into Pose' in the Animation tab.")]
    public bool setBakeIntoPoseY = true;
    public bool bakeIntoPoseY = true;

    [Header("Root Transform (common companions)")]
    [Tooltip("Some teams prefer forcing these for consistency. Leave disabled if you only want Bake Y.")]
    public bool setRootRotation = false;
    public RootTransformRotation rootRotation = RootTransformRotation.Original;

    public bool setRootPositionXZ = false;
    public RootTransformPositionXZ rootPositionXZ = RootTransformPositionXZ.Original;

    public enum RootTransformRotation { Original, BodyOrientation }
    public enum RootTransformPositionXZ { Original, CenterOfMass }

    [Header("Name Overrides (optional)")]
    [Tooltip("If a clip name contains ANY of these tokens (case-insensitive), it will be forced to NOT loop.")]
    public List<string> neverLoopNameTokens = new List<string> { "attack", "shoot", "hit", "death", "jump" };

    [Tooltip("If a clip name contains ANY of these tokens (case-insensitive), it will be forced to loop.")]
    public List<string> alwaysLoopNameTokens = new List<string> { "idle", "walk", "run", "strafe" };

    [Header("Advanced")]
    [Tooltip("If enabled, will set importer.importAnimation = true.")]
    public bool forceImportAnimationOn = true;

    [Tooltip("If enabled, applies settings only if the token rules decide. If disabled, applies bulk first then overrides.")]
    public bool overridesOnly = false;

    [Header("Renaming")]
    [Tooltip("If enabled and the FBX contains exactly one imported clip, rename that clip to the FBX file name.")]
    public bool renameSingleClipToFileName = true;

    [Tooltip("If enabled, multi-clip FBXs will be renamed too (recommended OFF for your workflow).")]
    public bool renameMultiClipToo = false;

    public enum MultiClipRenameMode
    {
        KeepOriginal,
        PrefixWithFileName,     // FileName_Original
        FileNamePlusIndex       // FileName_00, FileName_01...
    }

    public MultiClipRenameMode multiClipRenameMode = MultiClipRenameMode.KeepOriginal;

    public bool MatchesAnyToken(string clipName, List<string> tokens)
    {
        if (string.IsNullOrWhiteSpace(clipName) || tokens == null) return false;
        var lower = clipName.ToLowerInvariant();
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (string.IsNullOrWhiteSpace(t)) continue;
            if (lower.Contains(t.ToLowerInvariant()))
                return true;
        }
        return false;
    }
}
