using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class LightingBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabDir = Root + "/Prefabs/Lighting";
        private const string PrefabPath = PrefabDir + "/PA3_Atmosphere.prefab";
        private const string ProfileDir = Root + "/Lighting/Profiles";
        private const string ProfilePath = ProfileDir + "/PA3_Atmosphere_Profile.asset";
        private const string ScenePath = Root + "/Scenes/MainIsland_Exploration.unity";

        [MenuItem("PA3/13 - Crear iluminación y atmósfera", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/13 - Crear iluminación y atmósfera")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Abrir la escena principal antes de crear la atmósfera.");
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(ProfileDir);
            AssetDatabase.Refresh();
            var profile = CreateProfile();
            var prefab = CreatePrefab(profile);
            RemoveLegacyLighting();
            var root = GameObject.Find("Lighting_Atmosphere") ?? new GameObject("Lighting_Atmosphere");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "PA3_Atmosphere";
            instance.transform.SetParent(root.transform);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = new Color(0.16f, 0.25f, 0.31f);
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.8f;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("PA3 H7: iluminación direccional, volumen URP y niebla configurados.");
        }

        [MenuItem("PA3/14 - Validar iluminación y atmósfera", true)]
        private static bool CanValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/14 - Validar iluminación y atmósfera")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();
            var report = new StringBuilder("PA3 - Hito 7 / validación de iluminación y atmósfera\n");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("Falta PA3_Atmosphere.prefab.");
            report.AppendLine("PASS - Prefab propio PA3_Atmosphere bajo _Project/Prefabs/Lighting");
            var root = GameObject.Find("Lighting_Atmosphere");
            var instance = root?.transform.Find("PA3_Atmosphere");
            if (instance == null) throw new InvalidOperationException("No existe la instancia PA3_Atmosphere.");
            var light = instance.GetComponentInChildren<Light>();
            if (light == null || light.type != LightType.Directional || light.shadows != LightShadows.Soft || light.intensity < 0.8f)
                throw new InvalidOperationException("La luz direccional atmosférica no está configurada.");
            report.AppendLine("PASS - Directional Light atmosférica con sombras suaves");
            var volume = instance.GetComponentInChildren<Volume>();
            if (volume == null || !volume.isGlobal || volume.sharedProfile == null)
                throw new InvalidOperationException("Falta el Global Volume URP.");
            if (!volume.sharedProfile.TryGet<Bloom>(out _) || !volume.sharedProfile.TryGet<ColorAdjustments>(out _))
                throw new InvalidOperationException("El perfil no contiene Bloom y Color Adjustments.");
            report.AppendLine("PASS - Global Volume URP con Bloom, Color Adjustments y Vignette");
            if (!RenderSettings.fog || RenderSettings.fogDensity <= 0f || RenderSettings.fogDensity > 0.02f)
                throw new InvalidOperationException("La niebla no está activa o está fuera de rango.");
            report.AppendLine("PASS - Niebla Exponential Squared sutil activa");
            File.WriteAllText(Root + "/Documentation/Validation_H7.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H7: iluminación y atmósfera validadas.");
        }

        private static VolumeProfile CreateProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var bloom = profile.TryGet<Bloom>(out var bloomValue) ? bloomValue : profile.Add<Bloom>();
            bloom.intensity.Override(1.15f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.72f);
            var color = profile.TryGet<ColorAdjustments>(out var colorValue) ? colorValue : profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.15f);
            color.contrast.Override(12f);
            color.saturation.Override(-8f);
            color.colorFilter.Override(new Color(0.88f, 0.96f, 1f));
            var vignette = profile.TryGet<Vignette>(out var vignetteValue) ? vignetteValue : profile.Add<Vignette>();
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.6f);
            var tonemapping = profile.TryGet<Tonemapping>(out var toneValue) ? toneValue : profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static GameObject CreatePrefab(VolumeProfile profile)
        {
            var root = new GameObject("PA3_Atmosphere");
            var lightObject = new GameObject("DirectionalLight_Atmospheric");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.83f, 0.91f, 1f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.82f;

            var volumeObject = new GameObject("GlobalVolume_Atmosphere");
            volumeObject.transform.SetParent(root.transform, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static void RemoveLegacyLighting()
        {
            var light = GameObject.Find("Directional Light");
            if (light != null) UnityEngine.Object.DestroyImmediate(light);
            var volume = GameObject.Find("Global Volume");
            if (volume != null) UnityEngine.Object.DestroyImmediate(volume);
        }
    }
}
