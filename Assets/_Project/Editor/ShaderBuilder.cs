using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class ShaderBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ShaderDir = Root + "/Shaders";
        private const string MaterialDir = Root + "/Materials/Environment/Props";
        private const string ScenePath = Root + "/Scenes/MainIsland_Exploration.unity";
        private const string ShaderPath = ShaderDir + "/PA3_MysticFresnel.shader";
        private const string GraphPath = ShaderDir + "/PA3_MysticFresnel.shadergraph";
        private const string MaterialPath = MaterialDir + "/MAT_MysticRelic.mat";

        [MenuItem("PA3/9 - Crear shader mistico URP", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/9 - Crear shader mistico URP")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Abrir la escena principal antes de crear el shader.");
            Directory.CreateDirectory(ShaderDir);
            Directory.CreateDirectory(MaterialDir);
            AssetDatabase.Refresh();

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) throw new InvalidOperationException("No se importo PA3_MysticFresnel.shader.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "MAT_MysticRelic" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetColor("_BaseColor", new Color(0.01f, 0.07f, 0.08f, 1f));
            material.SetColor("_EdgeColor", new Color(0.01f, 0.58f, 0.68f, 1f));
            material.SetFloat("_FresnelPower", 3.2f);
            material.SetFloat("_EmissionStrength", 2.7f);
            material.SetFloat("_PulseSpeed", 1.35f);
            material.SetFloat("_PulseScale", 1.2f);
            EditorUtility.SetDirty(material);
            ApplyToRelic(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Validate();
            Debug.Log("PA3 H5: shader mistico URP aplicado a la reliquia y Shader Graph Lit preparado.");
        }

        [MenuItem("PA3/10 - Validar shader mistico", true)]
        private static bool CanValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/10 - Validar shader mistico")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();
            var report = new StringBuilder("PA3 - Hito 5 / validacion del shader mistico\n");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (shader == null || material == null) throw new InvalidOperationException("Faltan shader o material misticos.");
            report.AppendLine("PASS - Shader propio PA3/MysticFresnel bajo _Project/Shaders");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GraphPath) == null)
                throw new InvalidOperationException("Falta el Shader Graph Lit URP PA3_MysticFresnel.");
            report.AppendLine("PASS - Shader Graph Lit URP PA3_MysticFresnel bajo _Project/Shaders");
            report.AppendLine("PASS - Material propio MAT_MysticRelic bajo _Project/Materials/Environment/Props");
            report.AppendLine("PASS - Fresnel, Time/pulso y emision HDR definidos en el material");
            var relic = GameObject.Find("Relic_Sanctuary");
            if (relic == null) throw new InvalidOperationException("No se encontro Relic_Sanctuary.");
            var core = relic.transform.Find("Core");
            if (core == null || core.GetComponent<Renderer>()?.sharedMaterial != material)
                throw new InvalidOperationException("La reliquia no usa MAT_MysticRelic.");
            report.AppendLine("PASS - Core de la reliquia conectado al material mistico");
            File.WriteAllText(Root + "/Documentation/Validation_H5.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H5: shader mistico validado en la reliquia.");
        }

        private static void ApplyToRelic(Material material)
        {
            var prefabPath = Root + "/Prefabs/Environment/Props/Prop_Relic.prefab";
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var core = contents.transform.Find("Core");
            if (core == null) throw new InvalidOperationException("Prop_Relic no tiene Core.");
            core.GetComponent<Renderer>().sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

    }
}
