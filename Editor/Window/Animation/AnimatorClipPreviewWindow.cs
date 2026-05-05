using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class AnimatorClipPreviewWindow : EditorWindow
{
    private Animator animator;
    private AnimationClip selectedClip;
    private readonly List<AnimationClip> clips = new();

    private float normalizedTime;
    private Vector2 scroll;

    [MenuItem("Window/Animation/Animator Clip Preview")]
    public static void Open()
    {
        GetWindow<AnimatorClipPreviewWindow>("Animator Preview");
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            var selectedAnimator = Selection.activeGameObject.GetComponentInChildren<Animator>();
            if (selectedAnimator != null)
            {
                animator = selectedAnimator;
                RefreshClips();
                Repaint();
            }
        }
    }

    private void OnDisable()
    {
        StopPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        animator = (Animator)EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);
        if (EditorGUI.EndChangeCheck())
            RefreshClips();

        if (animator == null)
        {
            EditorGUILayout.HelpBox("Select or assign a GameObject with an Animator.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Refresh Clips"))
            RefreshClips();

        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var clip in clips)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isSelected = clip == selectedClip;

                if (GUILayout.Toggle(isSelected, clip.name, "Button"))
                {
                    if (selectedClip != clip)
                    {
                        selectedClip = clip;
                        normalizedTime = 0f;
                        Sample();
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();

        if (selectedClip == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Clip", selectedClip.name);
        EditorGUILayout.LabelField("Length", $"{selectedClip.length:0.00}s");

        EditorGUI.BeginChangeCheck();
        normalizedTime = EditorGUILayout.Slider("Time", normalizedTime, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
            Sample();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Start Preview"))
                Sample();

            if (GUILayout.Button("Stop Preview"))
                StopPreview();
        }
    }

    private void RefreshClips()
    {
        clips.Clear();
        selectedClip = null;
        normalizedTime = 0f;

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        var controller = animator.runtimeAnimatorController;

        foreach (var clip in controller.animationClips)
        {
            if (clip != null && !clips.Contains(clip))
                clips.Add(clip);
        }

        clips.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
    }

    private void Sample()
    {
        if (animator == null || selectedClip == null)
            return;

        if (!AnimationMode.InAnimationMode())
            AnimationMode.StartAnimationMode();

        float time = Mathf.Clamp01(normalizedTime) * selectedClip.length;

        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(animator.gameObject, selectedClip, time);
        AnimationMode.EndSampling();

        SceneView.RepaintAll();
    }

    private void StopPreview()
    {
        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();

        SceneView.RepaintAll();
    }
}