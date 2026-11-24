using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor
{
    public static class WeekNineBuilder
    {
        public const string ScenePath="Assets/PickDoll/Scenes/Week09.unity";
        private static Material pink,cream,metal,dark,led;
        private static Mesh rounded;
        [MenuItem("PickDoll/Week 09/Generate Scene")]
        public static void Generate() {
            var scene=EditorSceneManager.OpenScene(WeekEightBuilder.ScenePath);var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();var root=cycle.transform;cycle.receiver.outsideFaceZ=-.825f;
            pink=Mat("Shell",new Color(.64f,.24f,.40f),.35f);cream=Mat("Cream",new Color(.96f,.87f,.79f),.28f);dark=Mat("Plum",new Color(.16f,.09f,.16f),.18f);metal=Mat("Silver",new Color(.58f,.62f,.66f),.55f,.65f);led=Mat("Diffuser",new Color(1,.80f,.86f),.2f);led.EnableKeyword("_EMISSION");led.SetColor("_EmissionColor",new Color(.85f,.25f,.40f));
            rounded=SaveMesh("Rounded casing",RoundBox());
            foreach(Transform child in root) {
                var renderer=child.GetComponent<Renderer>();if(!renderer)continue;
                if(child.name.StartsWith("Cabinet"))renderer.sharedMaterial=pink;
                if(child.name=="Frame"||child.name=="Control Deck")renderer.sharedMaterial=cream;
                if(child.name=="Roof")renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/Week06 Canopy.mat");
                if(child.name=="LED Diffuser")renderer.sharedMaterial=led;
            }
            // Opaque facade hides the internal slope; the open mouth and pickup bay remain clear.
            Box(root,"Front fascia left",new Vector3(-.465f,.25f,-.80f),new Vector3(.07f,.45f,.045f),pink);
            Box(root,"Front fascia right",new Vector3(.24f,.25f,-.80f),new Vector3(.57f,.45f,.045f),pink);
            Box(root,"Front fascia top",new Vector3(-.23f,.475f,-.80f),new Vector3(.40f,.05f,.045f),cream);
            foreach(float x in new[]{-.50f,.50f})Box(root,"Lower extension side",new Vector3(x,.27f,-.64f),new Vector3(.035f,.46f,.35f),pink);
            Box(root,"Front shelf cover",new Vector3(0,.49f,-.65f),new Vector3(1.03f,.035f,.34f),cream);
            foreach(float x in new[]{-.545f,.085f})Box(root,"Pickup bay casing",new Vector3(x,.18f,-1.08f),new Vector3(.035f,.28f,.62f),pink);
            Box(root,"Pickup bay lip",new Vector3(-.23f,.085f,-1.40f),new Vector3(.66f,.07f,.025f),cream);
            Text(root,"PRIZE OUT",new Vector3(.22f,.27f,-.828f),.045f,cream.color);
            Text(root,"a little win",new Vector3(.22f,.18f,-.829f),.025f,new Color(1,.67f,.74f));
            Box(root,"Marquee band",new Vector3(0,1.62f,-.455f),new Vector3(1.00f,.105f,.035f),pink);
            Text(root,"PickDoll",new Vector3(0,1.62f,-.478f),.065f,cream.color);
            var lights=new List<Renderer>();
            foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.name=="LED Diffuser")lights.Add(r);
            foreach(float y in new[]{.51f,1.55f})lights.Add(Box(root,"Covered LED line",new Vector3(0,y,-.458f),new Vector3(.96f,.012f,.016f),led).GetComponent<Renderer>());
            lights.Add(Box(root,"Pickup LED line",new Vector3(-.23f,.447f,-.831f),new Vector3(.37f,.011f,.012f),led).GetComponent<Renderer>());
            // Dress the rail and carriage without adding obstacles to the physical play space.
            foreach(Transform child in root)if(child.name=="X Rail")child.GetComponent<Renderer>().sharedMaterial=cream;
            var carriage=cycle.machine.carriage;
            var cover=Box(carriage,"Carriage cover",Vector3.zero,new Vector3(.19f,.08f,.16f),cream);cover.transform.localPosition=new Vector3(0,.01f,0);
            foreach(var r in cycle.hub.GetComponentsInChildren<Renderer>())r.sharedMaterial=metal;
            var housing=Part(cycle.hub.transform,"Cylinder housing",PrimitiveType.Cylinder,new Vector3(0,.051f,0),new Vector3(.105f,.052f,.105f),metal);
            Part(cycle.hub.transform,"Collar",PrimitiveType.Cylinder,new Vector3(0,.004f,0),new Vector3(.128f,.009f,.128f),pink);
            Part(cycle.hub.transform,"Upper collar",PrimitiveType.Cylinder,new Vector3(0,.099f,0),new Vector3(.107f,.006f,.107f),pink);
            var fingerMesh=SaveMesh("Curved finger",FingerMesh());
            foreach(var joint in cycle.fingers){foreach(var r in joint.GetComponentsInChildren<Renderer>())r.enabled=false;var visual=new GameObject("Polished curved finger",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(joint.transform,false);visual.GetComponent<MeshFilter>().sharedMesh=fingerMesh;visual.GetComponent<Renderer>().sharedMaterial=metal;Part(joint.transform,"Rounded fingertip",PrimitiveType.Sphere,new Vector3(-.105f,-.29f,0)*(2f/3f),new Vector3(.014f,.014f,.035f),metal);}
            var coilObject=new GameObject("Flexible coiled cable",typeof(LineRenderer));var line=coilObject.GetComponent<LineRenderer>();line.positionCount=65;line.useWorldSpace=true;line.widthMultiplier=.003f;line.numCapVertices=3;line.sharedMaterial=dark;
            var cable=coilObject.AddComponent<CoilCableVisual>();cable.top=carriage;cable.bottom=cycle.hub.transform;cable.coil=line;
            var lamp=GameObject.Find("Cabinet Lamp");if(lamp&&lamp.TryGetComponent<Light>(out var bulb))bulb.color=new Color(1,.90f,.84f);
            var feedback=cycle.gameObject.AddComponent<AcquisitionFeedback>();feedback.receiver=cycle.receiver;feedback.ledStrips=lights.ToArray();
            PlayerSettings.bundleVersion="0.9.0";PlayerSettings.Android.bundleVersionCode=11;Physics.SyncTransforms();EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK09_SCENE_READY");
        }
        private static Material Mat(string name,Color c,float smooth,float metallic=0){string path="Assets/PickDoll/Materials/Week09 "+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);EditorUtility.SetDirty(m);return m;}
        private static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material){var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.transform.position=p;obj.transform.localScale=size;obj.GetComponent<MeshFilter>().sharedMesh=rounded;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;}
        private static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Material material){var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;}
        private static void Text(Transform parent,string text,Vector3 p,float size,Color color){var obj=new GameObject(text,typeof(TextMesh));obj.transform.SetParent(parent,false);obj.transform.position=p;var t=obj.GetComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");obj.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;t.text=text;t.fontSize=80;t.characterSize=size*.2f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;}
        private static Mesh SaveMesh(string name,Mesh generated){string folder="Assets/PickDoll/Meshes";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/PickDoll","Meshes");string path=folder+"/Week09 "+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh){EditorUtility.CopySerialized(generated,mesh);UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(mesh);}else{mesh=generated;AssetDatabase.CreateAsset(mesh,path);}return mesh;}
        private static Mesh RoundBox(){var v=new List<Vector3>();var n=new List<Vector3>();var tris=new List<int>();foreach(var normal in new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back}){var u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.5f?Vector3.forward:Vector3.up);var w=Vector3.Cross(normal,u);int start=v.Count;for(int y=0;y<=8;y++)for(int x=0;x<=8;x++){var p=normal*.5f+u*(x/8f-.5f)+w*(y/8f-.5f);var inner=new Vector3(Mathf.Clamp(p.x,-.4f,.4f),Mathf.Clamp(p.y,-.4f,.4f),Mathf.Clamp(p.z,-.4f,.4f));var direction=(p-inner).normalized;v.Add(inner+direction*.1f);n.Add(direction);}for(int y=0;y<8;y++)for(int x=0;x<8;x++){int a=start+y*9+x;tris.AddRange(new[]{a,a+1,a+10,a,a+10,a+9});}}var m=new Mesh();m.SetVertices(v);m.SetNormals(n);m.SetTriangles(tris,0);m.RecalculateBounds();return m;}
        private static Mesh FingerMesh(){var path=new[]{Vector3.zero,new Vector3(-.005f,-.09f,0),new Vector3(-.045f,-.20f,0),new Vector3(-.085f,-.275f,0),new Vector3(-.105f,-.29f,0)}.Select(p=>p*(2f/3f)).ToArray();var vertices=new List<Vector3>();var triangles=new List<int>();for(int j=0;j<path.Length;j++){var tangent=path[Mathf.Min(j+1,path.Length-1)]-path[Mathf.Max(0,j-1)];var side=new Vector3(-tangent.y,tangent.x,0).normalized;for(int k=0;k<12;k++){float a=k*Mathf.PI*2/12;vertices.Add(path[j]+side*Mathf.Cos(a)*(j==4?.0075f:.0055f)+Vector3.forward*Mathf.Sin(a)*(j==4?.018f:.012f));}}for(int j=0;j<path.Length-1;j++)for(int k=0;k<12;k++){int a=j*12+k,b=j*12+(k+1)%12;triangles.AddRange(new[]{a,b+12,a+12,a,b,b+12});}var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        [MenuItem("PickDoll/Week 09/Validate")]
        public static void Check(){try{Generate();var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();if(c.fingers.Sum(f=>f.GetComponentsInChildren<BoxCollider>().Length)!=12)throw new Exception("Finger collision geometry changed");if(!UnityEngine.Object.FindAnyObjectByType<AcquisitionFeedback>())throw new Exception("Feedback missing");Directory.CreateDirectory("Builds/Week09/Checks");File.WriteAllText("Builds/Week09/Checks/editor.txt","PASS\nFeedback configured; 12 existing finger colliders retained; decoration adds no colliders\nPlayer regression required for physical delivery and feedback timing\n");Debug.Log("PICKDOLL_WEEK09_VALIDATION_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
        [MenuItem("PickDoll/Week 09/Build Windows")]public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week09/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 09/Build Android")]public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week09/Android/PickDoll-week09.apk");}
        private static void Build(BuildTarget target,string path){var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week09 build failed");Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK09_ANDROID_READY":"PICKDOLL_WEEK09_WINDOWS_READY");}
    }
    public sealed class PickDollVibrationPermission : IPostGenerateGradleAndroidProject {
        public int callbackOrder=>0;
        public void OnPostGenerateGradleAndroidProject(string path){var file=Path.Combine(path,"src/main/AndroidManifest.xml");var xml=new XmlDocument();xml.Load(file);const string ns="http://schemas.android.com/apk/res/android";foreach(XmlElement node in xml.GetElementsByTagName("uses-permission"))if(node.GetAttribute("name",ns)=="android.permission.VIBRATE")return;var permission=xml.CreateElement("uses-permission");permission.SetAttribute("name",ns,"android.permission.VIBRATE");xml.DocumentElement.AppendChild(permission);xml.Save(file);}
    }
}
