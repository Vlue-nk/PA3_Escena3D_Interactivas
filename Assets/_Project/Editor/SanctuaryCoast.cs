using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PA3.EditorTools
{
    public static class SanctuaryCoast
    {
        const string Folder = "Assets/_Project/Terrain/Layers/";

        [MenuItem("PA3/25 - Pintar arena costera %&F7")]
        public static void Paint()
        {
            PA3Workspace.RequireEditMode();
            var terrain = Object.FindAnyObjectByType<Terrain>();
            if (terrain == null) throw new System.InvalidOperationException("Falta Terrain.");
            var data = terrain.terrainData;
            string layerPath = Folder + "Coast_Sand.terrainlayer";
            var sand = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (sand == null)
            {
                Directory.CreateDirectory(Folder);
                var texture = new Texture2D(256, 256, TextureFormat.RGB24, true);
                var random = new System.Random(327);
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float grain = (float)random.NextDouble() * .045f;
                        float ripple = Mathf.Sin(y * Mathf.PI / 16 + Mathf.Sin(x * Mathf.PI / 128) * .8f) * .014f;
                        texture.SetPixel(x, y, new Color(.72f + grain + ripple, .63f + grain + ripple, .44f + grain + ripple));
                    }
                texture.Apply();
                string texturePath = Folder + "Coast_Sand.png";
                File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath);
                sand = new TerrainLayer {name = "Coast_Sand", diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath), tileSize = new Vector2(4, 4), metallic = 0, smoothness = .08f};
                AssetDatabase.CreateAsset(sand, layerPath);
            }
            Undo.RegisterCompleteObjectUndo(data, "Pintar arena costera");
            var layers = data.terrainLayers.ToList();
            int sandIndex = layers.IndexOf(sand);
            if (sandIndex < 0) {sandIndex = layers.Count; layers.Add(sand); data.terrainLayers = layers.ToArray();}
            int width = data.alphamapWidth, height = data.alphamapHeight;
            var weights = data.GetAlphamaps(0, 0, width, height);
            for (int z = 0; z < height; z++)
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1), v = z / (float)(height - 1);
                    float wx = terrain.transform.position.x + u * data.size.x;
                    float wz = terrain.transform.position.z + v * data.size.z;
                    float altitude = terrain.transform.position.y + data.GetInterpolatedHeight(u, v);
                    float edge = Mathf.Sqrt(wx * wx / (42 * 42) + (wz - 1) * (wz - 1) / (45 * 45));
                    float variation = (Mathf.PerlinNoise(wx * .16f + 20, wz * .16f + 20) - .5f) * .035f;
                    float coastal = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.73f + variation, .85f + variation, edge));
                    float lowland = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.5f, 3.1f, altitude));
                    float cliff = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(38, 60, data.GetSteepness(u, v)));
                    float weight = coastal * lowland * (1 - cliff * .8f);
                    float previous = 1 - weights[z, x, sandIndex];
                    for (int i = 0; i < layers.Count; i++)
                        if (i != sandIndex) weights[z, x, i] = previous > .0001f ? weights[z, x, i] / previous * (1 - weight) : (i == 2 ? 1 - weight : 0);
                    weights[z, x, sandIndex] = weight;
                }
            data.SetAlphamaps(0, 0, weights);
            data.SetBaseMapDirty();
            EditorUtility.SetDirty(data);
            foreach (var texture in data.alphamapTextures) EditorUtility.SetDirty(texture);
            AssetDatabase.SaveAssets();
            Debug.Log("Costa actualizada: arena, transición a césped y roca en pendientes; relieve y colisiones conservados.");
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 0, 0), Quaternion.Euler(55, 0, 0), 67);
        }
    }
}
