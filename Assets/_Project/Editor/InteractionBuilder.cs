using System;
using System.IO;
using System.Text;
using PA3.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class InteractionBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabDir = Root + "/Prefabs/Interactables";
        private const string PrefabPath = PrefabDir + "/Interactable_MysticRelic.prefab";
        private const string ScenePath = Root + "/Scenes/MainIsland_Exploration.unity";
        private const string RelicPath = Root + "/Prefabs/Environment/Props/Prop_Relic.prefab";
        private const string MysticMaterialPath = Root + "/Materials/Environment/Props/MAT_MysticRelic.mat";
        private const string GlowMaterialPath = Root + "/Materials/Environment/Props/Prop_Glow.mat";

        [MenuItem("PA3/11 - Crear interacción y VFX", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/11 - Crear interacción y VFX")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Abrir la escena principal antes de crear la interacción.");
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
            var prefab = CreatePrefab();

            var old = GameObject.Find("MysticRelic_Interactable");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = GameObject.Find("Interactables") ?? new GameObject("Interactables");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "MysticRelic_Interactable";
            instance.transform.SetParent(root.transform);
            instance.transform.position = GroundPosition(-1.8f, 22.5f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("PA3 H6: interacción de reliquia y VFX modular creados.");
        }

        [MenuItem("PA3/12 - Validar interacción y VFX", true)]
        private static bool CanValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/12 - Validar interacción y VFX")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();
            var report = new StringBuilder("PA3 - Hito 6 / validación de interacción y VFX\n");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Falta Interactable_MysticRelic.prefab.");
            report.AppendLine("PASS - Prefab propio Interactable_MysticRelic bajo _Project/Prefabs/Interactables");
            var instance = GameObject.Find("MysticRelic_Interactable");
            if (instance == null) throw new InvalidOperationException("No existe MysticRelic_Interactable en la escena.");
            var interaction = instance.GetComponent<MysticRelicInteraction>();
            if (interaction == null) throw new InvalidOperationException("Falta MysticRelicInteraction.");
            report.AppendLine("PASS - Componente MysticRelicInteraction conectado");
            var trigger = instance.GetComponent<SphereCollider>();
            if (trigger == null || !trigger.isTrigger || trigger.radius < 3f)
                throw new InvalidOperationException("El trigger de interacción no está configurado.");
            report.AppendLine("PASS - SphereCollider trigger con volumen de proximidad");
            var vfx = instance.GetComponentInChildren<ParticleSystem>();
            if (vfx == null || vfx.main.playOnAwake) throw new InvalidOperationException("VFX de activación no configurado.");
            report.AppendLine("PASS - ParticleSystem de activación listo para respuesta al jugador");
            if (interaction.PulseLight == null || interaction.CoreRenderer == null)
                throw new InvalidOperationException("Faltan referencias de luz o núcleo de la reliquia.");
            report.AppendLine("PASS - Luz de pulso y núcleo místico conectados");
            File.WriteAllText(Root + "/Documentation/Validation_H6.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H6: interacción y VFX validados.");
        }

        private static GameObject CreatePrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(RelicPath);
            if (source == null) throw new InvalidOperationException("No se encontró Prop_Relic.prefab.");
            var mysticMaterial = AssetDatabase.LoadAssetAtPath<Material>(MysticMaterialPath);
            var glowMaterial = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            var root = new GameObject("Interactable_MysticRelic");
            var visual = UnityEngine.Object.Instantiate(source);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            var core = visual.transform.Find("Core");
            var interaction = root.AddComponent<MysticRelicInteraction>();
            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.4f, 0f);
            trigger.radius = 3.35f;

            var vfxObject = new GameObject("ActivationVFX");
            vfxObject.transform.SetParent(root.transform, false);
            vfxObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var vfx = vfxObject.AddComponent<ParticleSystem>();
            var main = vfx.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1.5f;
            main.startLifetime = 1.15f;
            main.startSpeed = 1.6f;
            main.startSize = 0.16f;
            main.startColor = new Color(0.08f, 0.85f, 1f, 0.92f);
            main.maxParticles = 64;
            var emission = vfx.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });
            var shape = vfx.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.55f;
            var renderer = vfx.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = glowMaterial;

            var lightObject = new GameObject("PulseLight");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var pulseLight = lightObject.AddComponent<Light>();
            pulseLight.type = LightType.Point;
            pulseLight.color = new Color(0.05f, 0.75f, 1f);
            pulseLight.range = 4.5f;
            pulseLight.intensity = 0f;

            var audioObject = new GameObject("FeedbackAudio");
            audioObject.transform.SetParent(root.transform, false);
            var audio = audioObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.volume = 0.7f;

            interaction.ActivationVfx = vfx;
            interaction.FeedbackAudio = audio;
            interaction.CoreRenderer = core != null ? core.GetComponent<Renderer>() : null;
            interaction.PulseLight = pulseLight;
            if (mysticMaterial != null && interaction.CoreRenderer != null)
                interaction.CoreRenderer.sharedMaterial = mysticMaterial;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static Vector3 GroundPosition(float x, float z)
        {
            var origin = new Vector3(x, 30f, z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 60f)) return hit.point;
            return new Vector3(x, 7.5f, z);
        }
    }
}
