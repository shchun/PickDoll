using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PickDoll.Editor
{
    public static class WeekTwoBuilder
    {
        public const string ScenePath = "Assets/PickDoll/Scenes/Week02.unity";
        [MenuItem("PickDoll/Week 02/Generate Scene")]
        public static void Generate()
        {
            var scene = EditorSceneManager.OpenScene(WeekOneBuilder.ScenePath);
            var machine = UnityEngine.Object.FindFirstObjectByType<MachineController>();
            var oldClaw = machine.claw;
            var target = new GameObject("Claw Yaw Target").transform;
            target.SetParent(machine.carriage, false); target.localPosition = oldClaw.localPosition;
            machine.claw = target;
            UnityEngine.Object.DestroyImmediate(oldClaw.gameObject);
            machine.travelLimit = .18f;
            UnityEngine.Object.FindFirstObjectByType<OrbitView>().controlsHeight = 470;
            var cycle = machine.gameObject.AddComponent<ClawCycle>(); cycle.machine = machine;
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/Claw.mat");
            var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/Dark.mat");
            var rubber = new PhysicsMaterial("Week02 Rubber") { dynamicFriction = .95f, staticFriction = 1f, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Maximum };
            var cloth = new PhysicsMaterial("Week02 Cloth") { dynamicFriction = .75f, staticFriction = .85f, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Average };
            SaveMaterial(rubber, "Week02 Rubber"); SaveMaterial(cloth, "Week02 Cloth");
            var hubObj = new GameObject("Physics Claw Hub", typeof(Rigidbody));
            hubObj.transform.position = new Vector3(0,cycle.topHeight,0);
            cycle.hub = hubObj.GetComponent<Rigidbody>(); cycle.hub.isKinematic = true; cycle.hub.interpolation = RigidbodyInterpolation.Interpolate;
            Part(hubObj.transform, "Hub", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.13f,.018f,.13f), metal, false);
            var hubCollider = hubObj.AddComponent<BoxCollider>(); hubCollider.size = new Vector3(.13f,.036f,.13f);
            cycle.fingers = new HingeJoint[3];
            for(int i=0;i<3;i++)
            {
                var yaw = Quaternion.Euler(0,i*120,0);
                var anchor = yaw * new Vector3(.06f,0,0);
                var obj = new GameObject("Physical Finger "+(i+1), typeof(Rigidbody));
                obj.transform.position = hubObj.transform.position + anchor;
                obj.transform.rotation = yaw * Quaternion.Euler(0,0,35);
                var body = obj.GetComponent<Rigidbody>(); ConfigureBody(body,.035f);
                var arm = Part(obj.transform,"Finger",PrimitiveType.Cube,new Vector3(0,-.145f,0),new Vector3(.022f,.29f,.035f),metal,true);
                var toe = Part(obj.transform,"Rubber Hook",PrimitiveType.Cube,new Vector3(-.04f,-.29f,0),new Vector3(.095f,.028f,.04f),dark,true);
                arm.GetComponent<Collider>().sharedMaterial = rubber; toe.GetComponent<Collider>().sharedMaterial = rubber;
                var joint = obj.AddComponent<HingeJoint>(); joint.connectedBody = cycle.hub;
                joint.autoConfigureConnectedAnchor = false; joint.anchor = Vector3.zero; joint.connectedAnchor = anchor; joint.axis = Vector3.forward;
                joint.useLimits = true; joint.limits = new JointLimits { min = -48, max = 3, contactDistance = 1 };
                joint.enableCollision = false; joint.useSpring = true;
                joint.spring = new JointSpring { spring = cycle.gripStiffness, damper = cycle.gripDamping, targetPosition = 0 };
                cycle.fingers[i] = joint;
            }
            for(int i=0;i<3;i++) for(int j=i+1;j<3;j++)
                foreach(var a in cycle.fingers[i].GetComponentsInChildren<Collider>())
                foreach(var b in cycle.fingers[j].GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(a,b);
            cycle.cable = Part(null,"Winch Cable",PrimitiveType.Cylinder,new Vector3(0,1.96f,0),new Vector3(.009f,.12f,.009f),dark,false).transform;
            var toys = scene.GetRootGameObjects().Where(x=>x.name=="Placeholder Bear").OrderBy(x=>x.transform.position.x).ToArray();
            var slots = new[] { new Vector3(-.16f,1.202f,.11f),new Vector3(0,1.202f,-.14f),new Vector3(.16f,1.202f,.11f) };
            cycle.prizes = new Rigidbody[toys.Length];
            for(int i=0;i<toys.Length;i++)
            {
                var toy = toys[i]; toy.name = "Physics Bear "+(i+1); toy.transform.position = slots[i];
                foreach(var collider in toy.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                var torso = toy.AddComponent<CapsuleCollider>(); torso.radius=.075f; torso.height=.18f;
                var head = toy.AddComponent<SphereCollider>(); head.center = new Vector3(0,.12f,0); head.radius=.0725f;
                foreach(float sign in new[]{-1f,1f}) { var foot=toy.AddComponent<SphereCollider>();foot.center=new Vector3(sign*.052f,-.055f,-.035f);foot.radius=.032f; }
                foreach(var c in toy.GetComponents<Collider>()) { c.sharedMaterial=cloth; c.contactOffset=.0015f; }
                var body = toy.AddComponent<Rigidbody>(); ConfigureBody(body,.1f); body.centerOfMass=new Vector3(0,.045f,0);
                cycle.prizes[i] = body;
            }
            Time.fixedDeltaTime=.01f; Physics.defaultContactOffset=.0015f;
            PlayerSettings.bundleVersion="0.2.0";PlayerSettings.Android.bundleVersionCode=2;
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets(); Debug.Log("PICKDOLL_WEEK02_SCENE_READY");
        }
        private static void SaveMaterial(PhysicsMaterial material,string name)
        {
            string path="Assets/PickDoll/Materials/"+name+".physicMaterial";
            if(AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material,path);
        }
        private static void ConfigureBody(Rigidbody body,float mass)
        {
            body.mass=mass;body.linearDamping=.12f;body.angularDamping=.4f;
            body.solverIterations=30;body.solverVelocityIterations=15;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity=15;
        }
        private static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collision)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;
            if(parent)obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=material;
            if(!collision)UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        [MenuItem("PickDoll/Week 02/Check Physics")]
        public static void Check()
        {
            Generate();
            var cycle=UnityEngine.Object.FindFirstObjectByType<ClawCycle>(); cycle.Initialize();
            // IgnoreCollision is runtime state and must be set again after loading a saved scene.
            RunPhysicsChecks(cycle);
            EditorSceneManager.OpenScene(ScenePath);
        }
        private static void RunPhysicsChecks(ClawCycle cycle)
        {
            var oldMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            Directory.CreateDirectory("Builds/Week02/Checks");
            var report=new System.Text.StringBuilder();int successes=0;
            try
            {
                for(int trial=0;trial<3;trial++)
                {
                    cycle.machine.ResetPosition();cycle.ResetCycle();
                    Sim(cycle,1);
                    var prize=cycle.prizes[trial];
                    var p=cycle.machine.carriage.position;p.x=prize.position.x;p.z=prize.position.z;cycle.machine.carriage.position=p;
                    Sim(cycle,.3f); cycle.Press();Sim(cycle,5.8f);
                    float raised=prize.position.y;
                    report.AppendLine($"trial {trial}: phase={cycle.Phase}, prize={prize.position}, angles={string.Join(",",cycle.fingers.Select(j=>j.angle.ToString("F1")))}");
                    var before=prize.position;
                    for(int step=0;step<80;step++){cycle.machine.Move(new Vector2(trial==2?-1:1,0),.01f,true);cycle.Step(.01f);Physics.Simulate(.01f);}
                    float carried=Vector2.Distance(new Vector2(before.x,before.z),new Vector2(prize.position.x,prize.position.z));
                    bool held=raised>1.48f && prize.position.y>1.45f && carried>.012f;
                    if(held)successes++;
                    cycle.Press();Sim(cycle,1.5f);
                    report.AppendLine($"lift={held}; raisedY={raised:F3}; carried={carried:F3}; releasedY={prize.position.y:F3}");
                }
                report.AppendLine($"ALIGNED_LIFTS {successes}/3");
                File.WriteAllText("Builds/Week02/Checks/physics-checks.txt",report.ToString());
                Debug.Log(report.ToString());
                if(successes<3)throw new Exception("Physical pickup tuning required: "+successes+"/3");
                Debug.Log("PICKDOLL_WEEK02_VALIDATION_PASS");
            }
            finally { Physics.simulationMode=oldMode; }
        }
        private static void Sim(ClawCycle cycle,float seconds)
        {
            for(int step=0;step<Mathf.CeilToInt(seconds/.01f);step++){cycle.Step(.01f);Physics.Simulate(.01f);}
        }
        [MenuItem("PickDoll/Week 02/Build Windows")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64,"Builds/Week02/Windows/PickDoll.exe");
        }
        [MenuItem("PickDoll/Week 02/Build Android")]
        public static void BuildAndroid()
        {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            EditorUserBuildSettings.buildAppBundle=false;AssetDatabase.SaveAssets();
            Build(BuildTarget.Android,"Builds/Week02/Android/PickDoll-week02.apk");
        }
        private static void Build(BuildTarget target,string path)
        {
            if(!File.Exists(ScenePath))Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Week02 build failed: "+report.summary.result);
            Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK02_ANDROID_READY":"PICKDOLL_WEEK02_WINDOWS_READY");
        }
    }
}
