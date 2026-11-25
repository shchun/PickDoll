using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor {
 public static class WeekTenBuilder {
  public const string ScenePath="Assets/PickDoll/Scenes/Week10.unity";
  [MenuItem("PickDoll/Week 10/Generate Scene")]
  public static void Generate(){
   var scene=EditorSceneManager.OpenScene(WeekNineBuilder.ScenePath);
   var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
   c.receiver.pickupFloor=GameObject.Find("Pickup Tray").GetComponent<Collider>();
   var floor=c.receiver.pickupFloor.gameObject.AddComponent<PickupFloorContact>();floor.receiver=c.receiver;
   c.GetComponent<AcquisitionFeedback>().celebration=true;
   var twist=c.hub.gameObject.AddComponent<PassiveTwist>();twist.hub=c.hub;
   var joint=c.hub.GetComponent<ConfigurableJoint>();joint.anchor+=Vector3.right*.003f;
   // Compensate the connected anchor too, retaining the exact saved assembly pose.
   joint.connectedAnchor=c.suspensionAnchor.transform.InverseTransformPoint(c.hub.transform.TransformPoint(joint.anchor));
   var white=Fabric("Ivory",new Color(.97f,.94f,.89f));var black=Fabric("Ink",new Color(.055f,.043f,.065f));var rose=Fabric("Rose",new Color(.73f,.39f,.48f));
   foreach(var body in c.prizes){
    var identity=body.GetComponent<PrizeIdentity>();
    var visual=new GameObject("Plush skin").transform;visual.SetParent(body.transform,false);
    if(identity.SpeciesId=="rabbit"){
     foreach(var r in body.GetComponentsInChildren<Renderer>())r.enabled=false;
     identity.displayName="Patch Bunny";body.name="Patch Bunny";
     Part(visual,"White chest",new Vector3(0,0,-.018f),new Vector3(.145f,.18f,.13f),white);
     Part(visual,"Black rump",new Vector3(0,-.004f,.030f),new Vector3(.145f,.168f,.105f),black);
     Part(visual,"Head",new Vector3(0,.12f,0),Vector3.one*.145f,white);
     foreach(float sign in new[]{-1f,1f}){
      Part(visual,"Eye patch",new Vector3(sign*.040f,.129f,-.054f),new Vector3(.048f,.079f,.032f),black);
      Part(visual,"Eye highlight",new Vector3(sign*.039f,.136f,-.071f),new Vector3(.012f,.015f,.006f),white);
      Part(visual,"Eye pupil",new Vector3(sign*.038f,.134f,-.075f),new Vector3(.006f,.009f,.004f),black);
      Part(visual,"White paw",new Vector3(sign*.052f,-.055f,-.035f),new Vector3(.061f,.058f,.065f),white);
      Part(visual,"Arm",new Vector3(sign*.071f,.004f,-.018f),new Vector3(.034f,.07f,.045f),white);
      var ear=Part(visual,"Black ear",new Vector3(sign*.033f,.217f+(sign<0?-.003f:0),0),new Vector3(.033f,.115f,.034f),black);
      Part(visual,"Inner ear",new Vector3(sign*.033f,.217f,-.016f),new Vector3(.016f,.083f,.006f),rose);
      Part(visual,"Blush",new Vector3(sign*.047f,.100f,-.056f),new Vector3(.019f,.010f,.008f),rose);
     }
     Part(visual,"Nose",new Vector3(0,.105f,-.075f),new Vector3(.012f,.008f,.008f),rose);
     Part(visual,"Mouth",new Vector3(0,.094f,-.071f),new Vector3(.014f,.004f,.006f),black);
     Part(visual,"Tail",new Vector3(0,-.025f,.082f),Vector3.one*.035f,white);
    }else{
     foreach(var r in body.GetComponentsInChildren<Renderer>().ToArray()){
      // Relocate the existing visual meshes only; physical collider transforms stay untouched.
      var copy=new GameObject(r.name,typeof(MeshFilter),typeof(MeshRenderer));copy.transform.SetParent(visual,false);
      copy.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);copy.transform.localScale=r.transform.lossyScale;
      var source=r.GetComponent<MeshFilter>();if(source)copy.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;
      var material=Fabric(r.sharedMaterial.name,r.sharedMaterial.color);copy.GetComponent<Renderer>().sharedMaterial=material;r.enabled=false;
     }
    }
    var seam=new GameObject("Belly stitch",typeof(LineRenderer));seam.transform.SetParent(visual,false);var line=seam.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=17;line.widthMultiplier=.0007f;line.sharedMaterial=rose;
    for(int i=0;i<17;i++)line.SetPosition(i,new Vector3(i%2==0?-.0015f:.0015f,-.025f+i*.0045f,-.076f));
    var soft=body.gameObject.AddComponent<PlushResponse>();soft.visual=visual;
   }
   PlayerSettings.bundleVersion="0.10.0";PlayerSettings.Android.bundleVersionCode=12;
   Physics.SyncTransforms();EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK10_SCENE_READY");
  }
  private static Material Fabric(string name,Color color){var path="Assets/PickDoll/Materials/Week10 "+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.03f);
   string texturePath="Assets/PickDoll/Materials/Week10 Fabric weave.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);if(!texture){texture=new Texture2D(64,64,TextureFormat.RGB24,false);texture.name="Fabric weave";texture.wrapMode=TextureWrapMode.Repeat;var pixels=new Color[4096];var random=new System.Random(10);for(int i=0;i<pixels.Length;i++){float v=.9f+(float)random.NextDouble()*.1f;pixels[i]=new Color(v,v,v);}texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);}m.SetTexture("_BaseMap",texture);m.SetTextureScale("_BaseMap",new Vector2(6,6));EditorUtility.SetDirty(m);return m;}
  private static Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material m){var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;}
  public static void Check(){Generate();var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();if(c.prizes.Length!=7||c.prizes.Select(p=>p.GetComponent<PrizeIdentity>().SpeciesId).Distinct().Count()!=3)throw new Exception("Stock changed");if(c.fingers.Sum(f=>f.GetComponentsInChildren<BoxCollider>().Length)!=12)throw new Exception("Finger geometry changed");Debug.Log("PICKDOLL_WEEK10_VALIDATION_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);}
  public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week10/Windows/PickDoll.exe");
  public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week10/Android/PickDoll-week10.apk");}
  private static void Build(BuildTarget target,string path){var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week10 build failed");Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK10_ANDROID_READY":"PICKDOLL_WEEK10_WINDOWS_READY");}
 }
}
