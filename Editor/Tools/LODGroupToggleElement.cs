using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace TenacityGames.Editor
{
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class LODGroupToggleElement : EditorToolbarToggle
    {
        public const string ID = "LODGroupToggleOverlay/Toggle";
        private const string PrefsKey = "LODGroupToggle_State";

        public LODGroupToggleElement()
        {
            text = "";
            tooltip = "ON: Forces LOD0 and disables LODGroups.\nOFF: Restores normal LOD behavior.";
            icon = EditorGUIUtility.IconContent("LODGroup Icon").image as Texture2D;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

            this.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(PrefsKey, evt.newValue);
                ToggleLODs(evt.newValue);
                LODGroupToggleStatusElement.RefreshAll();
            });
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            bool savedState = EditorPrefs.GetBool(PrefsKey, false);
            SetValueWithoutNotify(savedState);
        }

        private void ToggleLODs(bool forceLOD0)
        {
            LODGroup[] allLODGroups = Object.FindObjectsByType<LODGroup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (LODGroup lodGroup in allLODGroups)
            {
                Undo.RecordObject(lodGroup, "Toggle LODGroup State");

                LOD[] lods = lodGroup.GetLODs();

                if (lods.Length == 0)
                    continue;

                HashSet<Renderer> lod0Renderers = new();

                foreach (Renderer renderer in lods[0].renderers)
                {
                    if (renderer != null)
                        lod0Renderers.Add(renderer);
                }

                for (int i = 0; i < lods.Length; i++)
                {
                    foreach (Renderer renderer in lods[i].renderers)
                    {
                        if (renderer == null)
                            continue;

                        Undo.RecordObject(renderer.gameObject, "Toggle LOD Renderer State");

                        if (forceLOD0)
                        {
                            renderer.gameObject.SetActive(lod0Renderers.Contains(renderer));
                        }
                        else
                        {
                            renderer.gameObject.SetActive(true);
                        }

                        EditorUtility.SetDirty(renderer.gameObject);
                    }
                }

                lodGroup.enabled = !forceLOD0;
                EditorUtility.SetDirty(lodGroup);
                EditorUtility.SetDirty(lodGroup.gameObject);
            }
        }
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    public class LODGroupToggleStatusElement : VisualElement
    {
        public const string ID = "LODGroupToggleOverlay/Status";
        private const string PrefsKey = "LODGroupToggle_State";

        private static readonly List<LODGroupToggleStatusElement> ActiveElements = new();

        private readonly Label statusLabel;

        public LODGroupToggleStatusElement()
        {
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.paddingLeft = 4;
            style.paddingRight = 6;

            statusLabel = new Label();
            statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusLabel.style.minWidth = 88;

            Add(statusLabel);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (!ActiveElements.Contains(this))
                    ActiveElements.Add(this);

                Refresh();
            });

            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                ActiveElements.Remove(this);
            });
        }

        public static void RefreshAll()
        {
            for (int i = ActiveElements.Count - 1; i >= 0; i--)
            {
                if (ActiveElements[i] == null)
                {
                    ActiveElements.RemoveAt(i);
                    continue;
                }

                ActiveElements[i].Refresh();
            }
        }

        private void Refresh()
        {
            bool forceLOD0 = EditorPrefs.GetBool(PrefsKey, false);

            statusLabel.text = forceLOD0
                ? "LODs Disabled"
                : "LODs Enabled";

            tooltip = forceLOD0
                ? "LODGroups are disabled. Only LOD0 renderers are active."
                : "LODGroups are enabled. Normal LOD behavior is active.";
        }
    }

    [Overlay(typeof(SceneView), "LOD Toggle Overlay", true)]
    public class LODGroupToggleOverlay : ToolbarOverlay
    {
        private LODGroupToggleOverlay() : base(
            LODGroupToggleElement.ID,
            LODGroupToggleStatusElement.ID)
        {
        }
    }
}