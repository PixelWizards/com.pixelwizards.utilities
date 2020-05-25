using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{
	public class EditorNote : MonoBehaviour
	{
		[TextArea]
		public string m_Text;
		public Vector3 m_Offset;
		static GUIStyle m_TextStyle;
		GUIContent m_GUIContent;
		Vector2 m_TextSize;

		private void Awake()
		{
			// if we aren't the editor, just remove this
			if (!Application.isEditor)
				Destroy(this);
		}

		[MenuItem("GameObject/Scene Note")]
		static void AddEditorNote()
		{
			var gameObject = new GameObject("Scene Note");
			if (Selection.activeGameObject != null)
			{
				gameObject.transform.parent = Selection.activeGameObject.transform;
				gameObject.transform.localPosition = GetObjectOffset(Selection.activeGameObject);
			}
			var note = gameObject.AddComponent<EditorNote>();
			note.m_Text = "New Scene Note";
			note.OnValidate();
			Selection.activeGameObject = gameObject;
		}

		static Vector3 GetObjectOffset(GameObject gameObject)
		{
			var meshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
            var collider = gameObject.GetComponentInChildren<Collider>();
			if (collider != null)
			{
				return Vector3.up * collider.bounds.size.y;
			}
			if (meshRenderer != null)
			{
				return Vector3.up * meshRenderer.bounds.size.y * 2.0f * gameObject.transform.localScale.y;
			}
			return Vector3.zero;
		}

		public void OnValidate()
		{
			if (m_TextStyle == null)
			{
				m_TextStyle = new GUIStyle();
				m_TextStyle.fontSize = 16;
				m_TextStyle.normal.textColor = Color.white;
				m_TextStyle.padding.left = m_TextStyle.padding.top = m_TextStyle.padding.bottom = 4;
				m_TextStyle.padding.left = m_TextStyle.padding.right = 8;
				m_TextStyle.normal.background = Texture2D.whiteTexture;
			}
			m_GUIContent = new GUIContent(m_Text);
			m_TextSize = m_TextStyle.CalcSize(m_GUIContent);
			m_Offset = GetObjectOffset(gameObject);
		}

		private void OnDrawGizmosSelected()
		{
			var pos = transform.position + m_Offset;
			var pos2D = HandleUtility.WorldToGUIPoint(pos);
			var rect = new Rect(pos2D, m_TextSize);
			Handles.BeginGUI();
			GUI.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.8f);
			GUI.Box(rect, m_Text, m_TextStyle);
			Handles.EndGUI();
		}
	}
}