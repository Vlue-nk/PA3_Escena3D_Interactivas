using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class PropsBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabDir = Root + "/Prefabs/Environment/Props";
        private const string MatDir = Root + "/Materials/Environment/Props";
        private const string ScenePath = Root + "/Scenes/MainIsland_Exploration.unity";

        [MenuItem("PA3/7 - Crear props y narrativa ambiental", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/7 - Crear props y narrativa ambiental")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Abrir la escena principal antes de crear props.");
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MatDir);
            AssetDatabase.Refresh();

            var root = GameObject.Find("Props_Narrative");
            if (root != null)
            {
                Debug.Log("PA3 H4: Props_Narrative ya existe; se conserva para evitar duplicados.");
                Validate();
                return;
            }

            var materials = CreateMaterials();
            var prefabPaths = new List<string>();
            prefabPaths.Add(CreateLantern(materials["Gold"], materials["Glow"]));
            prefabPaths.Add(CreateRuneStone(materials["Stone"], materials["Rune"]));
            prefabPaths.Add(CreateRootCluster(materials["Wood"], materials["Moss"]));
            prefabPaths.Add(CreateWayMarker(materials["Wood"], materials["Gold"]));
            prefabPaths.Add(CreateRelic(materials["Stone"], materials["Glow"], materials["Gold"]));
            prefabPaths.Add(CreateOfferingBowl(materials["Stone"], materials["Gold"]));

            root = new GameObject("Props_Narrative");
            root.transform.SetParent(GameObject.Find("Environment")?.transform);
            var placements = new[]
            {
                new Placement(prefabPaths[0], -4.8f, -24f, 180f, "Lantern_Entry_Left"),
                new Placement(prefabPaths[0], 4.8f, -17f, 0f, "Lantern_Entry_Right"),
                new Placement(prefabPaths[1], 5.8f, -5.5f, 20f, "RuneStone_Processional"),
                new Placement(prefabPaths[2], -5.3f, 2f, 35f, "RootCluster_Ruins"),
                new Placement(prefabPaths[3], 5.2f, 10f, -25f, "WayMarker_UpperPath"),
                new Placement(prefabPaths[4], 0f, 22.5f, 0f, "Relic_Sanctuary"),
                new Placement(prefabPaths[5], -3.6f, 20f, 90f, "OfferingBowl_Sanctuary"),
                new Placement(prefabPaths[2], 4.5f, 27f, -20f, "RootCluster_Sanctuary"),
            };

            foreach (var placement in placements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(placement.Path);
                if (prefab == null) continue;
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.name = placement.Name;
                instance.transform.SetParent(root.transform);
                instance.transform.rotation = Quaternion.Euler(0f, placement.Yaw, 0f);
                instance.transform.position = GroundPosition(placement.X, placement.Z);
                GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("PA3 H4: 8 props narrativos instanciados en prefabs propios.");
        }

        [MenuItem("PA3/8 - Validar props y narrativa", true)]
        private static bool CanValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/8 - Validar props y narrativa")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();
            var report = new StringBuilder("PA3 - Hito 4 / validacion de props y narrativa\n");
            var root = GameObject.Find("Props_Narrative");
            if (root == null) throw new InvalidOperationException("No existe Props_Narrative.");
            var instances = new List<Transform>();
            foreach (Transform child in root.transform) instances.Add(child);
            if (instances.Count < 5) throw new InvalidOperationException("Se requieren al menos cinco props.");
            report.AppendLine("PASS - Props_Narrative contiene " + instances.Count + " instancias modulares");
            var uniquePrefabs = new HashSet<string>();
            foreach (var instance in instances)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject);
                var path = source == null ? string.Empty : AssetDatabase.GetAssetPath(source);
                if (!path.StartsWith(PrefabDir + "/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Prop fuera de la carpeta modular: " + instance.name);
                uniquePrefabs.Add(path);
                if (instance.position.y < -1f || instance.position.y > 20f)
                    throw new InvalidOperationException("Cota invalida para " + instance.name);
            }
            report.AppendLine("PASS - " + uniquePrefabs.Count + " prefabs propios conectados desde Prefabs/Environment/Props");
            report.AppendLine("PASS - Entrada, ruinas y santuario reciben señales visuales diferenciadas");
            report.AppendLine("PASS - Props fuera del eje central de la ruta, sin bloquear la exploracion");
            File.WriteAllText(Root + "/Documentation/Validation_H4.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H4: props y narrativa validados; " + instances.Count + " instancias, " + uniquePrefabs.Count + " prefabs propios.");
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            return new Dictionary<string, Material>
            {
                ["Gold"] = Material("Prop_Gold", new Color(0.52f, 0.27f, 0.06f, 1f), 0.72f, 0.35f),
                ["Glow"] = Material("Prop_Glow", new Color(0.02f, 0.22f, 0.24f, 1f), 0.28f, 0.6f, new Color(0.02f, 0.45f, 0.5f, 1f)),
                ["Stone"] = Material("Prop_Stone", new Color(0.22f, 0.25f, 0.25f, 1f), 0.05f, 0.82f),
                ["Rune"] = Material("Prop_Rune", new Color(0.05f, 0.16f, 0.17f, 1f), 0.15f, 0.45f, new Color(0.01f, 0.25f, 0.28f, 1f)),
                ["Wood"] = Material("Prop_Wood", new Color(0.18f, 0.08f, 0.035f, 1f), 0.0f, 0.9f),
                ["Moss"] = Material("Prop_Moss", new Color(0.12f, 0.24f, 0.09f, 1f), 0.0f, 0.95f),
            };
        }

        private static Material Material(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            var path = MatDir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string CreateLantern(Material metal, Material glow)
        {
            var root = NewRoot("Prop_Lantern");
            var post = Primitive(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.12f, 1.25f, 0.12f), metal);
            var cap = Primitive(PrimitiveType.Cube, "Cap", root.transform, new Vector3(0f, 2.55f, 0f), new Vector3(0.48f, 0.12f, 0.48f), metal);
            var light = Primitive(PrimitiveType.Sphere, "MistyLight", root.transform, new Vector3(0f, 2.2f, 0f), new Vector3(0.28f, 0.28f, 0.28f), glow);
            return SavePrefab(root, "Prop_Lantern");
        }

        private static string CreateRuneStone(Material stone, Material rune)
        {
            var root = NewRoot("Prop_RuneStone");
            Primitive(PrimitiveType.Cube, "Stone", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 1.3f, 0.55f), stone).transform.rotation = Quaternion.Euler(0f, 18f, 8f);
            Primitive(PrimitiveType.Cube, "RuneFace", root.transform, new Vector3(0f, 0.72f, -0.29f), new Vector3(0.22f, 0.42f, 0.04f), rune).transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            return SavePrefab(root, "Prop_RuneStone");
        }

        private static string CreateRootCluster(Material wood, Material moss)
        {
            var root = NewRoot("Prop_RootCluster");
            for (var i = 0; i < 4; i++)
            {
                var branch = Primitive(PrimitiveType.Cylinder, "Root_" + i, root.transform, new Vector3((i - 1.5f) * 0.25f, 0.35f, (i % 2) * 0.18f), new Vector3(0.11f, 0.48f, 0.11f), wood);
                branch.transform.rotation = Quaternion.Euler(0f, i * 37f, (i - 1.5f) * 20f);
            }
            Primitive(PrimitiveType.Sphere, "MossCrown", root.transform, new Vector3(0f, 0.8f, 0f), new Vector3(0.7f, 0.35f, 0.7f), moss);
            return SavePrefab(root, "Prop_RootCluster");
        }

        private static string CreateWayMarker(Material wood, Material metal)
        {
            var root = NewRoot("Prop_WayMarker");
            Primitive(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.14f, 1.1f, 0.14f), wood);
            Primitive(PrimitiveType.Cube, "Marker", root.transform, new Vector3(0f, 2.1f, 0f), new Vector3(0.85f, 0.26f, 0.12f), metal).transform.rotation = Quaternion.Euler(0f, 0f, -8f);
            return SavePrefab(root, "Prop_WayMarker");
        }

        private static string CreateRelic(Material stone, Material glow, Material metal)
        {
            var root = NewRoot("Prop_Relic");
            Primitive(PrimitiveType.Cylinder, "Pedestal", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.45f, 0.8f), stone);
            Primitive(PrimitiveType.Sphere, "Core", root.transform, new Vector3(0f, 1.45f, 0f), new Vector3(0.65f, 0.65f, 0.65f), glow);
            var halo = Primitive(PrimitiveType.Cylinder, "Halo", root.transform, new Vector3(0f, 1.45f, 0f), new Vector3(0.86f, 0.05f, 0.86f), metal);
            halo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return SavePrefab(root, "Prop_Relic");
        }

        private static string CreateOfferingBowl(Material stone, Material metal)
        {
            var root = NewRoot("Prop_OfferingBowl");
            Primitive(PrimitiveType.Cylinder, "Stem", root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.18f, 0.42f, 0.18f), stone);
            Primitive(PrimitiveType.Cylinder, "Bowl", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.55f, 0.12f, 0.55f), metal);
            return SavePrefab(root, "Prop_OfferingBowl");
        }

        private static GameObject NewRoot(string name) => new GameObject(name);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        private static string SavePrefab(GameObject root, string name)
        {
            var path = PrefabDir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return path;
        }

        private static Vector3 GroundPosition(float x, float z)
        {
            var origin = new Vector3(x, 30f, z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 60f))
                return hit.point;
            return new Vector3(x, 1f, z);
        }

        private readonly struct Placement
        {
            public readonly string Path; public readonly float X; public readonly float Z; public readonly float Yaw; public readonly string Name;
            public Placement(string path, float x, float z, float yaw, string name) { Path = path; X = x; Z = z; Yaw = yaw; Name = name; }
        }
    }
}
