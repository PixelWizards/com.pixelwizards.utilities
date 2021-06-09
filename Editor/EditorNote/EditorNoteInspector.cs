using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{

    [CustomEditor(typeof(EditorNote))]
    public class EditorNoteInspector : Editor
    {
        private static EditorNote note;

        private void OnEnable()
        {
            note = (EditorNote)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.Label("Editor Note", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            note.m_Text = GUILayout.TextArea(note.m_Text);

            EditorGUILayout.Space();
        }
    }
}