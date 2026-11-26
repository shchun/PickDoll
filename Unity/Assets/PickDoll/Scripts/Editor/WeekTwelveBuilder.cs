using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
namespace PickDoll.Editor {
 public static class WeekTwelveBuilder {
  public const string ScenePath="Assets/PickDoll/Scenes/Week12.unity";
  const string Root="Assets/PickDoll/Art/Week12";
  static Transform art;static Material cream,rose,light,ink,glass;
  public static void Generate(){
   var scene=EditorSceneManager.OpenScene(WeekElevenBuilder.ScenePath);
   Physics.SyncTransforms();var protectedGeometry=GeometrySignature();
   foreach(var path in new[]{"Assets/PickDoll/Art",Root,Root+"/Materials",Root+"/Meshes"})if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
   cream=Mat("Cream",new Color(.9f,.81f,.72f));rose=Mat("Rose",new Color(.72f,.30f,.43f));ink=Mat("Ink",new Color(.085f,.045f,.075f));light=Mat("Warm light",new Color(1,.78f,.52f));light.EnableKeyword("_EMISSION");light.SetColor("_EmissionColor",new Color(1,.65f,.34f)*1.6f);
   glass=Mat("Clear",new Color(.8f,.89f,.93f,.025f),true);
   string[] hidden={"Frame","LED Diffuser","Marquee band","Covered LED line","Pickup LED line","Front fascia left","Front fascia right","Front fascia top","Lower extension side","Front shelf cover","Pickup bay casing","Pickup bay lip","Cabinet Side","Cabinet Back","Cabinet Bottom","Cabinet Front Left","Cabinet Front Right","Cabinet Front Upper","Cabinet Front Base","Exit Trim"};
   foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){if(hidden.Contains(r.name)||r.name.StartsWith("Exit trim"))r.enabled=false;if(r.name.EndsWith("Glass")){r.sharedMaterial=glass;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}}
   foreach(var t in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))if(t.text=="PickDoll"||t.text=="PRIZE OUT"||t.text=="a little win")t.GetComponent<Renderer>().enabled=false;
   art=new GameObject("Week12 Bunny toy house").transform;
   Ring("Front rounded frame",new Vector3(0,1.195f,-.455f),.98f,1.11f,.12f,.011f,cream);
   Ring("Warm continuous diffuser",new Vector3(0,1.195f,-.469f),.98f,1.11f,.12f,.0035f,light);
   Ring("Rear rounded frame",new Vector3(0,1.195f,.455f),.98f,1.11f,.12f,.009f,cream);
   foreach(float x in new[]{-.48f,.48f})Box("Roof side rim",new Vector3(x,1.75f,0),new Vector3(.025f,.035f,.86f),.011f,cream);
   var roof=GameObject.Find("Roof");roof.GetComponent<Renderer>().sharedMaterial=glass;roof.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
   var header=Box("Small raised sign",new Vector3(0,1.79f,-.455f),new Vector3(1.02f,.075f,.09f),.03f,Mat("Sign",new Color(.72f,.30f,.43f),true));
   header.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
   var label=new GameObject("Toy house wordmark").AddComponent<TextMesh>();label.transform.SetParent(art);label.transform.position=new Vector3(0,1.79f,-.505f);label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=70;label.characterSize=.008f;label.text="PickDoll";label.color=new Color(1,.91f,.78f);
   foreach(float s in new[]{-1f,1f}){var ear=Ball("Little bunny ear",new Vector3(s*.25f,1.89f,.22f),new Vector3(.09f,.22f,.045f),cream);ear.transform.rotation=Quaternion.Euler(0,0,-s*12);var inside=Ball("Pink ear inset",new Vector3(s*.25f,1.895f,.197f),new Vector3(.047f,.145f,.01f),rose);inside.transform.rotation=ear.transform.rotation;}
   // Only the outside control shelf changes collision bounds; the playfield and delivery remain intact.
   var deck=GameObject.Find("Control Deck").transform;deck.position=new Vector3(0,.59f,-.50f);deck.localScale=new Vector3(1.04f,.035f,.16f);deck.GetComponent<Renderer>().enabled=false;Box("Shallow rounded controls",deck.position,deck.localScale,.016f,cream);
   Move("Joystick",new Vector3(-.27f,.65f,-.50f),new Vector3(.018f,.035f,.018f));Move("Joystick Cap",new Vector3(-.27f,.684f,-.50f),Vector3.one*.055f);Move("Pick Button",new Vector3(.27f,.625f,-.50f),new Vector3(.10f,.009f,.10f));
   foreach(float s in new[]{-1f,1f})Box("Rounded lower side",new Vector3(s*.505f,.2875f,-.16f),new Vector3(.035f,.51f,1.30f),.016f,cream);
   Box("Rounded rear shell",new Vector3(0,.2875f,.475f),new Vector3(1.04f,.51f,.035f),.016f,cream);
   Box("Right shoulder cap",new Vector3(.265f,.575f,-.65f),new Vector3(.50f,.025f,.30f),.011f,cream);
   Box("Small right cheek",new Vector3(.265f,.2875f,-.80f),new Vector3(.50f,.51f,.035f),.016f,cream);
   Box("Left outlet cheek",new Vector3(-.49f,.2875f,-.80f),new Vector3(.03f,.51f,.035f),.014f,cream);
   Box("Outlet lintel",new Vector3(-.23f,.5575f,-.80f),new Vector3(.49f,.035f,.035f),.016f,cream);
   // A thin open arch frames the existing unobstructed delivery passage.
   var arch=new List<Vector3>{new Vector3(-.475f,.07f,-.824f),new Vector3(-.475f,.43f,-.824f)};
   for(int i=0;i<=12;i++){float a=Mathf.PI-i*Mathf.PI/12;arch.Add(new Vector3(-.23f+Mathf.Cos(a)*.245f,.43f+Mathf.Sin(a)*.10f,-.824f));}arch.Add(new Vector3(.015f,.07f,-.824f));Tube("Outlet arch",arch,false,.009f,rose);
   var tray=GameObject.Find("Pickup Tray");tray.GetComponent<Renderer>().sharedMaterial=cream;
   foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.name.StartsWith("Tray "))r.sharedMaterial=glass;
   var trim=Ring("Thin pickup rim",new Vector3(-.23f,.065f,-1.08f),.63f,.62f,.08f,.007f,rose);trim.transform.Rotate(90,0,0,Space.Self);
   Ball("Mascot face",new Vector3(.26f,.30f,-.827f),new Vector3(.16f,.14f,.018f),cream);
   foreach(float s in new[]{-1f,1f}){Ball("Mascot black ear",new Vector3(.26f+s*.038f,.395f,-.832f),new Vector3(.029f,.10f,.012f),ink);Ball("Mascot eye patch",new Vector3(.26f+s*.042f,.32f,-.839f),new Vector3(.043f,.062f,.008f),ink);Ball("Mascot eye shine",new Vector3(.26f+s*.041f,.325f,-.844f),Vector3.one*.007f,cream);}
   Ball("Mascot pink nose",new Vector3(.26f,.286f,-.842f),new Vector3(.014f,.009f,.006f),rose);
   var platform=Mat("Dusty playfield",new Color(.64f,.33f,.43f));foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.name.StartsWith("Platform"))r.sharedMaterial=platform;
   var orbit=UnityEngine.Object.FindAnyObjectByType<OrbitView>();orbit.target.position=new Vector3(0,.90f,-.08f);orbit.framingHeight=2.4f;orbit.framingWidth=1.65f;orbit.minimumDistance=2.8f;
   var fade=art.gameObject.AddComponent<CabinetViewFade>();fade.orbit=orbit;fade.panels=new[]{header.GetComponent<Renderer>()};fade.label=label;
   var ground=GameObject.Find("Ground");if(ground)ground.GetComponent<Renderer>().sharedMaterial=Mat("Backdrop",new Color(.13f,.085f,.12f));
   foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.sharedMaterial&&r.sharedMaterial.HasProperty("_Surface")&&r.sharedMaterial.GetFloat("_Surface")==1){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;if(r.name!="Small raised sign")r.sharedMaterial=glass;}
   Camera.main.clearFlags=CameraClearFlags.SolidColor;Camera.main.backgroundColor=new Color(.11f,.065f,.105f);RenderSettings.ambientLight=new Color(.30f,.25f,.28f);
   var lamp=GameObject.Find("Cabinet Lamp");if(lamp)lamp.GetComponent<Light>().intensity=1.4f;
   Physics.SyncTransforms();if(protectedGeometry!=GeometrySignature())throw new Exception("Protected claw/playfield/delivery collision geometry changed");
   foreach(var source in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){if(source.type!=LightType.Directional)source.shadows=LightShadows.None;else{source.shadowBias=.1f;source.shadowNormalBias=.3f;}}
   PlayerSettings.bundleVersion="0.12.0";PlayerSettings.Android.bundleVersionCode=16;Physics.SyncTransforms();EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK12_SCENE_READY");
  }
  static void Move(string name,Vector3 p,Vector3 s){var t=GameObject.Find(name).transform;t.position=p;t.localScale=s;}
  static Material Mat(string name,Color color,bool transparent=false){string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.12f);if(transparent){m.SetFloat("_ReceiveShadows",0);m.EnableKeyword("_RECEIVE_SHADOWS_OFF");m.SetFloat("_BlendModePreserveSpecular",0);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");m.SetFloat("_SpecularHighlights",0);m.SetFloat("_EnvironmentReflections",0);}EditorUtility.SetDirty(m);return m;}
  static GameObject MeshObject(string name,Vector3 pos,Mesh mesh,Material mat){string path=Root+"/Meshes/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(art);o.transform.position=pos;o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
  static GameObject Ball(string n,Vector3 p,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=n;o.transform.SetParent(art);o.transform.position=p;o.transform.localScale=s;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=m;return o;}
  static GameObject Box(string n,Vector3 p,Vector3 size,float radius,Material m){var v=new List<Vector3>();var normals=new List<Vector3>();var tri=new List<int>();var half=size*.5f;var inner=half-Vector3.one*Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.99f);int steps=8;foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back}){var u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var w=Vector3.Cross(normal,u);int start=v.Count;for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++){var q=Vector3.Scale(normal+u*(x*2f/steps-1)+w*(y*2f/steps-1),half);var clamped=new Vector3(Mathf.Clamp(q.x,-inner.x,inner.x),Mathf.Clamp(q.y,-inner.y,inner.y),Mathf.Clamp(q.z,-inner.z,inner.z));var d=(q-clamped).normalized;v.Add(clamped+d*(half-inner).x);normals.Add(d);}for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int a=start+y*(steps+1)+x;tri.AddRange(new[]{a,a+1,a+steps+2,a,a+steps+2,a+steps+1});}}var mesh=new Mesh();mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetTriangles(tri,0);mesh.RecalculateBounds();return MeshObject(n,p,mesh,m);}
  static GameObject Ring(string n,Vector3 p,float width,float height,float radius,float tube,Material m){var points=new List<Vector3>();for(int corner=0;corner<4;corner++){float a=corner*90;var center=new Vector3((corner==0||corner==3?1:-1)*(width/2-radius),(corner<2?1:-1)*(height/2-radius),0);for(int i=0;i<=8;i++){float angle=(a+i*90f/8)*Mathf.Deg2Rad;points.Add(center+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*radius);}}var obj=Tube(n,points,true,tube,m);obj.transform.position=p;return obj;}
  static GameObject Tube(string n,List<Vector3> path,bool closed,float radius,Material m){var v=new List<Vector3>();var normals=new List<Vector3>();var tris=new List<int>();int sides=10;for(int i=0;i<path.Count;i++){var tangent=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)]).normalized;if(closed)tangent=(path[(i+1)%path.Count]-path[(i+path.Count-1)%path.Count]).normalized;var side=Vector3.Cross(Vector3.forward,tangent).normalized;for(int k=0;k<sides;k++){float a=k*Mathf.PI*2/sides;var radial=side*Mathf.Cos(a)+Vector3.forward*Mathf.Sin(a);v.Add(path[i]+radial*radius);normals.Add(radial);}}for(int i=0;i<(closed?path.Count:path.Count-1);i++)for(int k=0;k<sides;k++){int a=i*sides+k,b=i*sides+(k+1)%sides,c=((i+1)%path.Count)*sides+(k+1)%sides,d=((i+1)%path.Count)*sides+k;tris.AddRange(new[]{a,b,c,a,c,d});}var mesh=new Mesh();mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return MeshObject(n,Vector3.zero,mesh,m);}
  static string GeometrySignature()=>string.Join("\n",UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>!new[]{"Control Deck","Joystick","Joystick Cap","Pick Button"}.Contains(c.name)).Select(c=>c.name+"|"+c.GetType().Name+"|"+c.bounds.center.ToString("F5")+"|"+c.bounds.size.ToString("F5")+"|"+c.enabled).OrderBy(x=>x));
  public static void Check(){Generate();var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();if(c.prizes.Length!=7||c.prizes.Select(p=>p.GetComponent<PrizeIdentity>().SpeciesId).Distinct().Count()!=7)throw new Exception("Catalog changed");if(art.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Cosmetics obstruct playfield");if(GameObject.Find("Pickup Tray").GetComponent<Collider>()==null)throw new Exception("Missing pickup floor");Debug.Log("PICKDOLL_WEEK12_VALIDATION_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);}
  public static void BuildAll(){BuildWindows();BuildAndroid();}
  public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week12/Windows/PickDoll.exe");
  public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week12/Android/PickDoll-week12.apk");}
  static void Build(BuildTarget target,string path){Generate();var rp=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=rp;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=rp;Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week12 build failed");Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK12_ANDROID_READY":"PICKDOLL_WEEK12_WINDOWS_READY");}
 }
}
