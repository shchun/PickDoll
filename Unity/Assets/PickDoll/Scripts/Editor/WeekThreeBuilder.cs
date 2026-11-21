using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PickDoll.Editor
{
    public static class WeekThreeBuilder
    {
        public const string ScenePath = "Assets/PickDoll/Scenes/Week03.unity";
        [MenuItem("PickDoll/Week 03/Generate Scene")]
        public static void Generate()
        {
            var scene = EditorSceneManager.OpenScene(WeekTwoBuilder.ScenePath);
            var cycle = UnityEngine.Object.FindFirstObjectByType<ClawCycle>();
            cycle.machine.travelLimit = .345f;
            cycle.closeAngle = -20; cycle.releaseAngle = 12;
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/Claw.mat");
            var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/Dark.mat");
            var rubber = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/PickDoll/Materials/Week02 Rubber.physicMaterial");
            foreach (Transform child in cycle.hub.transform) UnityEngine.Object.DestroyImmediate(child.gameObject);
            Visual(cycle.hub.transform,"Solenoid housing",PrimitiveType.Cylinder,new Vector3(0,.015f,0),new Vector3(.105f,.045f,.105f),metal);
            Visual(cycle.hub.transform,"Upper collar",PrimitiveType.Cylinder,new Vector3(0,.067f,0),new Vector3(.058f,.012f,.058f),dark);
            Visual(cycle.hub.transform,"Lower collar",PrimitiveType.Cylinder,new Vector3(0,-.026f,0),new Vector3(.123f,.008f,.123f),dark);
            cycle.hub.GetComponent<BoxCollider>().size = new Vector3(.12f,.08f,.12f);
            var points = new [] { Vector3.zero, new Vector3(.012f,-.09f,0), new Vector3(.008f,-.20f,0), new Vector3(-.014f,-.275f,0), new Vector3(-.075f,-.29f,0) };
            for(int i=0;i<cycle.fingers.Length;i++)
            {
                var finger=cycle.fingers[i];
                while(finger.transform.childCount>0)UnityEngine.Object.DestroyImmediate(finger.transform.GetChild(0).gameObject);
                finger.transform.rotation=Quaternion.Euler(0,i*120,0)*Quaternion.Euler(0,0,18);
                for(int j=0;j<points.Length-1;j++)
                {
                    var delta=points[j+1]-points[j];
                    var part=Visual(finger.transform,j==3?"Rubber curved tip":"Curved steel blade",PrimitiveType.Cube,(points[j+1]+points[j])*.5f,new Vector3(j==3?.016f:.012f,delta.magnitude+.004f,.026f),j==3?dark:metal);
                    part.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
                    part.gameObject.AddComponent<BoxCollider>().sharedMaterial=rubber;
                }
                var pin=Visual(finger.transform,"Hinge pin",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.026f,.019f,.026f),dark);pin.localRotation=Quaternion.Euler(90,0,0);
                var limits=finger.limits;limits.min=-40;limits.max=16;finger.limits=limits;
            }
            cycle.hub.isKinematic=false;cycle.hub.mass=.18f;cycle.hub.useGravity=true;
            cycle.hub.linearDamping=.12f;cycle.hub.angularDamping=.65f;cycle.hub.solverIterations=40;cycle.hub.solverVelocityIterations=20;
            cycle.hub.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            cycle.hub.centerOfMass=new Vector3(0,-.015f,0);
            var anchor=new GameObject("Winch Suspension Anchor",typeof(Rigidbody));
            cycle.suspensionAnchor=anchor.GetComponent<Rigidbody>();cycle.suspensionAnchor.isKinematic=true;
            anchor.transform.position=cycle.hub.position+Vector3.up*cycle.suspensionLength;
            var joint=cycle.hub.gameObject.AddComponent<ConfigurableJoint>();joint.connectedBody=cycle.suspensionAnchor;
            joint.autoConfigureConnectedAnchor=false;joint.anchor=Vector3.up*cycle.suspensionLength;joint.connectedAnchor=Vector3.zero;
            joint.xMotion=joint.yMotion=joint.zMotion=ConfigurableJointMotion.Locked;
            // Joint X is the vertical twist axis; lateral swing is limited independently.
            joint.axis=Vector3.up;joint.secondaryAxis=Vector3.forward;
            joint.angularXMotion=ConfigurableJointMotion.Free;
            joint.angularYMotion=joint.angularZMotion=ConfigurableJointMotion.Limited;
            joint.angularYLimit=joint.angularZLimit=new SoftJointLimit{limit=28};
            joint.enableCollision=false;
            PlayerSettings.bundleVersion="0.3.0";PlayerSettings.Android.bundleVersionCode=3;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK03_SCENE_READY");
        }
        private static Transform Visual(Transform parent,string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=pos;obj.transform.localScale=scale;
            obj.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());return obj.transform;
        }
        [MenuItem("PickDoll/Week 03/Check Setup")]
        public static void Check()
        {
            try {
                Generate();var c=UnityEngine.Object.FindFirstObjectByType<ClawCycle>();
                if(!c.PassiveSuspension||c.hub.isKinematic||c.machine.travelLimit<.34f||c.fingers.Length!=3)throw new Exception("Suspension setup invalid");
                c.Initialize();c.Press();if(!c.CanMove)throw new Exception("Lowering input blocked");
                Debug.Log("PICKDOLL_WEEK03_VALIDATION_PASS");
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
        }
        [MenuItem("PickDoll/Week 03/Build Windows")]
        public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week03/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 03/Build Android")]
        public static void BuildAndroid()
        {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            EditorUserBuildSettings.buildAppBundle=false;AssetDatabase.SaveAssets();Build(BuildTarget.Android,"Builds/Week03/Android/PickDoll-week03.apk");
        }
        private static void Build(BuildTarget target,string path)
        {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;AssetDatabase.SaveAssets();
            Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Week03 build failed: "+report.summary.result);
            Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK03_ANDROID_READY":"PICKDOLL_WEEK03_WINDOWS_READY");
        }
    }
}
