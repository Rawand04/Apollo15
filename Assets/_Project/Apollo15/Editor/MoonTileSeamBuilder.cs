using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpaceSeries.Core.Environment;

namespace SpaceSeries.Apollo15.Editor
{
    public static class MoonTileSeamBuilder
    {
        public const string SourcePath = "Assets/_Project/Apollo15/Prefabs/Vehicles/MoonTile.fbx";
        public const string MeshPath = "Assets/_Project/Apollo15/Meshes/MoonTileSeamCorrected.asset";
        public const string PrefabPath = "Assets/_Project/Apollo15/Prefabs/Environment/MoonTile.prefab";
        private const string MaterialPath = "Assets/_Project/Apollo15/Materials/MoonRegolith.mat";
        private const int Resolution = 65;
        private const float Size = 20f;
        private const float BorderWidth = 3f;

        [MenuItem("Apollo15/Terrain/Create Seam-Corrected MoonTile")]
        public static void CreateTile()
        {
            if (File.Exists(MeshPath) || File.Exists(PrefabPath))
                throw new System.InvalidOperationException("Derived tile already exists. Preserve existing assets; choose a new output path to rebuild.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var filter = source.GetComponentInChildren<MeshFilter>();
            var sourceMesh = filter.sharedMesh;
            var original = sourceMesh.vertices;
            var triangles = sourceMesh.triangles;
            var transformed = new Vector3[original.Length];
            Vector3 min = Vector3.one * float.PositiveInfinity, max = Vector3.one * float.NegativeInfinity;
            for (int i = 0; i < original.Length; i++)
            {
                transformed[i] = filter.transform.TransformPoint(original[i]);
                min = Vector3.Min(min, transformed[i]); max = Vector3.Max(max, transformed[i]);
            }
            float heightScale = Size / (max.x - min.x);
            var vertices = new Vector3[Resolution * Resolution];
            var uv = new Vector2[vertices.Length];
            int sampled = 0;
            for (int z = 0; z < Resolution; z++) for (int x = 0; x < Resolution; x++)
            {
                float u = x / (float)(Resolution - 1), v = z / (float)(Resolution - 1);
                Vector2 point = new Vector2(Mathf.Lerp(min.x, max.x, u), Mathf.Lerp(min.z, max.z, v));
                float height = float.NegativeInfinity;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = transformed[triangles[t]], b = transformed[triangles[t + 1]], c = transformed[triangles[t + 2]];
                    float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                    if (Mathf.Abs(denominator) < 1e-12f) continue;
                    float wa = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.y - c.z)) / denominator;
                    float wb = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.y - c.z)) / denominator;
                    float wc = 1f - wa - wb;
                    if (wa >= -0.00001f && wb >= -0.00001f && wc >= -0.00001f)
                        height = Mathf.Max(height, wa * a.y + wb * b.y + wc * c.y);
                }
                if (float.IsNegativeInfinity(height))
                {
                    // The imported boundary is irregular. Extend nearest source height before blending.
                    float closest = float.PositiveInfinity;
                    foreach (var p in transformed)
                    {
                        float distance = (new Vector2(p.x, p.z) - point).sqrMagnitude;
                        if (distance < closest) { closest = distance; height = p.y; }
                    }
                }
                else sampled++;
                float distanceToEdge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * Size;
                // One flat ring prevents even the discrete mesh normals from differing at seams.
                float blend = Mathf.Clamp01((distanceToEdge - Size / (Resolution - 1)) / BorderWidth);
                blend = blend * blend * blend * (blend * (blend * 6f - 15f) + 10f);
                // Matched corner aprons keep wide landing footpads supported at tile junctions.
                float cornerX = Mathf.Min(u, 1f - u) * Size;
                float cornerZ = Mathf.Min(v, 1f - v) * Size;
                float cornerBlend = Mathf.Clamp01((Mathf.Sqrt(cornerX * cornerX + cornerZ * cornerZ) - 5f) / 1.5f);
                cornerBlend = cornerBlend * cornerBlend * (3f - 2f * cornerBlend);
                blend *= cornerBlend;
                int index = z * Resolution + x;
                vertices[index] = new Vector3((u - 0.5f) * Size, height * heightScale * blend, (v - 0.5f) * Size);
                uv[index] = new Vector2(u, v);
            }
            var indices = new int[(Resolution - 1) * (Resolution - 1) * 6];
            int cursor = 0;
            for (int z = 0; z < Resolution - 1; z++) for (int x = 0; x < Resolution - 1; x++)
            {
                int a = z * Resolution + x, b = a + Resolution;
                indices[cursor++] = a; indices[cursor++] = b; indices[cursor++] = a + 1;
                indices[cursor++] = a + 1; indices[cursor++] = b; indices[cursor++] = b + 1;
            }
            var mesh = new Mesh { name = "MoonTileSeamCorrected" };
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = indices;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            Directory.CreateDirectory(Path.GetDirectoryName(MeshPath));
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "MoonRegolith" };
            material.SetColor("_BaseColor", new Color(0.33f, 0.33f, 0.33f, 1f));
            material.SetFloat("_Smoothness", 0.08f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            var tile = new GameObject("MoonTile");
            try
            {
                tile.AddComponent<MeshFilter>().sharedMesh = mesh;
                tile.AddComponent<MeshRenderer>().sharedMaterial = material;
                tile.AddComponent<MeshCollider>().sharedMesh = mesh;
                tile.isStatic = true;
                PrefabUtility.SaveAsPrefabAsset(tile, PrefabPath);
            }
            finally { Object.DestroyImmediate(tile); }
            AssetDatabase.SaveAssets();
            Debug.Log($"Derived MoonTile: {sampled}/{vertices.Length} samples projected onto source, matched boundaries, {mesh.vertexCount} vertices. Original FBX/import settings unchanged.");
        }

        [MenuItem("Apollo15/Terrain/Generate Selected Moon Surface")]
        public static void GenerateSelected()
        {
            var layout = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponent<MoonSurfaceLayout>();
            if (layout == null) throw new System.InvalidOperationException("Select a MoonSurfaceLayout object.");
            Generate(layout);
            EditorSceneManager.SaveScene(layout.gameObject.scene);
        }

        public static void Generate(MoonSurfaceLayout layout)
        {
            if (layout.TilePrefab == null) throw new System.InvalidOperationException("Assign a tile prefab first.");
            Undo.RecordObject(layout, "Generate lunar surface");
            if (layout.GeneratedRoot != null) Undo.DestroyObjectImmediate(layout.GeneratedRoot.gameObject);
            var generated = new GameObject("GeneratedTiles");
            Undo.RegisterCreatedObjectUndo(generated, "Generate lunar surface");
            generated.transform.SetParent(layout.transform, false);
            layout.Configure(layout.TilePrefab, generated.transform);
            for (int z = 0; z < layout.Rows; z++) for (int x = 0; x < layout.Columns; x++)
            {
                var tile = (GameObject)PrefabUtility.InstantiatePrefab(layout.TilePrefab, generated.transform);
                Undo.RegisterCreatedObjectUndo(tile, "Generate lunar tile");
                tile.name = $"MoonTile_{x:D2}_{z:D2}";
                tile.transform.localPosition = new Vector3((x - (layout.Columns - 1) * 0.5f) * layout.TileSize, 0f, (z - (layout.Rows - 1) * 0.5f) * layout.TileSize);
                tile.transform.localScale = Vector3.one * (layout.TileSize / Size);
            }
            float width = layout.Columns * layout.TileSize, depth = layout.Rows * layout.TileSize;
            for (int i = 0; i < 4; i++)
            {
                var wall = new GameObject("PerimeterCollider_" + i);
                Undo.RegisterCreatedObjectUndo(wall, "Create perimeter collision");
                wall.transform.SetParent(generated.transform, false);
                wall.transform.localPosition = i < 2 ? new Vector3((i == 0 ? -1 : 1) * width * 0.5f, 3f, 0f) : new Vector3(0f, 3f, (i == 2 ? -1 : 1) * depth * 0.5f);
                wall.AddComponent<BoxCollider>().size = i < 2 ? new Vector3(0.5f, 10f, depth) : new Vector3(width, 10f, 0.5f);
                wall.isStatic = true;
            }
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
        }
    }
}
