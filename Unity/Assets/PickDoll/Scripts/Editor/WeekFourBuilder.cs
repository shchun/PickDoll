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
    public static class WeekFourBuilder
    {
        public const string ScenePath="Assets/PickDoll/Scenes/Week04.unity";
        [MenuItem("PickDoll/Week 04/Generate Scene")]
        public static void Generate()
        {
            var scene=EditorSceneManager.OpenScene(WeekThreeBuilder.ScenePath);
            var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
            cycle.smoothMotion=true;cycle.loweringSpeed=.30f;cycle.raisingSpeed=.42f;
            cycle.verticalAcceleration=.9f;cycle.closingRamp=.65f;cycle.closingDuration=.95f;
            cycle.gripStiffness=1.8f;cycle.gripDamping=.085f;cycle.gripVariationEnabled=true;
            cycle.weakGripChance=.25f;cycle.weakGripRange=new Vector2(.4f,.65f);cycle.normalGripRange=new Vector2(.95f,1.05f);
            cycle.machine.speed=.32f;cycle.machine.carrySpeed=.40f;cycle.machine.acceleration=.9f;
            cycle.hub.angularDamping=1.0f;cycle.closeAngle=-10;
            foreach(var finger in cycle.fingers) {
                finger.GetComponent<Rigidbody>().angularDamping=1.2f;
                // A bell-shaped opening stays inside the wall clearance instead of pushing the toy away.
                var points=new[]{Vector3.zero,new Vector3(-.005f,-.09f,0),new Vector3(-.045f,-.20f,0),new Vector3(-.085f,-.275f,0),new Vector3(-.105f,-.29f,0)};
                var segments=finger.GetComponentsInChildren<BoxCollider>();
                for(int i=0;i<segments.Length;i++) {
                    var part=segments[i].transform;var delta=points[i+1]-points[i];
                    part.localPosition=(points[i+1]+points[i])*.5f;part.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
                    part.localScale=new Vector3(i==3?.016f:.012f,delta.magnitude+.004f,i==3?.04f:.026f);
                }
            }
            PlayerSettings.bundleVersion="0.4.0";PlayerSettings.Android.bundleVersionCode=4;
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK04_SCENE_READY");
        }
        public static readonly Vector2[] Slots={Vector2.zero,new Vector2(-.33f,-.33f),new Vector2(.33f,-.33f),new Vector2(-.33f,.33f),new Vector2(.33f,.33f),new Vector2(-.34f,0),new Vector2(.34f,0),new Vector2(0,-.34f),new Vector2(0,.34f)};
        [MenuItem("PickDoll/Week 04/Validate Physics")]
        public static void Check()
        {
            var previous=Physics.simulationMode;var report=new StringBuilder();bool passed=true;
            try {
                Physics.simulationMode=SimulationMode.Script;
                EditorSceneManager.OpenScene(WeekThreeBuilder.ScenePath);
                var baseline=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();baseline.Initialize();
                int before=TrialGrid(baseline,report,"week03_reference");
                Generate();var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();cycle.Initialize();
                cycle.gripVariationEnabled=false;int after=TrialGrid(cycle,report,"week04_normal");
                passed&=after>=before&&after>=6;
                cycle.gripVariationEnabled=true;
                bool ranges=true;int weakCount=0;
                for(int i=0;i<100;i++){float category=(i+.5f)/100;float value=cycle.EvaluateGrip(category,.5f);if(category<cycle.weakGripChance){weakCount++;ranges&=value>=.4f&&value<=.65f;}else ranges&=value>=.95f&&value<=1.05f;}
                passed&=ranges&&weakCount==25;
                report.AppendLine($"grip_distribution_bounds={ranges}; weak_categories={weakCount}/100 (deterministic input grid, not observed win rate)");
                report.AppendLine($"normal_settled_reference={before}/9 -> {after}/9; editor scripted physics, not a runtime win-rate claim");
                Directory.CreateDirectory("Builds/Week04/Checks");File.WriteAllText("Builds/Week04/Checks/editor-physics.txt",(passed?"PASS\n":"FAIL\n")+report);
                if(!passed)throw new Exception("Week04 tuning needs adjustment.\n"+report);
                // Persist the original gameplay flags, not the deterministic test profile.
                EditorSceneManager.OpenScene(ScenePath);
                Physics.simulationMode=previous;
                Debug.Log("PICKDOLL_WEEK04_VALIDATION_PASS\n"+report);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            } catch(Exception e){Physics.simulationMode=previous;Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
            finally {Physics.simulationMode=previous;}
        }
        private static int TrialGrid(ClawCycle cycle,StringBuilder report,string profile)
        {
            int lifts=0;
            for(int i=0;i<Slots.Length;i++) {
                cycle.machine.ResetPosition();var p=cycle.machine.carriage.position;p.x=Slots[i].x;p.z=Slots[i].y;cycle.machine.carriage.position=p;cycle.ResetCycle();
                for(int j=1;j<cycle.prizes.Length;j++){cycle.prizes[j].isKinematic=true;cycle.prizes[j].position=new Vector3(4+j,1.2f,4);}
                var prize=cycle.prizes[0];prize.position=new Vector3(Slots[i].x,1.202f,Slots[i].y);Physics.SyncTransforms();
                Sim(cycle,1.2f);cycle.Press();Sim(cycle,6);
                bool held=prize.position.y>1.45f;if(held)lifts++;
                report.AppendLine($"{profile} slot {i}: lifted={held}; y={prize.position.y:F3}");
                cycle.Press();Sim(cycle,1.7f);if(prize.position.y>1.37f)throw new Exception("OPEN stuck at "+profile+" slot "+i);
            }
            foreach(var prize in cycle.prizes)prize.isKinematic=false;return lifts;
        }
        private static void Sim(ClawCycle cycle,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.01f);i++){cycle.Step(.01f);Physics.Simulate(.01f);}}
        [MenuItem("PickDoll/Week 04/Build Windows")]
        public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week04/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 04/Build Android")]
        public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week04/Android/PickDoll-week04.apk");}
        private static void Build(BuildTarget target,string path)
        {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;
            Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));
            var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week04 build failed: "+result.summary.result);
            Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK04_ANDROID_READY":"PICKDOLL_WEEK04_WINDOWS_READY");
        }
    }
}
