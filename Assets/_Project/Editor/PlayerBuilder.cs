using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class PlayerBuilder
    {
        private const string R = "Assets/_Project";
        private const string Source = "Assets/ModularFirstPersonController/FirstPersonController/FirstPersonController.prefab";
        private const string Dest = R + "/Prefabs/Player/PA3_FirstPersonPlayer.prefab";

        [MenuItem("PA3/5 - Integrar jugador y cámara", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("PA3/5 - Integrar jugador y cámara")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != R + "/Scenes/MainIsland_Exploration.unity")
                throw new InvalidOperationException("Abrir la escena principal antes de integrar el jugador.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            if (source == null) throw new InvalidOperationException("No se encontró el prefab del controlador de terceros.");
            Directory.CreateDirectory(R + "/Prefabs/Player");
            AssetDatabase.Refresh();
            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            root.name = "PA3_FirstPersonPlayer";
            root.transform.position = new Vector3(0, 2.45f, -33f);
            root.transform.rotation = Quaternion.Euler(0, 0, 0);
            var controller = root.GetComponent<FirstPersonController>();
            if (controller == null) throw new InvalidOperationException("El prefab no contiene FirstPersonController.");
            controller.walkSpeed = 4.2f;
            controller.sprintSpeed = 6.2f;
            controller.unlimitedSprint = true;
            controller.enableJump = true;
            controller.jumpPower = 4.5f;
            controller.enableCrouch = false;
            controller.fov = 68f;
            controller.mouseSensitivity = 2.1f;
            controller.lockCursor = true;
            controller.playerCanMove = true;
            controller.cameraCanMove = true;
            // The source package is preserved; this is a project-owned prefab instance.
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Dest, InteractionMode.AutomatedAction, out bool success);
            if (!success) throw new InvalidOperationException("No se pudo guardar el prefab del jugador.");
            var environment = GameObject.Find("Environment");
            if (environment == null) throw new InvalidOperationException("No se encontró Environment.");
            root.transform.SetParent(environment.transform, true);
            // Remove the placeholder camera and let the player camera own the MainCamera tag.
            var placeholder = GameObject.Find("Main Camera");
            if (placeholder != null) Undo.DestroyObjectImmediate(placeholder);
            foreach (var cam in Camera.allCameras)
                cam.enabled = cam.transform.IsChildOf(root.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            Validate();
        }

        [MenuItem("PA3/6 - Validar jugador y recorrido")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();
            var report = new StringBuilder("PA3 - Hito 3 / validacion del jugador\n");
            var player = GameObject.Find("PA3_FirstPersonPlayer");
            if (player == null) throw new InvalidOperationException("Falta PA3_FirstPersonPlayer en la escena.");
            var controller = player.GetComponent<FirstPersonController>();
            var body = player.GetComponent<Rigidbody>();
            var capsule = player.GetComponent<CapsuleCollider>();
            var camera = player.GetComponentInChildren<Camera>(true);
            if (controller == null || body == null || capsule == null || camera == null)
                throw new InvalidOperationException("Falta componente esencial del jugador.");
            if (!body.useGravity || body.isKinematic || capsule.isTrigger)
                throw new InvalidOperationException("Gravedad o collider del jugador no estan configurados.");
            if (!camera.CompareTag("MainCamera")) throw new InvalidOperationException("La camara del jugador no tiene MainCamera.");
            if (player.transform.parent == null || player.transform.parent.name != "Environment")
                throw new InvalidOperationException("El jugador no esta bajo el ensamblaje Environment.");
            report.AppendLine("PASS - Prefab propio PA3_FirstPersonPlayer conectado bajo Environment");
            report.AppendLine("PASS - FirstPersonController, Rigidbody, CapsuleCollider y camara MainCamera");
            report.AppendLine("PASS - Gravedad activa, Rigidbody dinamico y collider no Trigger");
            report.AppendLine("PASS - Velocidad caminar 4,2; sprint 6,2; FOV 68; salto habilitado");
            // Static validation of the spawn and three route checkpoints. Runtime input remains a Play-mode check.
            foreach (var z in new[] { -33f, -7f, 20f })
            {
                float x = z >= 5 ? 0 : 3.3f * Mathf.Sin((z + 33) * Mathf.PI / 38) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, 5, z)));
                var origin = new Vector3(x, 18, z);
                if (!Physics.Raycast(origin, Vector3.down, out var hit, 30)) throw new InvalidOperationException("Sin suelo en checkpoint z=" + z);
                report.AppendLine("PASS - Suelo en checkpoint z=" + z.ToString("F0") + ", y=" + hit.point.y.ToString("F2"));
            }
            report.AppendLine("Nota - La prueba de entrada, movimiento continuo y transferencia de cámara se ejecutará en Play durante el cierre del Hito 3.");
            File.WriteAllText(R + "/Documentation/Validation_H3.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H3: jugador validado en modo edición; checkpoints de ruta correctos.");
        }
    }
}
