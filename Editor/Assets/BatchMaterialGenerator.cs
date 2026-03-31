using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelWizards.Utilities
{
    public static class BatchMaterialGenerator
    {
        [MenuItem("Tools/Art/Generate URP Materials Next To Textures (Selected Folder)")]
        public static void GenerateMaterialsNextToTextures()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                Debug.LogError("Select a folder in the Project window.");
                return;
            }

            var rootPath = AssetDatabase.GetAssetPath(selected);
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                Debug.LogError("Selection is not a folder. Select a folder in the Project window.");
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("Could not find URP/Lit shader. Is URP installed and active?");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { rootPath });

            int created = 0;
            int updated = 0;
            int skipped = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    var texPath = AssetDatabase.GUIDToAssetPath(guid);
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    if (tex == null)
                        continue;

                    // Optional: skip non-albedo by simple name hints (delete if you don't want it).
                    // Kenney packs are often albedo-only, but just in case:
                    var n = tex.name.ToLowerInvariant();
                    if (n.Contains("normal") || n.EndsWith("_n") || n.EndsWith("_norm"))
                    {
                        skipped++;
                        continue;
                    }

                    // Material lives beside the texture.
                    var texDir = Path.GetDirectoryName(texPath)?.Replace("\\", "/") ?? "Assets";
                    var matPath = $"{texDir}/{tex.name}.mat";

                    var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (existing != null)
                    {
                        // Update in-place.
                        if (existing.shader != shader)
                            existing.shader = shader;

                        existing.SetTexture("_BaseMap", tex);
                        EditorUtility.SetDirty(existing);

                        updated++;
                        continue;
                    }

                    var mat = new Material(shader)
                    {
                        name = tex.name
                    };
                    mat.SetTexture("_BaseMap", tex);

                    AssetDatabase.CreateAsset(mat, matPath);
                    created++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"Done. Created: {created}, Updated: {updated}, Skipped: {skipped}");
        }
    }
}