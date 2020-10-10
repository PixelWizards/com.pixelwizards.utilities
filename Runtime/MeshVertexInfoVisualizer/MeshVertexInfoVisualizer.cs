using UnityEngine;

/// <summary>
/// From here: https://gist.githubusercontent.com/tf1379/a523b3cae997ce17e874d432fb7fa3b5/raw/6ad9b2868671fd7c5098a39238e9d213d214e408/MeshVertexInfoVisualizer.cs
/// </summary>
namespace PixelWizards.Utilities
{
    [ExecuteInEditMode]
	public class MeshVertexInfoVisualizer : MonoBehaviour
	{
		[System.Flags]
		public enum InfoType
		{
			None = 0x00,
			Normal = (0x01 << 1),
			Tangent = (0x01 << 2),
			Binormal = (0x01 << 3)
		}

		static Color NormalColor = Color.magenta;
		static Color TangentColor = Color.green;
		static Color BinormalColor = Color.blue;

		public Mesh mesh;


		public InfoType infoType = InfoType.Normal;

		[Range(0f, 1f)]
		public float scale = 0.05f;

		void OnEnable()
		{
			FetchMesh();
		}

		void OnValidate()
		{
			FetchMesh();
		}

		void FetchMesh()
		{
			if (mesh != null) { return; }

			SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
			if (smr != null)
			{
				mesh = smr.sharedMesh;
			}

			if (mesh != null) { return; }

			MeshFilter filter = GetComponent<MeshFilter>();
			if (filter != null)
			{
				mesh = filter.sharedMesh;
			}
		}

		void OnDrawGizmos()
		{
			if (mesh == null) { return; }

			scale = Mathf.Abs(scale);
			ShowVertexInfo(mesh);
		}

		bool EnableInfoType(InfoType it)
		{
			return (infoType & it) != 0;
		}

		void ShowVertexInfo(Mesh mesh)
		{
			Vector3[] vertices = mesh.vertices;
			Vector3[] normals = (EnableInfoType(InfoType.Normal) || EnableInfoType(InfoType.Binormal)) ? mesh.normals : null;
			Vector4[] tangents = (EnableInfoType(InfoType.Tangent) || EnableInfoType(InfoType.Binormal)) ? mesh.tangents : null;

			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vertex = transform.TransformPoint(vertices[i]);
				Vector3 normal = (normals != null && i < normals.Length) ? transform.TransformDirection(normals[i]) : Vector3.zero;
				Vector4 tangent4 = Vector4.zero;
				Vector3 tangnet3 = Vector3.zero;
				if (tangents != null && i < tangents.Length)
				{
					tangent4 = tangents[i];
					tangnet3 = transform.TransformDirection(tangent4.x, tangent4.y, tangent4.z);
				}
				DrawVertexInfo(vertex, normal, tangnet3, tangent4.w);
			}
		}

		void DrawVertexInfo(Vector3 vertex, Vector3 normal, Vector3 tangnet, float binormalSign)
		{
			if (EnableInfoType(InfoType.Normal))
			{
				Gizmos.color = NormalColor;
				Gizmos.DrawLine(vertex, vertex + normal * scale);
				Gizmos.color = Color.white;
			}

			if (EnableInfoType(InfoType.Tangent))
			{
				Gizmos.color = TangentColor;
				Gizmos.DrawLine(vertex, vertex + tangnet * scale);
				Gizmos.color = Color.white;
			}

			if (EnableInfoType(InfoType.Binormal))
			{
				Gizmos.color = BinormalColor;
				Vector3 binormal = Vector3.Cross(normal, tangnet) * Mathf.Sign(binormalSign);
				Gizmos.DrawLine(vertex, vertex + binormal * scale);
				Gizmos.color = Color.white;
			}
		}
	}
}