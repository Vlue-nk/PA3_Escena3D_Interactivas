using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using PA3.Interaction;
using System.Linq;
using Object=UnityEngine.Object;

namespace PA3.EditorTools
{
    public static class SanctuaryFinish
    {
        const string R="Assets/_Project";
        [MenuItem("PA3/22 - Aplicar arte y luz %&F8")]
        public static void Apply()
        {
            PA3Workspace.RequireEditMode();
            var mat=AssetDatabase.LoadAssetAtPath<Material>(R+"/Materials/Environment/Props/MAT_MysticRelic.mat");
            mat.shader=AssetDatabase.LoadAssetAtPath<Shader>(R+"/Shaders/PA3_MysticFresnel.shadergraph");
            if(mat.shader==null || ShaderUtil.ShaderHasError(mat.shader))throw new System.InvalidOperationException("Shader Graph con errores");
            mat.SetFloat("_EmissionStrength",3f);EditorUtility.SetDirty(mat);
            foreach(var materialGuid in AssetDatabase.FindAssets("t:Material",new[]{R+"/Materials/Nature"}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuid));
                if(m.name.Contains("Grass")) {m.SetColor("_BaseColor",new Color(.3f,.48f,.13f));EditorUtility.SetDirty(m);}
                if(m.name.Contains("Poppy")) {m.SetColor("_BaseColor",new Color(.8f,.26f,.12f));EditorUtility.SetDirty(m);}
            }
            var journey=Object.FindAnyObjectByType<SanctuaryJourney>();
            SanctuaryPolish.BuildHUD(journey);
            // Keep renderer and physics configuration on the project-owned player prefab.
            var player=journey.Player;
            PrefabUtility.ApplyPrefabInstance(player.gameObject,InteractionMode.AutomatedAction);
            var lightSettings=AssetDatabase.LoadAssetAtPath<LightingSettings>(R+"/Lighting/Sanctuary_LightingSettings.lighting");
            if(lightSettings==null){lightSettings=new LightingSettings();AssetDatabase.CreateAsset(lightSettings,R+"/Lighting/Sanctuary_LightingSettings.lighting");}
            lightSettings.bakedGI=true;lightSettings.realtimeGI=false;
            lightSettings.lightmapper=LightingSettings.Lightmapper.UnityComputeGPU;
            lightSettings.lightmapResolution=6;lightSettings.lightmapMaxSize=1024;
            lightSettings.directSampleCount=16;lightSettings.indirectSampleCount=32;lightSettings.environmentSampleCount=32;
            lightSettings.maxBounces=2;lightSettings.mixedBakeMode=MixedLightingMode.Shadowmask;
            Lightmapping.lightingSettings=lightSettings;EditorUtility.SetDirty(lightSettings);
            foreach(var light in Object.FindObjectsByType<Light>())
            {
                if(light.type==LightType.Directional)light.lightmapBakeType=LightmapBakeType.Mixed;
                else if(light.name.StartsWith("AmberGuide"))light.lightmapBakeType=LightmapBakeType.Baked;
                else light.lightmapBakeType=LightmapBakeType.Realtime;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
            }
            foreach(var tr in Terrain.activeTerrains)GameObjectUtility.SetStaticEditorFlags(tr.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                bool architecture=renderer.transform.IsChildOf(GameObject.Find("Sanctuary_Platform").transform)||renderer.transform.IsChildOf(GameObject.Find("Arrival_Gateway").transform)||renderer.transform.IsChildOf(GameObject.Find("Processional_Ruins").transform);
                if(architecture)GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            }
            var probeObject=GameObject.Find("Path_LightProbes");if(probeObject==null)probeObject=new GameObject("Path_LightProbes");
            var probes=probeObject.GetComponent<LightProbeGroup>();if(probes==null)probes=probeObject.AddComponent<LightProbeGroup>();
            var terrain=Terrain.activeTerrain;var positions=new System.Collections.Generic.List<Vector3>();
            for(int z=-34;z<=28;z+=6)foreach(float x in new[]{-5f,0,5f})foreach(float y in new[]{.6f,2.4f})positions.Add(new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y+y,z));
            probes.probePositions=positions.ToArray();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Shader Graph Lit conectado, interfaz legible, materiales corregidos y luces preparadas para hornear.");
        }

        [MenuItem("PA3/23 - Hornear iluminacion")]
        public static void Bake()
        {
            PA3Workspace.RequireEditMode();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Lightmapping.BakeAsync();
        }
    }
}


