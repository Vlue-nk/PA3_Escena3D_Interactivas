using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PA3.EditorTools
{
    public static class MissingScriptScanner
    {
        private const string Root = "Assets/_Project";
        [MenuItem("PA3/98 - Diagnosticar scripts faltantes")]
        public static void Scan()
        {
            var report = new StringBuilder("PA3 - Diagnostico de scripts faltantes\n");
            var sceneCount = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                ScanObject(root, root.name, ref sceneCount, report);
            report.AppendLine("Scene missing scripts: " + sceneCount);
            var prefabCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var contents = PrefabUtility.LoadPrefabContents(path);
                var before = prefabCount;
                ScanObject(contents, path, ref prefabCount, report);
                if (prefabCount == before) report.AppendLine("PASS - " + path);
                PrefabUtility.UnloadPrefabContents(contents);
            }
            report.AppendLine("Prefab missing scripts: " + prefabCount);
            File.WriteAllText(Root + "/Documentation/Validation_MissingScripts.txt", report.ToString());
            AssetDatabase.Refresh();
            Debug.Log("PA3: diagnostico de scripts faltantes completado. Scene=" + sceneCount + " Prefab=" + prefabCount);
        }

        private static void ScanObject(GameObject obj, string path, ref int count, StringBuilder report)
        {
            var missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
            if (missing > 0)
            {
                count += missing;
                report.AppendLine("MISSING - " + path + " (" + missing + ")");
            }
            foreach (Transform child in obj.transform) ScanObject(child.gameObject, path + "/" + child.name, ref count, report);
        }
    }
}
