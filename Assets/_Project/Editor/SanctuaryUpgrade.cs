using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using PA3.Cinematics;
using PA3.Interaction;
using Object = UnityEngine.Object;

namespace PA3.EditorTools
{
    public static class SanctuaryUpgrade
    {
        const string R="Assets/_Project";
        const string Nature="Assets/Polytope Studio/Lowpoly_Environments/Prefabs/";
        static Terrain terrain;
        static readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
        static System.Random random;
        static float Rand(float a,float b)=>(float)(a+random.NextDouble()*(b-a));
        static float Ground(float x,float z)=>terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
        static float PathX(float z)=> z>=5?0:3.3f*Mathf.Sin((z+33)*Mathf.PI/38)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,5,z)));

        [MenuItem("PA3/20 - Renovar santuario jugable %&r")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("MainIsland_Exploration.unity")) throw new InvalidOperationException("Abrir MainIsland_Exploration.");
            random=new System.Random(318);
            materials.Clear();
            foreach(string dir in new[]{"Materials/Nature","Prefabs/Environment/Nature","Audio","Scripts/Interaction"}) Directory.CreateDirectory(R+"/"+dir);
            AssetDatabase.Refresh();
            terrain=Object.FindAnyObjectByType<Terrain>();
            if(terrain==null)throw new InvalidOperationException("Terrain requerido");
            UpgradePlayerAndIntro();
            BuildForest();
            UpgradeAtmosphere();
            BuildJourney();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Physics.SyncTransforms();
            Debug.Log("Santuario renovado: bosque Polytope, cielo AllSky, cámara restaurada, Timeline enlazado, tres memorias y audio.");
        }

        [MenuItem("PA3/21 - Reparar camaras y entrada")]
        public static void RepairCameras()
        {
            PA3Workspace.RequireEditMode();
            terrain=Object.FindAnyObjectByType<Terrain>();
            UpgradePlayerAndIntro();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Cámaras independientes: introducción con Timeline; jugador a 1,75 m sobre el suelo.");
        }

        static void UpgradePlayerAndIntro()
        {
            var player=Object.FindAnyObjectByType<FirstPersonController>();
            if(player==null)throw new InvalidOperationException("Falta controlador");
            player.transform.SetParent(null,true);
            player.transform.position=new Vector3(0,Ground(0,-33)+1.1f,-33);
            player.transform.rotation=Quaternion.identity;
            player.gameObject.tag="Player";
            player.playerCamera.transform.localPosition=Vector3.zero;
            player.playerCamera.transform.localRotation=Quaternion.identity;
            player.joint.localPosition=new Vector3(0,.75f,0);
            player.joint.localRotation=Quaternion.identity;
            player.playerCamera.fieldOfView=70;
            player.playerCamera.nearClipPlane=.08f;
            player.playerCamera.farClipPlane=500;
            player.playerCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            player.fov=70;player.mouseSensitivity=1.4f;player.enableHeadBob=false;
            player.enableZoom=false;player.useSprintBar=false;player.crosshair=false;
            player.walkSpeed=4;player.sprintSpeed=6;player.unlimitedSprint=true;
            player.playerCanMove=true;player.cameraCanMove=true;player.enableJump=true;
            var rb=player.GetComponent<Rigidbody>();rb.interpolation=RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            rb.constraints=RigidbodyConstraints.FreezeRotation;
            var capsule=player.GetComponent<CapsuleCollider>();capsule.radius=.32f;
            var friction=new PhysicsMaterial("Player_NoFriction") {dynamicFriction=0,staticFriction=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounciness=0};
            string frictionPath=R+"/Materials/Player_NoFriction.physicMaterial";
            if(AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(frictionPath)==null)AssetDatabase.CreateAsset(friction,frictionPath);
            else Object.DestroyImmediate(friction);
            capsule.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(frictionPath);
            var brain=player.playerCamera.GetComponent<CinemachineBrain>();
            if(brain==null)brain=player.playerCamera.gameObject.AddComponent<CinemachineBrain>();
            brain.enabled=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.playerCamera);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.playerCamera.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rb);
            PrefabUtility.RecordPrefabInstancePropertyModifications(capsule);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.joint);
            PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
            var intro=Object.FindAnyObjectByType<IntroSequenceController>();
            intro.Player=player.gameObject;
            var outputTransform=intro.transform.Find("IntroOutputCamera");
            var outputObject=outputTransform!=null?outputTransform.gameObject:new GameObject("IntroOutputCamera");
            outputObject.transform.SetParent(intro.transform,false);
            var output=outputObject.GetComponent<Camera>();if(output==null)output=outputObject.AddComponent<Camera>();
            output.enabled=false;output.nearClipPlane=.1f;output.farClipPlane=500;
            output.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var introBrain=outputObject.GetComponent<CinemachineBrain>();if(introBrain==null)introBrain=outputObject.AddComponent<CinemachineBrain>();
            intro.IntroOutput=output;
            intro.IntroCamera.transform.localPosition=new Vector3(8,10,-18);
            intro.IntroCamera.transform.localRotation=Quaternion.LookRotation(new Vector3(0,6,22)-new Vector3(8,10,-18));
            var animator=intro.IntroCamera.GetComponent<Animator>();
            if(animator==null)animator=intro.IntroCamera.gameObject.AddComponent<Animator>();
            animator.applyRootMotion=false;
            foreach(var track in ((TimelineAsset)intro.Director.playableAsset).GetOutputTracks())
            {
                if(track is CinemachineTrack)intro.Director.SetGenericBinding(track,introBrain);
                if(track is AnimationTrack animationTrack)
                {
                    animationTrack.trackOffset=TrackOffset.ApplyTransformOffsets;
                    animationTrack.position=Vector3.zero;animationTrack.rotation=Quaternion.identity;
                    foreach(var clip in animationTrack.GetClips())
                    {
                        var asset=(AnimationPlayableAsset)clip.asset;
                        asset.removeStartOffset=false;
                        EditorUtility.SetDirty(asset);
                    }
                    EditorUtility.SetDirty(animationTrack);
                    intro.Director.SetGenericBinding(track,animator);
                }
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(intro);
            PrefabUtility.RecordPrefabInstancePropertyModifications(intro.Director);
            PrefabUtility.RecordPrefabInstancePropertyModifications(intro.IntroCamera.transform);
        }

        static Material Convert(Material source)
        {
            if(source==null)return null;
            if(materials.TryGetValue(source,out var cached))return cached;
            string path=R+"/Materials/Nature/"+source.name+"_URP.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=source.name+"_URP"};AssetDatabase.CreateAsset(mat,path);}
            Texture tex=null;
            foreach(string prop in new[]{"_BaseTexture","_BaseMap","_MainTex"})if(source.HasProperty(prop)&&source.GetTexture(prop)!=null){tex=source.GetTexture(prop);break;}
            mat.SetTexture("_BaseMap",tex);
            mat.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);
            mat.SetFloat("_Smoothness",.12f);
            bool foliage=source.name.IndexOf("Leaves",StringComparison.OrdinalIgnoreCase)>=0||source.name.Contains("Foliage")||source.name.Contains("Grass")||source.name.Contains("Leaf")||source.name.Contains("Poppy");
            mat.SetFloat("_Cull",foliage?0:2);
            mat.SetFloat("_AlphaClip",foliage?1:0);
            if(foliage){mat.EnableKeyword("_ALPHATEST_ON");mat.SetFloat("_Cutoff",.35f);mat.renderQueue=2450;}
            mat.enableInstancing=true;
            EditorUtility.SetDirty(mat);materials[source]=mat;return mat;
        }

        static GameObject NaturePrefab(string relative)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Nature+relative+".prefab");
            if(source==null)throw new InvalidOperationException("Falta Polytope: "+relative);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(source);
            foreach(var r in g.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
            string path=R+"/Prefabs/Environment/Nature/"+source.name+"_URP.prefab";
            var result=PrefabUtility.SaveAsPrefabAsset(g,path);Object.DestroyImmediate(g);return result;
        }

        static GameObject Place(GameObject prefab,Transform parent,float x,float z,float height,bool solid=false)
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            g.transform.SetParent(parent,false);
            var renderers=g.GetComponentsInChildren<Renderer>(true);
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=height/Mathf.Max(.1f,bounds.size.y);
            g.transform.localScale=Vector3.one*scale;
            g.transform.rotation=Quaternion.Euler(0,Rand(0,360),0);
            g.transform.position=new Vector3(x,Ground(x,z)-bounds.min.y*scale-.035f,z);
            foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            if(solid)
            {
                var collider=g.AddComponent<CapsuleCollider>();collider.radius=.22f/scale;collider.height=height*.65f/scale;collider.center=Vector3.up*collider.height*.5f;
            }
            return g;
        }

        static void BuildForest()
        {
            var old=GameObject.Find("Island_WildlifeAndForest");if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject("Island_WildlifeAndForest");
            var broad=NaturePrefab("Trees/PT_Fruit_Tree_01_green");
            var pine=NaturePrefab("Trees/PT_Pine_Tree_03_green");
            var shrub=NaturePrefab("Shrubs/PT_Generic_Shrub_01_green");
            var rock=NaturePrefab("Rocks/PT_Generic_Rock_01");
            var menhir=NaturePrefab("Rocks/PT_Menhir_Rock_02");
            var grass=NaturePrefab("Plants/PT_Grass_02");
            var flowers=NaturePrefab("Flowers/PT_Poppy_02");
            var logs=NaturePrefab("Trees/PT_Fruit_Tree_01_logs");
            var groups=new Dictionary<string,Transform>();
            foreach(string n in new[]{"Canopy","Undergrowth","Coastline","Meadow","AncientStones"}){var g=new GameObject(n);g.transform.SetParent(root.transform);groups[n]=g.transform;}
            int count=0;
            for(int i=0;i<240&&count<62;i++)
            {
                float x=Rand(-34,34),z=Rand(-32,35);
                if(Ground(x,z)<1.1f||Mathf.Abs(x-PathX(z))<8|| (Mathf.Abs(x)<13&&z>9))continue;
                Place(count%3==0?pine:broad,groups["Canopy"],x,z,Rand(6.5f,11f),true);count++;
            }
            // Foreground trees frame the gate, leaving a clear path through the middle.
            Place(broad,groups["Canopy"],-6,-29,9,true);Place(broad,groups["Canopy"],8,-30,10,true);
            for(int i=0;i<190;i++)
            {
                float x=Rand(-32,32),z=Rand(-34,34);
                if(Ground(x,z)<.9f||Mathf.Abs(x-PathX(z))<4.3f||(Mathf.Abs(x)<10&&z>11))continue;
                Place(i%3==0?rock:shrub,groups["Undergrowth"],x,z,Rand(.6f,1.8f));
            }
            for(int i=0;i<380;i++)
            {
                float z=Rand(-34,12),x=PathX(z)+(i%2==0?-1:1)*Rand(3.9f,11.5f);
                if(Ground(x,z)<1)continue;
                Place(i%7==0?flowers:grass,groups["Meadow"],x,z,Rand(.25f,.7f));
            }
            for(int i=0;i<54;i++)
            {
                float a=i*Mathf.PI*2/54,x=Mathf.Cos(a)*Rand(34,39),z=Mathf.Sin(a)*Rand(36,40);
                Place(rock,groups["Coastline"],x,z,Rand(2,4));
            }
            foreach(float x in new[]{-10f,10f})for(int i=0;i<4;i++)Place(menhir,groups["AncientStones"],x+Rand(-1,1),15+i*4,Rand(2,4));
            Place(logs,groups["Undergrowth"],-7,-9,1.1f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,R+"/Prefabs/Environment/Island_Forest.prefab",InteractionMode.AutomatedAction);
            root.transform.SetParent(GameObject.Find("Environment").transform,true);
        }

        static void UpgradeAtmosphere()
        {
            string skyPath=R+"/Materials/Nature/Sanctuary_Sunset.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(sky==null){sky=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/AllSkyFree/Cold Sunset/Cold Sunset.mat"));AssetDatabase.CreateAsset(sky,skyPath);}
            sky.SetFloat("_Exposure",1.05f);sky.SetFloat("_Rotation",100);
            RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.52f,.62f,.68f);
            RenderSettings.ambientEquatorColor=new Color(.33f,.42f,.42f);
            RenderSettings.ambientGroundColor=new Color(.19f,.23f,.18f);
            RenderSettings.fog=true;RenderSettings.fogDensity=.009f;
            RenderSettings.fogColor=new Color(.44f,.57f,.61f);
            var sun=Object.FindObjectsByType<Light>().First(l=>l.type==LightType.Directional);
            sun.color=new Color(1f,.82f,.58f);sun.intensity=1.5f;sun.transform.rotation=Quaternion.Euler(28,-45,0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sun);PrefabUtility.RecordPrefabInstancePropertyModifications(sun.transform);
            var volume=Object.FindAnyObjectByType<Volume>();
            if(volume.sharedProfile.TryGet<ColorAdjustments>(out var color)){color.saturation.Override(6);color.contrast.Override(8);color.colorFilter.Override(Color.white);color.postExposure.Override(.1f);EditorUtility.SetDirty(color);}
            if(volume.sharedProfile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.6f);EditorUtility.SetDirty(bloom);}
            foreach(string name in new[]{"Stone_Limestone","Stone_Sandstone"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(R+"/Materials/Environment/"+name+".mat");
                mat.SetColor("_BaseColor",name.EndsWith("Limestone")?new Color(.4f,.44f,.39f):new Color(.59f,.53f,.39f));EditorUtility.SetDirty(mat);
            }
        }

        static void BuildJourney()
        {
            var existing=GameObject.Find("Journey");if(existing!=null)Object.DestroyImmediate(existing);
            var journey=new GameObject("Journey").AddComponent<SanctuaryJourney>();
            journey.Player=Object.FindAnyObjectByType<FirstPersonController>();
            journey.Intro=Object.FindAnyObjectByType<IntroSequenceController>();
            // Replace the duplicate decorative relic with one consistent interactive set.
            var decoration=GameObject.Find("Relic_Sanctuary");if(decoration!=null)Object.DestroyImmediate(decoration);
            var interactRoot=GameObject.Find("Interactables");
            foreach(Transform child in interactRoot.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Prefabs/Interactables/Interactable_MysticRelic.prefab");
            var relics=new List<MysticRelicInteraction>();
            var points=new[]{new Vector3(1.5f,0,-19),new Vector3(2,0,0),new Vector3(0,8.38f,23)};
            var clip=CreateChime();
            for(int i=0;i<points.Length;i++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name="Memory_"+(i+1);go.transform.SetParent(interactRoot.transform);
                var p=points[i];if(i<2)p.y=Ground(p.x,p.z);go.transform.position=p;
                var relic=go.GetComponent<MysticRelicInteraction>();relic.Journey=journey;
                relic.FeedbackAudio.clip=clip;relic.FeedbackAudio.maxDistance=18;relic.FeedbackAudio.rolloffMode=AudioRolloffMode.Linear;
                var trigger=go.GetComponent<SphereCollider>();trigger.radius=2.7f;
                var particleMat=AssetDatabase.LoadAssetAtPath<Material>(R+"/Materials/Nature/MemoryParticles.mat");
                if(particleMat==null){particleMat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));particleMat.SetColor("_BaseColor",new Color(.25f,1,1,1));AssetDatabase.CreateAsset(particleMat,R+"/Materials/Nature/MemoryParticles.mat");}
                relic.ActivationVfx.GetComponent<ParticleSystemRenderer>().sharedMaterial=particleMat;
                PrefabUtility.RecordPrefabInstancePropertyModifications(relic);
                PrefabUtility.RecordPrefabInstancePropertyModifications(relic.FeedbackAudio);
                PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
                PrefabUtility.RecordPrefabInstancePropertyModifications(relic.ActivationVfx.GetComponent<ParticleSystemRenderer>());
                relics.Add(relic);
            }
            journey.Relics=relics.ToArray();
            var lightObject=new GameObject("Sanctuary_AwakeningLight");lightObject.transform.SetParent(journey.transform);lightObject.transform.position=new Vector3(0,11,22);
            journey.SanctuaryLight=lightObject.AddComponent<Light>();journey.SanctuaryLight.color=new Color(.15f,.9f,1);journey.SanctuaryLight.range=20;journey.SanctuaryLight.intensity=0;
            // Warm navigation lights provide a clear visual route through the foliage.
            var old=GameObject.Find("Path_Lights");if(old!=null)Object.DestroyImmediate(old);
            var lights=new GameObject("Path_Lights");
            var gold=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            string goldPath=R+"/Materials/Nature/AmberGuide.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(goldPath)!=null){Object.DestroyImmediate(gold);gold=AssetDatabase.LoadAssetAtPath<Material>(goldPath);}else AssetDatabase.CreateAsset(gold,goldPath);
            gold.SetColor("_BaseColor",new Color(1,.58f,.15f));gold.EnableKeyword("_EMISSION");gold.SetColor("_EmissionColor",new Color(3,1.4f,.2f));
            for(int i=0;i<9;i++)
            {
                float z=-29+i*5,x=PathX(z)+(i%2==0?-3.7f:3.7f),y=Ground(x,z);
                var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="AmberGuide_"+i;g.transform.SetParent(lights.transform);g.transform.position=new Vector3(x,y+.65f,z);g.transform.localScale=Vector3.one*.18f;g.GetComponent<Renderer>().sharedMaterial=gold;Object.DestroyImmediate(g.GetComponent<Collider>());
                var l=g.AddComponent<Light>();l.color=new Color(1,.6f,.2f);l.range=3;l.intensity=1.1f;
            }
            PrefabUtility.SaveAsPrefabAssetAndConnect(lights,R+"/Prefabs/Environment/Path_Lights.prefab",InteractionMode.AutomatedAction);
        }

        static AudioClip CreateChime()
        {
            string path=R+"/Audio/Memory_Chime.wav";
            if(!File.Exists(path))
            {
                const int rate=22050,count=rate*2;
                using(var w=new BinaryWriter(File.Create(path)))
                {
                    w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
                    for(int i=0;i<count;i++){float t=i/(float)rate;double sample=(Math.Sin(t*528*2*Math.PI)+.4*Math.Sin(t*792*2*Math.PI)+.2*Math.Sin(t*1056*2*Math.PI))*Math.Exp(-t*2.7)*Math.Min(1,t*40)*.36;w.Write((short)(sample*32767));}
                }
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
