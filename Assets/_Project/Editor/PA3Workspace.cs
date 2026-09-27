using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class PA3Workspace
    {
        public const string Root = "Assets/_Project";

        public static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("PA3: salir de Play antes de editar recursos.");
        }

        [MenuItem("PA3/1 - Validar base tecnica")]
        public static void ValidateBase()
        {
            RequireEditMode();
            var report = new StringBuilder();
            report.AppendLine("PA3 - Hito 1 / validacion tecnica");
            report.AppendLine("Unity: " + Application.unityVersion);
            report.AppendLine("Escena: " + SceneManager.GetActiveScene().path);
            Check(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset,
                "URP activo en el nivel de calidad actual", report);
            Check(EditorSettings.serializationMode == SerializationMode.ForceText,
                "Serializacion Force Text", report);
            Check(File.ReadAllText("ProjectSettings/VersionControlSettings.asset").Contains("Visible Meta Files"),
                "Visible Meta Files", report);
            Check(File.ReadAllText("ProjectSettings/ProjectSettings.asset").Contains("activeInputHandler: 2"),
                "Input System Both", report);
            Check(SceneManager.GetActiveScene().path == Root + "/Scenes/MainIsland_Exploration.unity",
                "Escena principal correcta", report);
            foreach (var folder in new[] { "Environment", "Player", "Interactables", "Lighting" })
                Check(AssetDatabase.IsValidFolder(Root + "/Prefabs/" + folder), "Prefabs/" + folder, report);
            report.AppendLine("Console se verifica visualmente, no se deduce de este informe.");
            report.AppendLine("Avisos historicos: 308 warnings MaterialLocation.External de importacion Polytope; 0 errores observados.");
            report.AppendLine("Paquete original conservado. Este informe no declara corregido el importador externo.");
            File.WriteAllText(Root + "/Documentation/Validation_H1.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3 H1: base validada. Ver Assets/_Project/Documentation/Validation_H1.txt");
        }

        private static void Check(bool condition, string label, StringBuilder report)
        {
            if (!condition) throw new InvalidOperationException("PA3: comprobacion fallida: " + label);
            report.AppendLine("PASS - " + label);
        }
    }
}
