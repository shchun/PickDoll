using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor
{
    public static class WeekFiveBuilder
    {
        public const string ScenePath="Assets/PickDoll/Scenes/Week05.unity";
        [MenuItem("PickDoll/Week 05/Generate Scene")]
        public static void Generate()
        {
            var scene=EditorSceneManager.OpenScene(WeekFourBuilder.ScenePath);
            var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();var root=cycle.transform;
            var pink=Mat("Cabinet");var trim=Mat("Trim");var dark=Mat("Dark");var floor=Mat("Playfield");var metal=Mat("Claw");
            foreach(var name in new[]{"Cabinet","Prize Platform","Prize Hatch","Hatch Lip"}) {
                var child=root.Find(name);if(child)UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            // Preserve play-space dimensions while halving the lower cabinet's 0.95 m height.
            const float shift=.475f;
            foreach(Transform child in root)if(child.position.y>=.95f)child.position-=Vector3.up*shift;
            foreach(var body in cycle.prizes)if(!body.transform.IsChildOf(root))body.transform.position-=Vector3.up*shift;
            if(!cycle.machine.carriage.IsChildOf(root))cycle.machine.carriage.position-=Vector3.up*shift;
            if(!cycle.hub.transform.IsChildOf(root))cycle.hub.transform.position-=Vector3.up*shift;
            foreach(var finger in cycle.fingers)if(!finger.transform.IsChildOf(root))finger.transform.position-=Vector3.up*shift;
            if(!cycle.suspensionAnchor.transform.IsChildOf(root))cycle.suspensionAnchor.transform.position-=Vector3.up*shift;
            cycle.topHeight-=shift;cycle.bottomHeight-=shift;cycle.machine.markerHeight=.641f;
            if(cycle.machine.aimMarker)cycle.machine.aimMarker.position-=Vector3.up*shift;
            var lamp=GameObject.Find("Cabinet Lamp");if(lamp)lamp.transform.position-=Vector3.up*shift;
            // Four rectangles leave a real 0.36 x 0.36 opening; no invisible full platform remains.
            Box(root,"Platform Left",new Vector3(-.4325f,.610f,0),new Vector3(.045f,.05f,.85f),floor);
            Box(root,"Platform Right",new Vector3(.2025f,.610f,0),new Vector3(.505f,.05f,.85f),floor);
            Box(root,"Platform Front",new Vector3(-.23f,.610f,-.4125f),new Vector3(.36f,.05f,.025f),floor);
            Box(root,"Platform Back",new Vector3(-.23f,.610f,.1925f),new Vector3(.36f,.05f,.465f),floor);
            foreach(float x in new[]{-.41f,-.05f})Box(root,"Drop Rim",new Vector3(x,.639f,-.22f),new Vector3(.008f,.008f,.36f),trim,false);
            foreach(float z in new[]{-.40f,-.04f})Box(root,"Drop Rim",new Vector3(-.23f,.639f,z),new Vector3(.36f,.008f,.008f),trim,false);
            foreach(float x in new[]{-.49f,.49f})Box(root,"Cabinet Side",new Vector3(x,.2625f,0),new Vector3(.02f,.475f,.95f),pink);
            Box(root,"Cabinet Back",new Vector3(0,.2625f,.465f),new Vector3(1,.475f,.02f),pink);
            Box(root,"Cabinet Bottom",new Vector3(0,.025f,0),new Vector3(1,.04f,.95f),pink);
            Box(root,"Cabinet Front Left",new Vector3(-.465f,.2625f,-.475f),new Vector3(.07f,.475f,.02f),pink);
            Box(root,"Cabinet Front Right",new Vector3(.23f,.2625f,-.475f),new Vector3(.54f,.475f,.02f),pink);
            Box(root,"Cabinet Front Upper",new Vector3(-.235f,.48f,-.475f),new Vector3(.39f,.04f,.02f),pink);
            Box(root,"Cabinet Front Base",new Vector3(-.235f,.06f,-.475f),new Vector3(.39f,.06f,.02f),pink);
            foreach(float x in new[]{-.43f,-.04f})Box(root,"Exit Trim",new Vector3(x,.275f,-.493f),new Vector3(.018f,.37f,.018f),trim,false);
            Box(root,"Exit Trim",new Vector3(-.235f,.46f,-.493f),new Vector3(.40f,.018f,.018f),trim,false);
            foreach(float x in new[]{-.425f,-.035f})Box(root,"Chute Side",new Vector3(x,.335f,-.245f),new Vector3(.025f,.56f,.50f),dark);
            Box(root,"Chute Back",new Vector3(-.23f,.3425f,-.025f),new Vector3(.39f,.585f,.025f),dark);
            foreach(float x in new[]{-.425f,-.035f})Box(root,"Outlet Side",new Vector3(x,.22f,-.685f),new Vector3(.025f,.32f,.40f),Mat("Glass"));
            var slope=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/PickDoll/Materials/Week05 Chute.physicMaterial");
            if(!slope){slope=new PhysicsMaterial("Week05 Chute");AssetDatabase.CreateAsset(slope,"Assets/PickDoll/Materials/Week05 Chute.physicMaterial");}
            slope.dynamicFriction=.06f;slope.staticFriction=.08f;slope.frictionCombine=PhysicsMaterialCombine.Minimum;slope.bounciness=0;EditorUtility.SetDirty(slope);
            var start=new Vector3(-.23f,.19f,-.055f);var end=new Vector3(-.23f,.065f,-.85f);
            var ramp=Box(root,"Delivery Ramp",(start+end)*.5f,new Vector3(.36f,.035f,Vector3.Distance(start,end)),metal);
            ramp.transform.rotation=Quaternion.LookRotation(end-start,Vector3.up);ramp.GetComponent<Collider>().sharedMaterial=slope;
            var tray=Box(root,"Pickup Tray",new Vector3(-.23f,.04f,-1.08f),new Vector3(.59f,.04f,.58f),dark);
            tray.GetComponent<Collider>().sharedMaterial=slope;
            foreach(float x in new[]{-.535f,.075f})Box(root,"Tray Side",new Vector3(x,.22f,-1.08f),new Vector3(.02f,.32f,.60f),Mat("Glass"));
            Box(root,"Tray Front",new Vector3(-.23f,.22f,-1.38f),new Vector3(.63f,.32f,.02f),Mat("Glass"));
            foreach(float x in new[]{-.414f,-.046f})Box(root,"Transparent Chute Guard",new Vector3(x,.735f,-.22f),new Vector3(.008f,.20f,.376f),Mat("Glass"));
            foreach(float z in new[]{-.404f,-.036f})Box(root,"Transparent Chute Guard",new Vector3(-.23f,.735f,z),new Vector3(.36f,.20f,.008f),Mat("Glass"));
            var obj=new GameObject("Prize Receiver");obj.transform.SetParent(root,false);var receiver=obj.AddComponent<PrizeReceiver>();
            receiver.prizes=cycle.prizes;receiver.entryZone=Zone(obj.transform,"Chute Entry",new Vector3(-.23f,.46f,-.22f),new Vector3(.36f,.36f,.36f));
            receiver.exitZone=Zone(obj.transform,"Final Exit",new Vector3(-.23f,.25f,-.87f),new Vector3(.50f,.50f,.55f));
            var orbit=UnityEngine.Object.FindAnyObjectByType<OrbitView>();orbit.minimumDistance=3.0f;orbit.framingHeight=2.85f;orbit.framingWidth=1.95f;orbit.target.position=new Vector3(0,.77f,-.18f);
            cycle.receiver=receiver;cycle.automaticDelivery=true;cycle.returnSpeed=.48f;
            var slots=new[]{new Vector3(-.20f,.727f,.16f),new Vector3(.17f,.727f,-.16f),new Vector3(.18f,.727f,.16f)};
            for(int i=0;i<cycle.prizes.Length;i++){var prize=cycle.prizes[i];prize.transform.position=slots[i];var identity=prize.gameObject.AddComponent<PrizeIdentity>();identity.displayName=new[]{"Mint Bear","Lilac Bear","Peach Bear"}[i];}
            Physics.SyncTransforms();
            PlayerSettings.bundleVersion="0.5.2";PlayerSettings.Android.bundleVersionCode=7;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK05_SCENE_READY");
        }
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/"+name+".mat");
        private static GameObject Box(Transform root,string name,Vector3 pos,Vector3 size,Material material,bool collision=true)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(root,false);obj.transform.position=pos;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj;
        }
        private static BoxCollider Zone(Transform root,string name,Vector3 pos,Vector3 size){var obj=new GameObject(name);obj.transform.SetParent(root,false);obj.transform.position=pos;var box=obj.AddComponent<BoxCollider>();box.isTrigger=true;box.size=size;return box;}
        [MenuItem("PickDoll/Week 05/Validate Delivery")]
        public static void Check()
        {
            var previous=Physics.simulationMode;var report=new StringBuilder();bool passed=true;
            try {
                Generate();Physics.simulationMode=SimulationMode.Script;
                var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();cycle.Initialize();var receiver=cycle.receiver;int events=0;receiver.Acquired+=_=>events++;
                var solid=Physics.OverlapSphere(new Vector3(-.23f,.610f,-.22f),.045f,~0,QueryTriggerInteraction.Ignore);passed&=solid.Length==0;report.AppendLine("physical_hole_clear="+(solid.Length==0));
                for(int trial=0;trial<3;trial++) {
                    cycle.machine.ResetPosition();for(int i=1;i<cycle.prizes.Length;i++){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
                    var prize=cycle.prizes[0];prize.position=new Vector3(-.23f,.825f,-.22f);prize.rotation=Quaternion.Euler(0,0,trial*90);prize.linearVelocity=Vector3.zero;prize.angularVelocity=Vector3.zero;Physics.SyncTransforms();
                    int count=receiver.Count;Sim(cycle,9);bool delivered=receiver.Count==count+1;passed&=delivered;
                    for(int j=0;j<8;j++)receiver.Probe();passed&=receiver.Count==count+1;
                    report.AppendLine($"gravity_drop orientation={trial*90}: delivered={delivered}; pos={prize.position}; entered={prize.GetComponent<PrizeIdentity>().EnteredChute}; count={receiver.Count}; events={events}");
                }
                passed&=events==3;
                cycle.gripVariationEnabled=false;
                foreach(var slot in new[]{new Vector2(-.20f,.16f),new Vector2(.17f,-.16f),new Vector2(.18f,.16f)}) {
                    cycle.machine.ResetPosition();var aim=cycle.machine.carriage.position;aim.x=slot.x;aim.z=slot.y;cycle.machine.carriage.position=aim;cycle.ResetCycle();
                    for(int i=1;i<cycle.prizes.Length;i++){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
                    var prize=cycle.prizes[0];prize.position=new Vector3(slot.x,.727f,slot.y);Physics.SyncTransforms();Sim(cycle,1.2f);
                    int before=receiver.Count;cycle.Press();Sim(cycle,15);bool success=receiver.Count==before+1&&cycle.Phase==ClawCycle.Stage.Ready;passed&=success;
                    report.AppendLine($"automatic_cycle at={slot}: success={success}; phase={cycle.Phase}; prize={prize.position}; count={receiver.Count}");
                }
                foreach(var prize in cycle.prizes)prize.isKinematic=false;
                cycle.machine.ResetPosition();int stockBefore=receiver.Count;
                for(int i=0;i<cycle.prizes.Length;i++) {
                    var prize=cycle.prizes[i];prize.position=new Vector3(-.23f+(i-1)*.04f,.825f,-.22f);prize.rotation=Quaternion.Euler(0,0,i*90);
                    prize.linearVelocity=Vector3.zero;prize.angularVelocity=Vector3.zero;Physics.SyncTransforms();Sim(cycle,3);
                }
                Sim(cycle,6);bool stacked=receiver.Count==stockBefore+3;
                foreach(var prize in cycle.prizes){stacked&=prize.position.y>.06f&&prize.position.z<-.60f;report.AppendLine($"stack_prize={prize.position}; entered={prize.GetComponent<PrizeIdentity>().EnteredChute}; delivered={prize.GetComponent<PrizeIdentity>().Delivered}");}
                passed&=stacked;report.AppendLine($"three_prizes_retained_in_tray={stacked}; count={receiver.Count}; events={events}");
                Directory.CreateDirectory("Builds/Week05/Checks");File.WriteAllText("Builds/Week05/Checks/editor-delivery.txt",(passed?"PASS\n":"FAIL\n")+report);
                if(!passed)throw new Exception("Delivery tuning required\n"+report);
                EditorSceneManager.OpenScene(ScenePath);Physics.simulationMode=previous;Debug.Log("PICKDOLL_WEEK05_VALIDATION_PASS\n"+report);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }catch(Exception e){Physics.simulationMode=previous;Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
            finally {Physics.simulationMode=previous;}
        }
        private static void Sim(ClawCycle cycle,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.01f);i++){cycle.Step(.01f);Physics.Simulate(.01f);cycle.receiver.Probe();}}
        [MenuItem("PickDoll/Week 05/Build Windows")]
        public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week05/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 05/Build Android")]
        public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week05/Android/PickDoll-week05.apk");}
        private static void Build(BuildTarget target,string path)
        {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;
            Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week05 build failed: "+result.summary.result);
            Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK05_ANDROID_READY":"PICKDOLL_WEEK05_WINDOWS_READY");
        }
    }
}
