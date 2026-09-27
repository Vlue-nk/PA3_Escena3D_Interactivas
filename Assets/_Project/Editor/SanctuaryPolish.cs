using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.Rendering.Universal;
using PA3.Interaction;
using Object=UnityEngine.Object;
namespace PA3.EditorTools {
public static class SanctuaryPolish {
const string R="Assets/_Project";
[MenuItem("PA3/24 - Refinar interfaz y particulas")]
public static void Apply(){
 PA3Workspace.RequireEditMode();
 foreach(var cam in Object.FindObjectsByType<Camera>()){var data=cam.GetUniversalAdditionalCameraData();data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;data.renderPostProcessing=true;PrefabUtility.RecordPrefabInstancePropertyModifications(data);}
 string prefabPath=R+"/Prefabs/Interactables/Interactable_MysticRelic.prefab";
 var prefab=PrefabUtility.LoadPrefabContents(prefabPath);try{var solid=prefab.GetComponent<CapsuleCollider>();if(solid==null)solid=prefab.AddComponent<CapsuleCollider>();solid.center=new Vector3(0,1.15f,0);solid.height=2.3f;solid.radius=.6f;solid.isTrigger=false;PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
 var journey=Object.FindAnyObjectByType<SanctuaryJourney>();BuildHUD(journey);
 var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
 for(int y=0;y<64;y++)for(int x=0;x<64;x++){float a=Mathf.Pow(Mathf.Clamp01(1-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f),2);tex.SetPixel(x,y,new Color(1,1,1,a));}tex.Apply();
 string path=R+"/Textures/MemorySpark.png";System.IO.File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
 var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
 var mat=AssetDatabase.LoadAssetAtPath<Material>(R+"/Materials/Nature/MemoryParticles.mat");mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetColor("_BaseColor",new Color(.2f,1.5f,1.8f,1));mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",2);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.One);mat.SetFloat("_ZWrite",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
 foreach(var relic in journey.Relics){var ps=relic.ActivationVfx;var main=ps.main;main.startSize=new ParticleSystem.MinMaxCurve(.06f,.16f);main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,1.8f);main.startLifetime=new ParticleSystem.MinMaxCurve(.8f,1.6f);main.startColor=Color.white;main.maxParticles=100;var emission=ps.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,70)});var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.3f,1,1),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});col.color=gradient;PrefabUtility.RecordPrefabInstancePropertyModifications(ps);}
 AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());Debug.Log("Interfaz SDF y particulas suaves aplicadas.");
}
public static void BuildHUD(SanctuaryJourney j){
 var old=GameObject.Find("JourneyHUD");if(old!=null)Object.DestroyImmediate(old);
 var hud=new GameObject("JourneyHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));hud.transform.SetParent(j.transform,false);var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.pixelPerfect=true;
 var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
 Panel(hud.transform,"ObjectiveBacking",new Vector2(.5f,.93f),new Vector2(560,62),new Color(.025f,.05f,.055f,.82f));Panel(hud.transform,"GoldRule",new Vector2(.5f,.884f),new Vector2(230,2),new Color(.76f,.61f,.32f,.9f));
 j.Heading=Label(hud.transform,"Heading",new Vector2(.5f,.93f),new Vector2(550,52),25,new Color(1,.91f,.67f));
 j.Hint=Label(hud.transform,"Interaction",new Vector2(.5f,.2f),new Vector2(680,55),24,Color.white);
 Panel(hud.transform,"ControlBacking",new Vector2(.5f,.045f),new Vector2(1150,44),new Color(.025f,.05f,.055f,.82f));
 j.Controls=Label(hud.transform,"Controls",new Vector2(.5f,.045f),new Vector2(1130,42),17,new Color(.91f,.94f,.93f));
 Panel(hud.transform,"Reticle",new Vector2(.5f,.5f),new Vector2(3,3),new Color(1,1,1,.8f));EditorUtility.SetDirty(j);
}
static TMP_Text Label(Transform parent,string name,Vector2 anchor,Vector2 size,int fontSize,Color color){
 var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);Place(go,anchor,size);var t=go.GetComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(R+"/UI/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");t.fontSize=fontSize;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Overflow;t.fontStyle=FontStyles.Normal;return t;
}
static void Panel(Transform parent,string name,Vector2 anchor,Vector2 size,Color color){var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);Place(g,anchor,size);var im=g.GetComponent<Image>();im.color=color;im.raycastTarget=false;}
static void Place(GameObject g,Vector2 anchor,Vector2 size){var rt=g.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=size;}
}}
