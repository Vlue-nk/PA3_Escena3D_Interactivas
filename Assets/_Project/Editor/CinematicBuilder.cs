using System;
using System.IO;
using System.Text;
using PA3.Cinematics;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace PA3.EditorTools
{
    public static class CinematicBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabDir = Root + "/Prefabs/Cinematics";
        private const string PrefabPath = PrefabDir + "/PA3_IntroSequence.prefab";
        private const string TimelineDir = Root + "/Cinematics";
        private const string TimelinePath = TimelineDir + "/PA3_IntroSequence.playable";
        private const string ScenePath = Root + "/Scenes/MainIsland_Exploration.unity";

        [MenuItem("PA3/15 - Crear cinemática introductoria", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/15 - Crear cinemática introductoria")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Abrir la escena principal antes de crear la cinemática.");
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(TimelineDir);
            AssetDatabase.Refresh();
            var prefab = CreatePrefab();
            var old = GameObject.Find("Cinematics");
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var root = new GameObject("Cinematics");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "PA3_IntroSequence";
            instance.transform.SetParent(root.transform);
            var controller = instance.GetComponent<IntroSequenceController>();
            controller.Player = GameObject.Find("PA3_FirstPersonPlayer");
            var playerCamera = controller.Player != null ? controller.Player.GetComponentInChildren<Camera>(true) : null;
            if (playerCamera == null)
            {
                var cameras = Resources.FindObjectsOfTypeAll<Camera>();
                foreach (var candidate in cameras)
                {
                    if (candidate.gameObject.scene.IsValid())
                    {
                        playerCamera = candidate;
                        break;
                    }
                }
            }
            if (playerCamera != null && playerCamera.GetComponent<CinemachineBrain>() == null)
                playerCamera.gameObject.AddComponent<CinemachineBrain>();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("PA3 H8: cinemática introductoria de 7 segundos creada.");
        }

        [MenuItem("PA3/16 - Validar cinemática introductoria", true)]
        private static bool CanValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/16 - Validar cinemática introductoria")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();
            var report = new StringBuilder("PA3 - Hito 8 / validación de cinemática introductoria\n");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                throw new InvalidOperationException("Falta PA3_IntroSequence.prefab.");
            report.AppendLine("PASS - Prefab propio PA3_IntroSequence bajo _Project/Prefabs/Cinematics");
            var root = GameObject.Find("Cinematics");
            var sequence = root?.transform.Find("PA3_IntroSequence");
            if (sequence == null) throw new InvalidOperationException("No existe PA3_IntroSequence en escena.");
            var director = sequence.GetComponent<PlayableDirector>();
            if (director == null || director.playableAsset == null)
                throw new InvalidOperationException("PlayableDirector sin Timeline.");
            if (director.playableAsset.duration < 6.9 || director.playableAsset.duration > 7.1)
                throw new InvalidOperationException("La duración de la cinemática no es 7 segundos.");
            report.AppendLine("PASS - Timeline asignado con duración de 7 segundos");
            var camera = sequence.GetComponentInChildren<CinemachineCamera>(true);
            if (camera == null) throw new InvalidOperationException("Falta Cinemachine Camera.");
            report.AppendLine("PASS - Cinemachine Camera presente para la toma introductoria");
            CinemachineBrain brain = null;
            foreach (var candidate in Resources.FindObjectsOfTypeAll<CinemachineBrain>())
            {
                if (candidate.gameObject.scene.IsValid())
                {
                    brain = candidate;
                    break;
                }
            }
            if (brain == null) throw new InvalidOperationException("MainCamera no tiene CinemachineBrain.");
            report.AppendLine("PASS - MainCamera con CinemachineBrain");
            var controller = sequence.GetComponent<IntroSequenceController>();
            if (controller == null || controller.Player == null)
                throw new InvalidOperationException("Controlador de cesión al jugador no configurado.");
            report.AppendLine("PASS - IntroSequenceController cede el control al jugador al finalizar");
            File.WriteAllText(Root + "/Documentation/Validation_H8.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H8: cinemática introductoria validada.");
        }

        private static GameObject CreatePrefab()
        {
            var timeline = CreateTimeline();
            var root = new GameObject("PA3_IntroSequence");
            var cameraObject = new GameObject("CinematicCamera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(8f, 10f, -18f);
            cameraObject.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, 6f, 22f) - cameraObject.transform.localPosition);
            var camera = cameraObject.AddComponent<CinemachineCamera>();
            camera.Priority.Value = 100;
            camera.Lens.FieldOfView = 52f;
            var director = root.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.SetReferenceValue(new PropertyName("IntroCamera"), camera);
            var controller = root.AddComponent<IntroSequenceController>();
            controller.Director = director;
            controller.IntroCamera = camera;
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static TimelineAsset CreateTimeline()
        {
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (timeline != null) return timeline;
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.editorSettings.frameRate = 30;
            AssetDatabase.CreateAsset(timeline, TimelinePath);
            var cameraTrack = timeline.CreateTrack<CinemachineTrack>(null, "Intro Camera");
            var cameraClip = cameraTrack.CreateClip<CinemachineShot>();
            cameraClip.start = 0;
            cameraClip.duration = 7;
            var shot = (CinemachineShot)cameraClip.asset;
            shot.DisplayName = "Santuario - apertura";
            shot.VirtualCamera.exposedName = "IntroCamera";
            var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Intro Camera Motion");
            var animationClip = new AnimationClip { name = "PA3_IntroCameraMotion", frameRate = 30 };
            animationClip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 8f, 7f, 2f));
            animationClip.SetCurve(string.Empty, typeof(Transform), "localPosition.y", AnimationCurve.Linear(0f, 10f, 7f, 8f));
            animationClip.SetCurve(string.Empty, typeof(Transform), "localPosition.z", AnimationCurve.Linear(0f, -18f, 7f, -8f));
            AssetDatabase.AddObjectToAsset(animationClip, timeline);
            var motionClip = animationTrack.CreateClip<AnimationPlayableAsset>();
            motionClip.start = 0;
            motionClip.duration = 7;
            ((AnimationPlayableAsset)motionClip.asset).clip = animationClip;
            EditorUtility.SetDirty(timeline);
            return timeline;
        }
    }
}
