using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor {
 public static class WeekElevenBuilder {
  public const string ScenePath="Assets/PickDoll/Scenes/Week11.unity";
  private static Material ivory,ink,pink,blue,brown,orange,leaf,gray,eye;
  private static PhysicsMaterial friction;
  [MenuItem("PickDoll/Week 11/Generate Scene")]
  public static void Generate(){
   var scene=EditorSceneManager.OpenScene(WeekTenBuilder.ScenePath);var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
   ivory=Mat("Ivory",new Color(.98f,.94f,.86f));ink=Mat("Ink",new Color(.055f,.035f,.065f));pink=Mat("Pink",new Color(.88f,.45f,.59f));blue=Mat("Blue",new Color(.26f,.48f,.78f));brown=Mat("Brown",new Color(.34f,.18f,.11f));orange=Mat("Carrot",new Color(1,.40f,.055f));leaf=Mat("Leaf",new Color(.30f,.64f,.20f));gray=Mat("Gray",new Color(.48f,.46f,.60f));eye=Mat("Glossy eye",new Color(.025f,.018f,.035f));eye.SetFloat("_Smoothness",.85f);eye.SetTexture("_BaseMap",null);
   friction=c.prizes[0].GetComponent<Collider>().sharedMaterial;
   var stock=c.prizes;for(int i=3;i<7;i++){UnityEngine.Object.DestroyImmediate(stock[i].gameObject);stock[i]=Make(i-3);}
   c.prizes=stock;c.receiver.prizes=stock;c.stock.prizes=stock;
   var combo=c.gameObject.AddComponent<ComboTracker>();combo.receiver=c.receiver;
   var book=c.gameObject.AddComponent<CollectionBook>();book.receiver=c.receiver;book.machine=c.machine;book.models=stock;book.combo=combo;book.silhouetteMaterial=Mat("Silhouette",new Color(.035f,.022f,.045f));
   // Long upright rabbits need clearance at the front lip, not an early acquisition trigger.
   var ramp=GameObject.Find("Delivery Ramp").transform;var rampStart=new Vector3(-.23f,.20f,-.055f);var rampEnd=new Vector3(-.23f,.045f,-.85f);ramp.position=(rampStart+rampEnd)*.5f;ramp.rotation=Quaternion.LookRotation(rampEnd-rampStart,Vector3.up);ramp.localScale=new Vector3(.46f,.035f,Vector3.Distance(rampStart,rampEnd));
   const string chutePath="Assets/PickDoll/Materials/Week11 Chute.physicMaterial";var chute=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(chutePath);if(!chute){chute=new PhysicsMaterial("Week11 Chute");AssetDatabase.CreateAsset(chute,chutePath);}chute.staticFriction=.05f;chute.dynamicFriction=.04f;chute.frictionCombine=PhysicsMaterialCombine.Minimum;chute.bounciness=0;EditorUtility.SetDirty(chute);ramp.GetComponent<Collider>().sharedMaterial=chute;
   foreach(var wall in c.GetComponentsInChildren<Collider>())if(wall.name=="Chute Side"||wall.name=="Chute Back"||wall.name=="Outlet Side"){wall.sharedMaterial=chute;if(wall.name!="Chute Back"){var pos=wall.transform.position;pos.x=pos.x<-.23f?-.4725f:.0125f;wall.transform.position=pos;}else{var size=wall.transform.localScale;size.x=.46f;wall.transform.localScale=size;}}
   foreach(var pair in new[]{new[]{"Cabinet Front Upper","0.575"},new[]{"Front fascia top","0.58"},new[]{"Front shelf cover","0.5725"}}){var part=GameObject.Find(pair[0]);if(part){var pos=part.transform.position;pos.y=float.Parse(pair[1],System.Globalization.CultureInfo.InvariantCulture);part.transform.position=pos;}}
   foreach(var prize in stock){var presentation=prize.gameObject.AddComponent<PrizeDeliveryPresentation>();presentation.receiver=c.receiver;presentation.skin=prize.GetComponent<PlushResponse>().visual;}
   var feedback=c.GetComponent<AcquisitionFeedback>();feedback.arcadeStyle=true;var refill=c.gameObject.AddComponent<AutomaticRestock>();refill.cycle=c;refill.feedback=feedback;var audio=c.gameObject.AddComponent<ArcadeSound>();audio.collect=Clip("collect");audio.combo=Clip("combo");audio.refill=Clip("restock");audio.ui=Clip("ui-pop");audio.grab=Clip("grab-boing");audio.clamp=Clip("clamp-tap");audio.release=Clip("release-plop");
   foreach(var name in new[]{"Cabinet Front Left","Front fascia left"}){var part=GameObject.Find(name).transform;var pos=part.position;pos.x=-.485f;part.position=pos;var size=part.localScale;size.x=.03f;part.localScale=size;}
   foreach(var name in new[]{"Cabinet Front Right","Front fascia right"}){var part=GameObject.Find(name).transform;var pos=part.position;pos.x=.25f;part.position=pos;var size=part.localScale;size.x=.50f;part.localScale=size;}
   c.stock.ApplyNatural(11);
   PlayerSettings.bundleVersion="0.11.1";PlayerSettings.Android.bundleVersionCode=14;Physics.SyncTransforms();EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK11_SCENE_READY");
  }
  private static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PickDoll/Audio/"+name+".wav");
  private static Rigidbody Make(int kind){
   string[] ids={"hoodie_otter","carrot_bunny","cream_cushion","patch_cat"};string[] names={"Cocoa Hoodie","Carrot Bunny","Cloud Cushion","Twinkle Cat"};
   var obj=new GameObject(names[kind]);var body=obj.AddComponent<Rigidbody>();body.mass=kind==2?.07f:.09f;body.linearDamping=.3f;body.angularDamping=1.2f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.solverIterations=40;body.solverVelocityIterations=20;
   var definitionPath="Assets/PickDoll/Prizes/"+ids[kind]+".asset";var definition=AssetDatabase.LoadAssetAtPath<PrizeDefinition>(definitionPath);if(!definition){definition=ScriptableObject.CreateInstance<PrizeDefinition>();AssetDatabase.CreateAsset(definition,definitionPath);}definition.speciesId=ids[kind];definition.displayName=names[kind];definition.mass=body.mass;definition.centerOfMass=new Vector3(0,kind==2?.025f:.04f,kind==2?0:-.008f);definition.placementRadius=.10f;EditorUtility.SetDirty(definition);
   body.centerOfMass=definition.centerOfMass;var id=obj.AddComponent<PrizeIdentity>();id.definition=definition;id.displayName=definition.displayName;
   var skin=new GameObject("Plush skin").transform;skin.SetParent(obj.transform,false);var response=obj.AddComponent<PlushResponse>();response.visual=skin;
   if(kind==2){
    Part(skin,"Pillow",new Vector3(0,.04f,0),new Vector3(.17f,.18f,.064f),ivory);var box=obj.AddComponent<BoxCollider>();box.center=new Vector3(0,.04f,0);box.size=new Vector3(.16f,.17f,.06f);
    for(int i=0;i<9;i++){float a=i*Mathf.PI/8;Part(skin,"Scalloped seam",new Vector3(Mathf.Cos(a)*.076f,.04f+Mathf.Sin(a)*.078f,0),new Vector3(.027f,.03f,.069f),ivory);}
    Part(skin,"Blue pocket",new Vector3(0,.012f,-.038f),new Vector3(.077f,.060f,.020f),blue);Eyes(skin,.075f,-.033f,.035f,.008f);
    foreach(float sign in new[]{-1f,1f}){Part(skin,"Little hand",new Vector3(sign*.048f,.006f,-.042f),new Vector3(.025f,.044f,.018f),ivory);Capsule(obj,new Vector3(sign*.048f,.006f,-.040f),.012f,.044f);}
   }else{
    var color=kind==0?brown:kind==1?pink:gray;
    var sole=obj.AddComponent<BoxCollider>();sole.center=new Vector3(0,-.084f,0);sole.size=new Vector3(.105f,.012f,.09f);Part(skin,"Soft flat seat",new Vector3(0,-.076f,0),new Vector3(.11f,.027f,.09f),kind==0?blue:color);
    Part(skin,"Body",Vector3.zero,new Vector3(.145f,.18f,.14f),kind==0?blue:color);Capsule(obj,Vector3.zero,.072f,.18f);
    Part(skin,"Head",new Vector3(0,.112f,0),new Vector3(kind==0?.135f:.15f,.14f,.13f),color);Sphere(obj,new Vector3(0,.112f,0),kind==0?.067f:.071f);
    Part(skin,"Muzzle",new Vector3(0,.088f,-.060f),new Vector3(.075f,.045f,.023f),kind==1?pink:ivory);
    Eyes(skin,.125f,-.061f,kind==2?.035f:.034f,kind==2?.008f:kind==0?.009f:kind==1?.006f:.025f);
    Part(skin,"Nose",new Vector3(0,.103f,-.076f),new Vector3(.014f,.009f,.007f),kind==3?pink:ink);
    Part(skin,"Smile",new Vector3(0,.084f,-.074f),new Vector3(.018f,.004f,.007f),ink);
    foreach(float sign in new[]{-1f,1f}){
     Part(skin,"Foot",new Vector3(sign*.050f,-.055f,-.035f),new Vector3(.060f,.060f,.064f),color);Sphere(obj,new Vector3(sign*.040f,-.060f,-.025f),.024f);
     Part(skin,"Hand",new Vector3(sign*.065f,-.003f,-.025f),new Vector3(.034f,.062f,.040f),kind==0?brown:color);
     if(kind==1){Part(skin,"Long ear",new Vector3(sign*.030f,.208f,0),new Vector3(.029f,.085f,.028f),pink);Capsule(obj,new Vector3(sign*.030f,.208f,0),.014f,.085f);}
     else if(kind==0)Part(skin,"Round ear",new Vector3(sign*.064f,.14f,0),new Vector3(.031f,.036f,.026f),brown);
     else {var ear=Part(skin,"Cat ear",new Vector3(sign*.050f,.182f,0),new Vector3(.035f,.049f,.03f),gray);ear.GetComponent<MeshFilter>().sharedMesh=CatEar();Capsule(obj,new Vector3(sign*.050f,.182f,0),.012f,.04f);ear.localRotation=Quaternion.Euler(0,0,-sign*18);Part(skin,"Inner cat ear",new Vector3(sign*.050f,.184f,-.014f),new Vector3(.018f,.025f,.007f),pink).GetComponent<MeshFilter>().sharedMesh=CatEar();}
    }
    if(kind==0){Part(skin,"White face",new Vector3(0,.087f,-.047f),new Vector3(.122f,.058f,.050f),ivory);Part(skin,"Pink hair roll",new Vector3(0,.187f,-.014f),new Vector3(.037f,.021f,.028f),pink);Part(skin,"Tail",new Vector3(0,-.040f,.070f),new Vector3(.032f,.030f,.07f),brown);Part(skin,"Collar",new Vector3(0,.065f,-.012f),new Vector3(.129f,.03f,.125f),blue);}
    if(kind==1){var carrot=Part(skin,"Carrot",new Vector3(.04f,-.013f,-.058f),new Vector3(.11f,.047f,.046f),orange);carrot.localRotation=Quaternion.Euler(0,0,-20);Capsule(obj,new Vector3(.04f,-.013f,-.058f),.022f,.11f,0);Part(skin,"Carrot leaves",new Vector3(.095f,-.029f,-.058f),new Vector3(.035f,.049f,.042f),leaf);}
    if(kind==3){Part(skin,"White bib",new Vector3(0,.008f,-.054f),new Vector3(.064f,.088f,.023f),ivory);foreach(float sign in new[]{-1f,1f}){Part(skin,"Forehead patch",new Vector3(sign*.039f,.160f,-.046f),new Vector3(.038f,.024f,.016f),ink);Part(skin,"Pink side patch",new Vector3(sign*.065f,.113f,0),new Vector3(.022f,.05f,.072f),pink);Part(skin,"Paw patch",new Vector3(sign*.042f,-.039f,-.04f),new Vector3(.029f,.035f,.026f),ink);}}
   }
   foreach(var collider in obj.GetComponents<Collider>()){collider.sharedMaterial=friction;collider.contactOffset=.0015f;}
   // A thin stitched line, with no extra rigid collision parts.
   var seam=new GameObject("Stitches",typeof(LineRenderer));seam.transform.SetParent(skin,false);var line=seam.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=15;line.widthMultiplier=.0007f;line.sharedMaterial=kind==2?brown:ivory;for(int i=0;i<15;i++)line.SetPosition(i,new Vector3(i%2==0?-.001f:.001f,-.03f+i*.004f,kind==2?-.033f:-.061f));body.ResetInertiaTensor();return body;
  }
  private static Mesh CatEar(){const string path="Assets/PickDoll/Meshes/Week11 Cat ear.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(0,.5f,0),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f)};mesh.triangles=new[]{0,2,1,3,4,2,0,3,2,1,2,4,0,1,4,0,4,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;}
  private static void Eyes(Transform p,float y,float z,float x,float radius){foreach(float sign in new[]{-1f,1f}){if(radius>.02f)Part(p,"Eye rim",new Vector3(sign*x,y,z),new Vector3(radius*2.25f,radius*2.4f,.021f),pink);Part(p,"Eye",new Vector3(sign*x,y,z-.009f),new Vector3(radius*2,radius*2,.019f),radius>.02f?eye:ink);if(radius>.02f)Part(p,"Eye sparkle",new Vector3(sign*x-.006f,y+.008f,z-.019f),new Vector3(.009f,.009f,.004f),ivory);}}
  private static void Sphere(GameObject obj,Vector3 pos,float radius){var c=obj.AddComponent<SphereCollider>();c.center=pos;c.radius=radius;}
  private static void Capsule(GameObject obj,Vector3 pos,float radius,float height,int direction=1){var c=obj.AddComponent<CapsuleCollider>();c.center=pos;c.radius=radius;c.height=height;c.direction=direction;}
  private static Transform Part(Transform p,string name,Vector3 pos,Vector3 scale,Material m){var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(p,false);obj.transform.localPosition=pos;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;}
  private static Material Mat(string name,Color color){var path="Assets/PickDoll/Materials/Week11 "+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.05f);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PickDoll/Materials/Week10 Fabric weave.asset"));m.SetTextureScale("_BaseMap",new Vector2(5,5));EditorUtility.SetDirty(m);return m;}
  public static void Check(){try{Generate();var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();if(c.prizes.Length!=7||c.prizes.Select(p=>p.GetComponent<PrizeIdentity>().SpeciesId).Distinct().Count()!=7)throw new Exception("Catalog mismatch");for(int seed=0;seed<100;seed++)c.stock.NaturalLayout(seed);WeekElevenDiagnostics.ValidateStability();Debug.Log("PICKDOLL_WEEK11_VALIDATION_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
  public static void BuildWindows(){Generate();WeekElevenDiagnostics.ValidateChute();Build(BuildTarget.StandaloneWindows64,"Builds/Week11/Windows/PickDoll.exe");}
  public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week11/Android/PickDoll-week11.apk");}
  private static void Build(BuildTarget target,string path){var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week11 build failed");Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK11_ANDROID_READY":"PICKDOLL_WEEK11_WINDOWS_READY");}
 }
}
