using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor
{
    public static class WeekEightBuilder
    {
        public const string ScenePath="Assets/PickDoll/Scenes/Week08.unity";
        [MenuItem("PickDoll/Week 08/Generate Scene")]
        public static void Generate() {
            var scene=EditorSceneManager.OpenScene(WeekSevenBuilder.ScenePath);
            var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
            cycle.machine.homePosition=cycle.receiver.holeCenter;
            cycle.contactDescent=true;cycle.bottomHeight=.83f;cycle.contactForce=1.2f;cycle.contactDuration=.12f;cycle.hardContactForce=4.5f;cycle.descentTimeout=4.5f;
            cycle.contactProbes=new ClawContactProbe[cycle.fingers.Length+1];cycle.contactProbes[0]=cycle.hub.gameObject.AddComponent<ClawContactProbe>();
            for(int i=0;i<cycle.fingers.Length;i++)cycle.contactProbes[i+1]=cycle.fingers[i].gameObject.AddComponent<ClawContactProbe>();
            cycle.stock.naturalArrangement=true;cycle.stock.ApplyNatural(8);
            // A small bear appearance study; keep tested mass and contact geometry unchanged.
            var blush=Material("Week08 Blush",new Color(.96f,.43f,.53f));var muzzle=Material("Week08 Muzzle",new Color(1,.88f,.70f));var smile=Material("Week08 Smile",new Color(.22f,.12f,.17f));
            foreach(var prize in cycle.prizes.Where(p=>p.GetComponent<PrizeIdentity>().SpeciesId=="bear")) {
                foreach(var eye in prize.GetComponentsInChildren<Transform>().Where(t=>t.name=="Eye")){eye.localScale=Vector3.one*.017f;eye.localPosition+=new Vector3(0,-.003f,-.002f);}
                Part(prize.transform,"Soft muzzle",new Vector3(0,.111f,-.069f),new Vector3(.061f,.038f,.016f),muzzle);
                Part(prize.transform,"Nose",new Vector3(0,.120f,-.080f),new Vector3(.017f,.011f,.008f),smile);
                Part(prize.transform,"Little smile",new Vector3(0,.104f,-.078f),new Vector3(.022f,.006f,.006f),smile);
                foreach(float sign in new[]{-1f,1f})Part(prize.transform,"Rosy cheek",new Vector3(sign*.044f,.117f,-.060f),new Vector3(.021f,.012f,.006f),blush);
            }
            Physics.SyncTransforms();PlayerSettings.bundleVersion="0.8.0";PlayerSettings.Android.bundleVersionCode=10;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK08_SCENE_READY");
        }
        private static Material Material(string name,Color color){var path="Assets/PickDoll/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.10f);EditorUtility.SetDirty(m);return m;}
        private static void Part(Transform parent,string name,Vector3 p,Vector3 s,Material m){var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=s;obj.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());}
        [MenuItem("PickDoll/Week 08/Validate")]
        public static void Check(){try {Generate();var c=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
            if(Vector2.Distance(c.machine.homePosition,c.receiver.holeCenter)>.001f||!c.contactDescent||c.contactProbes.Length!=4)throw new Exception("Home/contact configuration");
            for(int seed=0;seed<100;seed++){var poses=c.stock.NaturalLayout(seed);for(int i=0;i<poses.Length;i++){var b=PrizeStock.BoundsAt(c.prizes[i],poses[i].position,poses[i].rotation);if(b.min.y<.644f||b.min.x<-.431f||b.max.x>.431f||b.min.z<-.431f||b.max.z>.431f)throw new Exception("Unsafe spawn");for(int j=0;j<i;j++)if(b.Intersects(PrizeStock.BoundsAt(c.prizes[j],poses[j].position,poses[j].rotation)))throw new Exception("Spawn penetration");}}
            Directory.CreateDirectory("Builds/Week08/Checks");File.WriteAllText("Builds/Week08/Checks/editor.txt","PASS\n100 natural layouts: 7 prizes, initial bounds do not interpenetrate or cross cabinet/floor\nHome over chute / contact probes configured\nPhysical contact, settling, grip and exit are verified in the player\n");Debug.Log("PICKDOLL_WEEK08_VALIDATION_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}}
        [MenuItem("PickDoll/Week 08/Build Windows")]public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week08/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 08/Build Android")]public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week08/Android/PickDoll-week08.apk");}
        private static void Build(BuildTarget target,string path){var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week08 build failed");Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK08_ANDROID_READY":"PICKDOLL_WEEK08_WINDOWS_READY");}
    }
}
