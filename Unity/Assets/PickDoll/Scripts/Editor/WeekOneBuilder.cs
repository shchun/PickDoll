using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PickDoll.Editor
{
    public static class WeekOneBuilder
    {
        public const string ScenePath = "Assets/PickDoll/Scenes/Week01.unity";
        private static string materialPath = "Assets/PickDoll/Materials/";
        [MenuItem("PickDoll/Generate Week 01 Scene")]
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/PickDoll/Scenes");
            Directory.CreateDirectory(materialPath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.25f,.22f,.3f);
            RenderSettings.skybox = null;
            var pink = Material("Cabinet",new Color(.58f,.16f,.32f),.25f);
            var trim = Material("Trim",new Color(.92f,.52f,.68f),.3f);
            var floor = Material("Playfield",new Color(.77f,.54f,.63f),.15f);
            var dark = Material("Dark",new Color(.09f,.065f,.12f),.1f);
            var metal = Material("Claw",new Color(.7f,.72f,.75f),.75f,.65f);
            var led = Material("Diffused LED",new Color(.93f,.78f,.88f),.25f,0,new Color(1,.36f,.64f));
            var marker = Material("Aim",new Color(.69f,1,.18f),.1f,0,new Color(.28f,.4f,.05f));
            var border = Material("Aim Border",new Color(.025f,.065f,.035f),.1f);
            var glass = Material("Glass",new Color(.72f,.85f,.94f,.07f),.85f);
            glass.SetFloat("_Surface",1); glass.SetFloat("_Blend",0);
            glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite",0); glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.renderQueue=3000;
            Box("Ground",new Vector3(0,-.04f,0),new Vector3(200,.05f,200),dark);
            var root = new GameObject("PickDoll Machine");
            Box("Cabinet",new Vector3(0,.5f,0),new Vector3(1,.95f,.95f),pink,root.transform);
            Box("Control Deck",new Vector3(0,.98f,-.57f),new Vector3(1.05f,.12f,.29f),trim,root.transform);
            Box("Prize Platform",new Vector3(0,1.085f,0),new Vector3(.91f,.05f,.85f),floor,root.transform);
            Box("Roof",new Vector3(0,2.24f,0),new Vector3(1.05f,.13f,1),pink,root.transform);
            foreach(float x in new[]{-.48f,.48f}) foreach(float z in new[]{-.43f,.43f})
            {
                Box("Frame",new Vector3(x,1.65f,z),new Vector3(.045f,1.12f,.045f),trim,root.transform);
                Box("LED Diffuser",new Vector3(x*.96f,1.65f,z*.96f),new Vector3(.014f,1.02f,.014f),led,root.transform);
            }
            Box("Left Glass",new Vector3(-.46f,1.65f,0),new Vector3(.008f,1.06f,.84f),glass,root.transform);
            Box("Right Glass",new Vector3(.46f,1.65f,0),new Vector3(.008f,1.06f,.84f),glass,root.transform);
            Box("Rear Glass",new Vector3(0,1.65f,.43f),new Vector3(.9f,1.06f,.008f),glass,root.transform);
            Box("Front Glass",new Vector3(0,1.65f,-.43f),new Vector3(.9f,1.06f,.008f),glass,root.transform);
            Box("Prize Hatch",new Vector3(0,.36f,-.482f),new Vector3(.4f,.27f,.025f),dark,root.transform);
            Box("Hatch Lip",new Vector3(0,.2f,-.55f),new Vector3(.45f,.035f,.2f),trim,root.transform);
            var stick = Primitive("Joystick",PrimitiveType.Cylinder,new Vector3(-.27f,1.09f,-.57f),new Vector3(.025f,.07f,.025f),metal,root.transform);
            Primitive("Joystick Cap",PrimitiveType.Sphere,new Vector3(-.27f,1.17f,-.57f),Vector3.one*.07f,dark,root.transform);
            Primitive("Pick Button",PrimitiveType.Cylinder,new Vector3(.27f,1.06f,-.57f),new Vector3(.12f,.025f,.12f),pink,root.transform);
            var carriage = Box("Carriage",new Vector3(0,2.08f,0),new Vector3(.17f,.07f,.14f),metal,root.transform).transform;
            // Do not scale the rig with the carriage's visual dimensions.
            carriage.SetParent(null); var visual=carriage.gameObject;
            var rig=new GameObject("Carriage Rig").transform; rig.position=carriage.position;
            carriage.SetParent(rig,true);
            Box("X Rail",new Vector3(0,2.14f,0),new Vector3(.9f,.025f,.025f),metal,root.transform);
            var claw=new GameObject("Claw Rig").transform; claw.SetParent(rig,false); claw.localPosition=new Vector3(0,-.25f,0);
            Primitive("Cable",PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(.009f,.12f,.009f),dark,claw,true);
            Primitive("Claw Hub",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.15f,.025f,.15f),metal,claw,true);
            for(int i=0;i<3;i++)
            {
                var pivot=new GameObject("Finger "+i).transform; pivot.SetParent(claw,false); pivot.localRotation=Quaternion.Euler(0,i*120,0);
                var finger=Box("Finger",new Vector3(.07f,-.075f,0),new Vector3(.025f,.15f,.025f),metal,pivot,true);
                finger.transform.localRotation=Quaternion.Euler(0,0,23);
                Box("Soft Tip",new Vector3(.055f,-.145f,0),new Vector3(.05f,.025f,.035f),dark,pivot,true);
            }
            var aim=new GameObject("Stable Aim Marker").transform;
            Ring(aim,.093f,.112f,border); Ring(aim,.098f,.107f,marker,.0003f);
            aim.position=new Vector3(0,1.116f,0);
            CreateToy(new Vector3(-.2f,1.20f,.13f),Material("Toy Mint",new Color(.35f,.68f,.61f),.1f),dark);
            CreateToy(new Vector3(.19f,1.20f,.14f),Material("Toy Peach",new Color(.95f,.58f,.38f),.1f),dark);
            CreateToy(new Vector3(0,1.20f,-.15f),Material("Toy Lilac",new Color(.58f,.45f,.8f),.1f),dark);
            var controller=root.AddComponent<MachineController>(); controller.carriage=rig; controller.claw=claw; controller.aimMarker=aim;
            var target=new GameObject("Camera Target").transform; target.position=new Vector3(0,1.17f,0);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(OrbitView)); cameraObject.tag="MainCamera";
            var camera=cameraObject.GetComponent<Camera>(); camera.backgroundColor=new Color(.08f,.055f,.105f); camera.clearFlags=CameraClearFlags.SolidColor;
            camera.fieldOfView=42; camera.nearClipPlane=.03f; camera.farClipPlane=250;
            var orbit=cameraObject.GetComponent<OrbitView>(); orbit.target=target; orbit.Apply();
            var key=new GameObject("Key Light",typeof(Light)).GetComponent<Light>(); key.type=LightType.Directional; key.intensity=.65f; key.transform.rotation=Quaternion.Euler(45,-25,0); key.shadows=LightShadows.Soft;
            var lamp=new GameObject("Cabinet Lamp",typeof(Light)).GetComponent<Light>(); lamp.type=LightType.Point; lamp.range=2.6f; lamp.intensity=2; lamp.transform.position=new Vector3(0,1.98f,-.2f); lamp.color=new Color(1,.86f,.9f);
            var hud=root.AddComponent<PrototypeHud>(); hud.machine=controller; hud.orbit=orbit;
            PlayerSettings.companyName="Precipi"; PlayerSettings.productName="PickDoll"; PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.precipi.pickdoll");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.bundleVersionCode=1;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("PICKDOLL_WEEK01_SCENE_READY");
        }
        private static Material Material(string name,Color color,float smooth,float metallic=0,Color? emission=null)
        {
            var path=materialPath+name+".mat";
            var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!value){value=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(value,path);}
            value.color=color; value.SetFloat("_Smoothness",smooth); value.SetFloat("_Metallic",metallic);
            if(emission.HasValue){value.EnableKeyword("_EMISSION"); value.SetColor("_EmissionColor",emission.Value);}
            return value;
        }
        private static GameObject Box(string name,Vector3 position,Vector3 size,Material material,Transform parent=null,bool local=false)
            =>Primitive(name,PrimitiveType.Cube,position,size,material,parent,local);
        private static GameObject Primitive(string name,PrimitiveType type,Vector3 position,Vector3 size,Material material,Transform parent=null,bool local=false)
        {
            var obj=GameObject.CreatePrimitive(type); obj.name=name;
            if(parent)obj.transform.SetParent(parent,false);
            if(local)obj.transform.localPosition=position;else obj.transform.position=position;
            obj.transform.localScale=size; obj.GetComponent<Renderer>().sharedMaterial=material; return obj;
        }
        private static void CreateToy(Vector3 position,Material material,Material eyes)
        {
            var root=new GameObject("Placeholder Bear").transform; root.position=position;
            Primitive("Body",PrimitiveType.Sphere,Vector3.zero,new Vector3(.16f,.18f,.14f),material,root,true);
            Primitive("Head",PrimitiveType.Sphere,new Vector3(0,.12f,0),Vector3.one*.145f,material,root,true);
            foreach(float sign in new[]{-1f,1f})
            {
                Primitive("Ear",PrimitiveType.Sphere,new Vector3(sign*.055f,.18f,0),Vector3.one*.055f,material,root,true);
                Primitive("Eye",PrimitiveType.Sphere,new Vector3(sign*.028f,.135f,-.066f),Vector3.one*.013f,eyes,root,true);
                Primitive("Foot",PrimitiveType.Sphere,new Vector3(sign*.052f,-.055f,-.035f),new Vector3(.07f,.055f,.085f),material,root,true);
            }
        }
        private static void Ring(Transform parent,float inner,float outer,Material material,float offset=0)
        {
            const int count=64; var vertices=new Vector3[count*2]; var triangles=new int[count*6];
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count; var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                vertices[i*2]=direction*inner+Vector3.up*offset; vertices[i*2+1]=direction*outer+Vector3.up*offset;
                int next=(i+1)%count*2, start=i*6;
                triangles[start]=i*2;triangles[start+1]=next;triangles[start+2]=i*2+1;
                triangles[start+3]=next;triangles[start+4]=next+1;triangles[start+5]=i*2+1;
            }
            var mesh=new Mesh{name="Aim Ring"}; mesh.vertices=vertices; mesh.triangles=triangles;mesh.RecalculateNormals();
            string path="Assets/PickDoll/Materials/"+(offset>0?"Aim Ring":"Aim Border")+".asset";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh,path);
            var obj=new GameObject("Aim Ring",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        [MenuItem("PickDoll/Build Android APK")]
        public static void BuildAndroid()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android Build Support is not installed.");
            if(!File.Exists(ScenePath))Generate();
            var mobilePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (!mobilePipeline) throw new InvalidOperationException("Mobile URP asset is missing.");
            GraphicsSettings.defaultRenderPipeline = mobilePipeline;
            QualitySettings.SetQualityLevel(0, false); QualitySettings.renderPipeline = mobilePipeline;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds/Android"); EditorUserBuildSettings.buildAppBundle=false;
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/Android/PickDoll-week01.apk",BuildTarget.Android,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed: "+report.summary.result);
            Debug.Log("PICKDOLL_ANDROID_BUILD_READY "+report.summary.totalSize);
        }
        [MenuItem("PickDoll/Build Windows Preview")]
        public static void BuildWindows()
        {
            if(!File.Exists(ScenePath))Generate(); Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/Windows/PickDoll.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            Debug.Log("PICKDOLL_WINDOWS_BUILD_READY");
        }
    }
}
