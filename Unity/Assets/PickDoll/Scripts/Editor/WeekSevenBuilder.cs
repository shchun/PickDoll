using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor
{
    public static class WeekSevenBuilder
    {
        public const string ScenePath="Assets/PickDoll/Scenes/Week07.unity";
        [MenuItem("PickDoll/Week 07/Generate Scene")]
        public static void Generate() {
            var scene=EditorSceneManager.OpenScene(WeekSixBuilder.ScenePath);
            var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();var receiver=cycle.receiver;
            var hud=cycle.GetComponent<WeekSixHud>();hud.compactHeader=true;hud.orbit.headerHeight=0;
            receiver.persistentProgress=true;
            var template=cycle.prizes[0].gameObject;
            var stock=new Rigidbody[7];
            for(int i=0;i<stock.Length;i++)stock[i]=UnityEngine.Object.Instantiate(template).GetComponent<Rigidbody>();
            foreach(var body in cycle.prizes)UnityEngine.Object.DestroyImmediate(body.gameObject);
            string[] ids={"bear","rabbit","penguin"},names={"Mint Bear","Berry Bunny","Sunny Penguin"};
            var colors=new[]{new Color(.31f,.74f,.61f),new Color(.84f,.39f,.57f),new Color(.26f,.36f,.57f)};
            var cream=Material("Week07 Cream",new Color(1,.89f,.72f));var orange=Material("Week07 Orange",new Color(1,.53f,.12f));
            var definitions=new PrizeDefinition[3];
            for(int n=0;n<3;n++) {
                var path=$"Assets/PickDoll/Prizes/{ids[n]}.asset";
                if(!AssetDatabase.IsValidFolder("Assets/PickDoll/Prizes"))AssetDatabase.CreateFolder("Assets/PickDoll","Prizes");
                var def=AssetDatabase.LoadAssetAtPath<PrizeDefinition>(path);
                if(!def){def=ScriptableObject.CreateInstance<PrizeDefinition>();AssetDatabase.CreateAsset(def,path);}
                def.speciesId=ids[n];def.displayName=names[n];def.mass=.1f;def.centerOfMass=new Vector3(0,.045f,0);def.placementRadius=.105f;EditorUtility.SetDirty(def);definitions[n]=def;
            }
            for(int i=0;i<stock.Length;i++) {
                int n=i%3;var body=stock[i];body.name=names[n]+" "+(i+1);
                var identity=body.GetComponent<PrizeIdentity>();identity.definition=definitions[n];identity.displayName=names[n];
                body.mass=definitions[n].mass;body.centerOfMass=definitions[n].centerOfMass;
                var fabric=Material("Week07 "+ids[n],colors[n]);
                foreach(var r in body.GetComponentsInChildren<Renderer>())if(r.name!="Eye")r.sharedMaterial=fabric;
                var ears=body.GetComponentsInChildren<Transform>().Where(t=>t.name=="Ear").ToArray();
                if(n==1)foreach(var ear in ears){ear.localPosition=new Vector3(ear.localPosition.x*.65f,.217f,0);ear.localScale=new Vector3(.033f,.115f,.034f);var c=body.gameObject.AddComponent<CapsuleCollider>();c.center=ear.localPosition;c.radius=.016f;c.height=.115f;c.sharedMaterial=body.GetComponent<Collider>().sharedMaterial;c.contactOffset=.0015f;}
                if(n==2)foreach(var ear in ears)UnityEngine.Object.DestroyImmediate(ear.gameObject);
                Part(body.transform,"Belly",new Vector3(0,-.005f,-.062f),new Vector3(.105f,.115f,.025f),cream);
                if(n==2)Part(body.transform,"Beak",new Vector3(0,.109f,-.074f),new Vector3(.038f,.020f,.035f),orange);
                foreach(float sign in new[]{-1f,1f}) {
                    var arm=Part(body.transform,n==2?"Flipper":"Arm",new Vector3(sign*.082f,.01f,0),new Vector3(.035f,n==2?.105f:.075f,.055f),fabric);
                    arm.localRotation=Quaternion.Euler(0,0,sign*(n==2?28:18));
                }
            }
            cycle.prizes=stock;receiver.prizes=stock;cycle.stock=cycle.gameObject.AddComponent<PrizeStock>();cycle.stock.prizes=stock;cycle.stock.receiver=receiver;
            var slots=PrizeStock.Layout(7,7);for(int i=0;i<7;i++){stock[i].transform.position=new Vector3(slots[i].x,.727f,slots[i].y);stock[i].transform.rotation=Quaternion.identity;}
            // Translate the entire suspension assembly together, never just Rigidbody.position in edit mode.
            var start=receiver.holeCenter;var delta=new Vector3(start.x-cycle.machine.carriage.position.x,0,start.y-cycle.machine.carriage.position.z);
            cycle.machine.carriage.position+=delta;cycle.hub.transform.position+=delta;cycle.suspensionAnchor.transform.position+=delta;
            foreach(var finger in cycle.fingers)finger.transform.position+=delta;
            var marker=cycle.machine.aimMarker.position;marker.x=start.x;marker.z=start.y;cycle.machine.aimMarker.position=marker;
            Physics.SyncTransforms();PlayerSettings.bundleVersion="0.7.0";PlayerSettings.Android.bundleVersionCode=9;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK07_SCENE_READY");
        }
        private static Material Material(string name,Color color) {
            var path="Assets/PickDoll/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(m);return m;
        }
        private static Transform Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material) {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
        }
        [MenuItem("PickDoll/Week 07/Validate")]
        public static void Check() {
            try {
                Generate();var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
                if(cycle.prizes.Length!=7||cycle.prizes.Select(p=>p.GetComponent<PrizeIdentity>().SpeciesId).Distinct().Count()!=3)throw new Exception("Stock/species mismatch");
                if(Vector2.Distance(cycle.machine.Aim,cycle.receiver.holeCenter)>.001f)throw new Exception("Startup not over chute");
                for(int seed=0;seed<500;seed++) {
                    var slots=PrizeStock.Layout(seed,7);
                    for(int i=0;i<7;i++) {
                        if(Mathf.Abs(slots[i].x)>.31f||Mathf.Abs(slots[i].y)>.31f||cycle.receiver.OverHole(slots[i],.08f))throw new Exception("Unsafe slot "+slots[i]);
                        for(int j=0;j<i;j++)if(Vector2.Distance(slots[i],slots[j])<.185f)throw new Exception("Stock overlaps");
                    }
                }
                var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"PickDoll-savecheck-"+Guid.NewGuid().ToString("N"));var path=dir+"/collection.json";
                try {
                    var store=new CollectionStore(path);bool added;
                    if(!store.Commit("a","bear",out added)||!added||!store.Commit("a","bear",out added)||added)throw new Exception("Receipt deduplication");
                    if(!store.Commit("b","rabbit",out added)||!added)throw new Exception("Second save");
                    var reload=new CollectionStore(path);if(reload.Total!=2||reload.CountFor("bear")!=1||reload.CountFor("rabbit")!=1)throw new Exception("Reload counts");
                    File.WriteAllText(path,"{broken");var backup=new CollectionStore(path);if(!backup.RecoveredBackup||backup.Total!=1)throw new Exception("Backup recovery");
                    if(!backup.Commit("c","penguin",out added)||new CollectionStore(path).Total!=2)throw new Exception("Write after recovery");
                    File.WriteAllText(path,"{}");File.WriteAllText(path+".bak","{}");var corrupt=new CollectionStore(path);
                    if(corrupt.Error==null||corrupt.Commit("d","bear",out added)||File.ReadAllText(path)!="{}")throw new Exception("Corrupt files must be preserved");
                }finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
                Directory.CreateDirectory("Builds/Week07/Checks");File.WriteAllText("Builds/Week07/Checks/editor.txt","PASS\n7 toys / 3 definitions\n500 random layouts without overlaps, wall or chute intersection\nStartup over chute\nAtomic receipt persistence, dedup, species totals, reload, backup recovery, corrupt-file preservation\n");
                Debug.Log("PICKDOLL_WEEK07_VALIDATION_PASS");if(Application.isBatchMode)EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
        [MenuItem("PickDoll/Week 07/Build Windows")]public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week07/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 07/Build Android")]public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week07/Android/PickDoll-week07.apk");}
        private static void Build(BuildTarget target,string path) {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;
            Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week07 build failed: "+result.summary.result);Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK07_ANDROID_READY":"PICKDOLL_WEEK07_WINDOWS_READY");
        }
    }
}
