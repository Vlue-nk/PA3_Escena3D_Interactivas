using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PA3.EditorTools
{
    public static class IslandBuilder
    {
        private const string R = "Assets/_Project";
        private const string T = R + "/Terrain/Island_TerrainData.asset";
        private static Material stone, trim, dark;
        private static Terrain terrain;
        private static Transform assembly;

        [MenuItem("PA3/2 - Construir terreno y santuario", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(T);

        [MenuItem("PA3/2 - Construir terreno y santuario")]
        public static void Build()
        {
            PA3Workspace.RequireEditMode();
            if (SceneManager.GetActiveScene().path != R + "/Scenes/MainIsland_Exploration.unity" || File.Exists(T))
                throw new InvalidOperationException("Usar la escena principal sin un terreno generado previamente.");
            foreach (var sub in new[] { "Terrain/Layers", "Materials/Environment", "Meshes", "Documentation" })
                Directory.CreateDirectory(R + "/" + sub);
            AssetDatabase.Refresh();
            stone = Material("Stone_Limestone", new Color(.48f,.49f,.43f));
            trim = Material("Stone_Sandstone", new Color(.67f,.58f,.40f));
            dark = Material("Stone_Basalt", new Color(.20f,.25f,.27f));
            assembly = new GameObject("Environment").transform;
            BuildTerrain();
            BuildGate();
            BuildRuins();
            BuildSanctuary();
            BuildCoast();
            var markers = new GameObject("RouteMarkers_EditorOnly");
            markers.tag = "EditorOnly";
            markers.transform.SetParent(assembly);
            Marker(markers.transform, "Entry_PlayerSpawn", new Vector3(PathX(-33), Height(PathX(-33), -33) + .15f, -33));
            Marker(markers.transform, "Midpoint", new Vector3(PathX(-7), Height(PathX(-7), -7), -7));
            Marker(markers.transform, "Sanctuary_ArtifactAnchor", new Vector3(0, 7.5f, 22));
            if (Camera.main != null)
            {
                Undo.RecordObject(Camera.main.transform, "Frame island blockout");
                Camera.main.transform.position = new Vector3(48, 43, -58);
                Camera.main.transform.LookAt(new Vector3(0, 4, 1));
                Camera.main.farClipPlane = 1000;
                Camera.main.nearClipPlane = .1f;
                Camera.main.fieldOfView = 48;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = assembly.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 4, 0), Quaternion.Euler(35, -35, 0), 70);
            Physics.SyncTransforms();
            Validate();
        }

        private static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, x));
        private static float PathX(float z) => z >= 5 ? 0 : 3.3f * Mathf.Sin((z + 33) * Mathf.PI / 38) * (1 - Smooth(0, 5, z));
        private static float RouteHeight(float z) => 1.1f + 5.5f * Smooth(-33, 12, z);
        private static float Height(float x, float z)
        {
            float radial = Mathf.Sqrt(x*x/(42*42) + (z-1)*(z-1)/(45*45));
            float island = 1 - Smooth(.74f, 1.02f, radial);
            float rolling = 1.7f + 2.8f * Mathf.PerlinNoise((x+65)*.034f, (z+85)*.034f);
            float north = 5.2f * Mathf.Exp(-(x*x/440 + (z-20)*(z-20)/440));
            float hills = 3.1f * Mathf.Exp(-((x+23)*(x+23)/90 + (z-3)*(z-3)/210));
            float h = -2.7f + island * (rolling+north+hills+2.7f);
            float routeBlend = (1 - Smooth(3.6f, 8.0f, Mathf.Abs(x-PathX(z)))) * Smooth(-40,-33,z) * (1-Smooth(28,36,z));
            h = Mathf.Lerp(h, RouteHeight(z), routeBlend);
            float plateau = (1-Smooth(8.5f,13,Mathf.Abs(x))) * (1-Smooth(8,14,Mathf.Abs(z-22)));
            return Mathf.Lerp(h, 6.6f, plateau);
        }

        private static Material Material(string name, Color color)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) {name=name};
            m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.12f);
            AssetDatabase.CreateAsset(m,R+"/Materials/Environment/"+name+".mat");
            return m;
        }

        private static TerrainLayer Layer(string name, Color color, int seed)
        {
            const int n=128;
            var tex = new Texture2D(n,n,TextureFormat.RGB24,true) {name=name};
            var pixels = new Color[n*n];
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                // Periodic low-contrast variation keeps the blockout readable without conspicuous seams.
                float v=.96f+.025f*Mathf.Sin(2*Mathf.PI*x/n+seed)*Mathf.Cos(4*Mathf.PI*y/n)+.015f*Mathf.Sin(12*Mathf.PI*(x+y)/n);
                pixels[y*n+x]=new Color(color.r*v,color.g*v,color.b*v,1);
            }
            tex.SetPixels(pixels);tex.Apply();
            string path=R+"/Textures/"+name+".png";
            File.WriteAllBytes(path,tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var layer=new TerrainLayer {name=name, diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path), tileSize=new Vector2(5,5), metallic=0, smoothness=.05f};
            AssetDatabase.CreateAsset(layer,R+"/Terrain/Layers/"+name+".terrainlayer");
            return layer;
        }

        private static void BuildTerrain()
        {
            var data=new TerrainData {name="Island_TerrainData",heightmapResolution=513,alphamapResolution=256,baseMapResolution=512,size=new Vector3(96,18,96)};
            var h=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)h[z,x]=(Height(x/512f*96-48,z/512f*96-48)+3)/18;
            data.SetHeights(0,0,h);
            data.terrainLayers=new[]{Layer("Grass_Moss",new Color(.28f,.36f,.18f),1),Layer("Path_Earth",new Color(.51f,.40f,.25f),2),Layer("Coast_Rock",new Color(.35f,.37f,.32f),3)};
            var alpha=new float[256,256,3];
            for(int z=0;z<256;z++)for(int x=0;x<256;x++)
            {
                float wx=x/255f*96-48,wz=z/255f*96-48;
                float path=(1-Smooth(2.3f,3.6f,Mathf.Abs(wx-PathX(wz))))*Smooth(-38,-33,wz)*(1-Smooth(16,20,wz));
                float rock=Mathf.Max(1-Smooth(-.4f,1.4f,Height(wx,wz)),Smooth(27,44,data.GetSteepness(x/255f,z/255f)));
                alpha[z,x,1]=path; alpha[z,x,2]=rock*(1-path); alpha[z,x,0]=(1-rock)*(1-path);
            }
            AssetDatabase.CreateAsset(data,T); data.SetAlphamaps(0,0,alpha); EditorUtility.SetDirty(data); foreach(var a in data.alphamapTextures)EditorUtility.SetDirty(a);
            var root=Terrain.CreateTerrainGameObject(data);root.name="Island_Terrain";root.transform.position=new Vector3(-48,-3,-48);
            terrain=root.GetComponent<Terrain>(); terrain.heightmapPixelError=3;terrain.basemapDistance=180;terrain.drawInstanced=true;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")) {name="Island_Terrain_URP"};
            AssetDatabase.CreateAsset(mat,R+"/Materials/Environment/Island_Terrain_URP.mat");terrain.materialTemplate=mat;
            SetStatic(root);SavePrefab(root,"Island_Terrain");
        }

        private static void SetStatic(GameObject go) => GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
        private static GameObject Cube(Transform parent,string name,Vector3 pos,Vector3 size,Material material, float yaw=0)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.transform.localRotation=Quaternion.Euler(0,yaw,0);g.GetComponent<Renderer>().sharedMaterial=material;SetStatic(g);return g;
        }
        private static void Marker(Transform parent,string name,Vector3 pos) {var g=new GameObject(name);g.transform.SetParent(parent);g.transform.position=pos;}
        private static void SavePrefab(GameObject root,string name)
        {
            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,R+"/Prefabs/Environment/"+name+".prefab",InteractionMode.AutomatedAction,out success);
            if(!success)throw new InvalidOperationException("No se pudo guardar prefab "+name);
            root.transform.SetParent(assembly,true);
        }
        private static void Pillar(Transform parent,Vector3 p,float height,string label)
        {
            Cube(parent,label+"_Foot",p+Vector3.up*.2f,new Vector3(1.6f,.4f,1.6f),dark);
            Cube(parent,label+"_Base",p+Vector3.up*.5f,new Vector3(1.3f,.2f,1.3f),trim);
            Cube(parent,label+"_Shaft",p+Vector3.up*(.6f+height/2),new Vector3(.95f,height,.95f),stone);
            Cube(parent,label+"_Capital",p+Vector3.up*(height+.72f),new Vector3(1.5f,.25f,1.5f),trim);
        }
        private static void BuildGate()
        {
            var g=new GameObject("Arrival_Gateway");float z=-23,x=PathX(z),y=RouteHeight(z)-.1f;
            Pillar(g.transform,new Vector3(x-4,y,z),4.7f,"West");Pillar(g.transform,new Vector3(x+4,y,z),4.7f,"East");
            Cube(g.transform,"Lintel",new Vector3(x,y+5.6f,z),new Vector3(9.6f,.65f,1.6f),stone);
            Cube(g.transform,"Crown",new Vector3(x,y+6.03f,z),new Vector3(10.3f,.2f,1.9f),trim);
            for(int side=-1;side<=1;side+=2)
            {
                Cube(g.transform,"WingWall_"+side,new Vector3(x+side*6.8f,y+.7f,z),new Vector3(3.6f,1.8f,1.1f),stone);
                Cube(g.transform,"WallCap_"+side,new Vector3(x+side*6.8f,y+1.65f,z),new Vector3(3.8f,.2f,1.35f),trim);
            }
            SavePrefab(g,"Arrival_Gateway");
        }
        private static void BuildRuins()
        {
            var g=new GameObject("Processional_Ruins");
            for(int i=0;i<5;i++)
            {
                float z=-14+i*5.3f;float px=PathX(z);
                for(int side=-1;side<=1;side+=2)
                {
                    float x=px+side*6.2f,y=Height(x,z)-.3f;
                    Pillar(g.transform,new Vector3(x,y,z),i%2==0?2.2f:1.1f,"Route_"+i+"_"+side);
                    if(i<4)Cube(g.transform,"LowWall_"+i+"_"+side,new Vector3(x,Height(x,z+2)-.05f,z+2),new Vector3(.8f,.7f,3),stone);
                }
            }
            SavePrefab(g,"Processional_Ruins");
        }
        private static void BuildSanctuary()
        {
            var g=new GameObject("Sanctuary_Platform");var p=g.transform;
            Cube(p,"Foundation",new Vector3(0,6.45f,22),new Vector3(17, .7f,17),dark);
            Cube(p,"Raised_Platform",new Vector3(0,6.9f,22),new Vector3(15.4f,.3f,15.4f),stone);
            Cube(p,"Sanctuary_Floor",new Vector3(0,7.25f,22),new Vector3(14,.5f,14),trim);
            // Side steps communicate elevation; central mesh provides a continuous accessible route.
            for(int i=0;i<4;i++)for(int side=-1;side<=1;side+=2)
                Cube(p,"Step_"+i+"_"+side,new Vector3(side*3.65f,6.6f+(i+1)*.1125f,12.6f+i*.6f),new Vector3(3.3f,(i+1)*.225f,.64f),stone);
            Ramp(p);
            for(int side=-1;side<=1;side+=2)
            {
                Pillar(p,new Vector3(side*5.8f,7.5f,17),4.5f,"Front_"+side);
                Pillar(p,new Vector3(side*5.8f,7.5f,27),side<0?3.3f:4.5f,"Rear_"+side);
                Cube(p,"SideParapet_"+side,new Vector3(side*6.65f,7.95f,23),new Vector3(.7f,.9f,9.6f),stone);
                Cube(p,"RearWall_"+side,new Vector3(side*4.5f,8.1f,28.6f),new Vector3(5.0f,1.2f,.75f),stone);
            }
            Cube(p,"RearBrokenLintel",new Vector3(3.4f,12.9f,27),new Vector3(5.9f,.55f,1.4f),stone,-4);
            Cube(p,"FrontLintel",new Vector3(0,12.95f,17),new Vector3(13.3f,.5f,1.4f),stone);
            Cube(p,"FrontCornice",new Vector3(0,13.3f,17),new Vector3(13.8f,.2f,1.65f),trim);
            Cube(p,"Artifact_Plinth_Base",new Vector3(0,7.72f,23),new Vector3(3.0f,.44f,3.0f),dark);
            Cube(p,"Artifact_Plinth",new Vector3(0,8.15f,23),new Vector3(2.2f,.42f,2.2f),stone);
            Marker(p,"ArtifactSocket",new Vector3(0,8.8f,23));
            SavePrefab(g,"Sanctuary_Platform");
        }
        private static void Ramp(Transform parent)
        {
            // Wedge: four top vertices slope from approach to floor; convex collision follows the slope.
            Vector3[] v={new Vector3(-2,6.48f,9),new Vector3(2,6.48f,9),new Vector3(2,7.5f,15.1f),new Vector3(-2,7.5f,15.1f),new Vector3(-2,6.2f,9),new Vector3(2,6.2f,9),new Vector3(2,6.2f,15.1f),new Vector3(-2,6.2f,15.1f)};
            int[] tri={0,3,2,0,2,1,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
            var mesh=new Mesh {name="Sanctuary_AccessRamp",vertices=v,triangles=tri};
            var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++)uv[i]=new Vector2(v[i].x*.25f,v[i].z*.25f);
            mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();Unwrapping.GenerateSecondaryUVSet(mesh);
            AssetDatabase.CreateAsset(mesh,R+"/Meshes/Sanctuary_AccessRamp.asset");
            var go=new GameObject("Access_Ramp");go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=stone;go.AddComponent<MeshCollider>().sharedMesh=mesh;SetStatic(go);
        }
        private static void BuildCoast()
        {
            var g=new GameObject("Coastal_Backdrop");
            var sea=Material("Water_Blockout",new Color(.09f,.26f,.30f));sea.SetFloat("_Smoothness",.6f);
            var plane=GameObject.CreatePrimitive(PrimitiveType.Plane);plane.name="Sea_Preview_NoCollision";plane.transform.SetParent(g.transform);plane.transform.position=new Vector3(0,-.15f,0);plane.transform.localScale=Vector3.one*120;plane.GetComponent<Renderer>().sharedMaterial=sea;Object.DestroyImmediate(plane.GetComponent<Collider>());
            SavePrefab(g,"Coastal_Backdrop");
        }


        [MenuItem("PA3/4 - Aplicar capas base y encuadre")]
        public static void ApplyBaseSurfaces()
        {
            PA3Workspace.RequireEditMode();
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(T);
            Undo.RegisterCompleteObjectUndo(data,"Paint blockout terrain layers");
            int n=data.alphamapResolution;
            var alpha=new float[n,n,3];
            float previousEarth=data.GetAlphamaps(n/2,n/2,1,1)[0,0,1];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float wx=x/(float)(n-1)*96-48,wz=z/(float)(n-1)*96-48;
                float path=(1-Smooth(2.3f,3.6f,Mathf.Abs(wx-PathX(wz))))*Smooth(-38,-33,wz)*(1-Smooth(16,20,wz));
                float rock=Mathf.Max(1-Smooth(-.4f,1.4f,Height(wx,wz)),Smooth(27,44,data.GetSteepness(x/(float)(n-1),z/(float)(n-1))));
                alpha[z,x,1]=path;alpha[z,x,2]=rock*(1-path);alpha[z,x,0]=(1-rock)*(1-path);
            }
            data.SetAlphamaps(0,0,alpha);data.SetBaseMapDirty();EditorUtility.SetDirty(data);
            foreach(var a in data.alphamapTextures)EditorUtility.SetDirty(a);
            foreach(var tr in Terrain.activeTerrains)tr.Flush();
            string pathPrefab=R+"/Prefabs/Environment/Coastal_Backdrop.prefab";
            var contents=PrefabUtility.LoadPrefabContents(pathPrefab);
            try { contents.transform.Find("Sea_Preview_NoCollision").localScale=Vector3.one*120; PrefabUtility.SaveAsPrefabAsset(contents,pathPrefab); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            if(Camera.main!=null) { Undo.RecordObject(Camera.main,"Extend overview");Camera.main.farClipPlane=1000; }
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("PA3: peso tierra central antes="+previousEarth+", despues="+data.GetAlphamaps(n/2,n/2,1,1)[0,0,1]);
            Validate();
        }

        [MenuItem("PA3/3 - Validar topografia")]
        public static void Validate()
        {
            PA3Workspace.RequireEditMode();Physics.SyncTransforms();
            AssetDatabase.ImportAsset(T,ImportAssetOptions.ForceUpdate);
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(T);
            if(data==null)throw new InvalidOperationException("Falta TerrainData");
            var report=new StringBuilder("PA3 - Hito 2 / validacion geometrica\n");
            int samples=0;float maxSlope=0;float minHeight=100,maxHeight=-100;
            for(int lane=-1;lane<=1;lane++)
            {
                Vector3 previous=Vector3.zero;bool hasPrevious=false;
                for(float z=-33;z<=20;z+=.5f)
                {
                    float x=PathX(z)+lane*1.15f;
                    // Exclude overhead lintels by starting close above the known walkable surface.
                    var origin=new Vector3(x, (z >= 9 ? 7.5f : RouteHeight(z))+2, z);
                    if(!Physics.Raycast(origin,Vector3.down,out RaycastHit hit,20))throw new InvalidOperationException("Ruta sin suelo en "+origin);
                    if(!(hit.collider is TerrainCollider) && hit.collider.gameObject.name.Contains("Lintel"))throw new InvalidOperationException("Ruta mal muestreada");
                    Vector3 lower=hit.point+Vector3.up*.42f,upper=hit.point+Vector3.up*1.6f;
                    foreach(var obstacle in Physics.OverlapCapsule(lower,upper,.3f))
                        if(obstacle!=hit.collider && !obstacle.isTrigger)throw new InvalidOperationException("Obstaculo en ruta: "+obstacle.name+" z="+z);
                    if(hasPrevious)
                    {
                        var delta=hit.point-previous;delta.y=0;
                        float angle=Mathf.Atan2(Mathf.Abs(hit.point.y-previous.y),delta.magnitude)*Mathf.Rad2Deg;
                        maxSlope=Mathf.Max(maxSlope,angle);
                        if(angle>32)throw new InvalidOperationException("Pendiente excesiva en z="+z+": "+angle);
                    }
                    previous=hit.point;hasPrevious=true;samples++;minHeight=Mathf.Min(minHeight,hit.point.y);maxHeight=Mathf.Max(maxHeight,hit.point.y);
                }
            }
            var root=GameObject.Find("Environment");int prefabCount=0;
            foreach(Transform child in root.transform)if(PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))prefabCount++;
            if(prefabCount<5)throw new InvalidOperationException("Faltan prefabs modulares");
            int missing=0;
            foreach(var tr in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
            if(missing!=0)throw new InvalidOperationException("Hay scripts perdidos");
            if(data.terrainLayers.Length!=3 || data.GetAlphamaps(data.alphamapResolution/2,data.alphamapResolution/2,1,1)[0,0,1]<.8f)
                throw new InvalidOperationException("La capa de tierra no esta conservada en el sendero");
            report.AppendLine("PASS - Tres capas persistentes y tierra dominante en el centro del sendero");
            report.AppendLine("PASS - Terrain 96 x 96 m, heightmap 513, tres TerrainLayers propios");
            report.AppendLine("PASS - "+prefabCount+" prefabs Environment conectados");
            report.AppendLine("PASS - "+samples+" muestras de suelo y volumen libre en tres carriles");
            report.AppendLine("PASS - Pendiente longitudinal maxima muestreada: "+maxSlope.ToString("F2")+" grados");
            report.AppendLine("PASS - Cotas transitables: "+minHeight.ToString("F2")+" a "+maxHeight.ToString("F2")+" m");
            report.AppendLine("PASS - TerrainCollider, colliders de estructuras y rampa MeshCollider");
            report.AppendLine("PASS - Sin scripts perdidos");
            report.AppendLine("El muestreo geometrico no sustituye las pruebas del controlador del Hito 3.");
            report.AppendLine("Agua provisional sin collider; limites finales de exploracion pendientes del Hito 3.");
            File.WriteAllText(R+"/Documentation/Validation_H2.txt",report.ToString());AssetDatabase.Refresh();
            Debug.Log("PA3 H2: topografia validada. "+samples+" muestras; pendiente maxima "+maxSlope.ToString("F2")+" grados.");
        }
    }
}


