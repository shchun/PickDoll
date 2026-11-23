using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace PickDoll.Editor
{
    public static class WeekSixBuilder
    {
        public const string ScenePath="Assets/PickDoll/Scenes/Week06.unity";
        // Keep tuning inputs together to avoid scattered edits and repeated device builds.
        public const float FingerScale=2f/3f, OpenAngle=45, CloseAngle=-37;
        public const float HubDamping=6, HubLinearDamping=1.5f;
        [MenuItem("PickDoll/Week 06/Generate Scene")]
        public static void Generate()
        {
            var scene=EditorSceneManager.OpenScene(WeekFiveBuilder.ScenePath);
            var cycle=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
            var legacy=cycle.GetComponent<PrototypeHud>();UnityEngine.Object.DestroyImmediate(legacy);
            var hud=cycle.gameObject.AddComponent<WeekSixHud>();hud.machine=cycle.machine;hud.orbit=UnityEngine.Object.FindAnyObjectByType<OrbitView>();
            hud.orbit.pinchZoom=true;hud.orbit.maxZoom=2;hud.orbit.controlsHeight=440;
            cycle.hub.angularDamping=HubDamping;cycle.hub.linearDamping=HubLinearDamping;
            cycle.closeAngle=CloseAngle;cycle.bottomHeight=.965f;cycle.openingTolerance=20;
            cycle.gripStiffness=.70f;cycle.gripDamping=.065f;cycle.closingRamp=.75f;cycle.closingDuration=1.05f;
            var points=new[]{Vector3.zero,new Vector3(-.005f,-.09f,0),new Vector3(-.045f,-.20f,0),new Vector3(-.085f,-.275f,0),new Vector3(-.105f,-.29f,0)};
            for(int n=0;n<cycle.fingers.Length;n++) {
                var previous=cycle.fingers[n];var transform=previous.transform;var anchor=previous.connectedAnchor;
                var limits=previous.limits;var spring=previous.spring;UnityEngine.Object.DestroyImmediate(previous);
                transform.rotation=Quaternion.Euler(0,n*120,0)*Quaternion.Euler(0,0,OpenAngle);
                transform.position=cycle.hub.transform.TransformPoint(anchor);Physics.SyncTransforms();
                var body=transform.GetComponent<Rigidbody>();body.mass=.025f;body.angularDamping=2.0f;
                var finger=transform.gameObject.AddComponent<HingeJoint>();finger.connectedBody=cycle.hub;finger.autoConfigureConnectedAnchor=false;
                finger.anchor=Vector3.zero;finger.connectedAnchor=anchor;finger.axis=Vector3.forward;finger.enableCollision=false;
                limits.min=-58;limits.max=16;finger.limits=limits;finger.useLimits=true;finger.spring=spring;finger.useSpring=true;cycle.fingers[n]=finger;
                var parts=finger.GetComponentsInChildren<BoxCollider>();
                for(int i=0;i<parts.Length;i++) {
                    var a=points[i]*FingerScale;var b=points[i+1]*FingerScale;var delta=b-a;var part=parts[i].transform;
                    part.localPosition=(a+b)*.5f;part.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
                    part.localScale=new Vector3(i==3?.016f:.012f,delta.magnitude+.003f,i==3?.04f:.026f);
                }
            }
            // Leave headroom for the new release orientations under the compact control deck.
            var ramp=cycle.transform.Find("Delivery Ramp");var rampStart=new Vector3(-.23f,.19f,-.055f);var rampEnd=new Vector3(-.23f,.065f,-.85f);
            ramp.position=(rampStart+rampEnd)*.5f;ramp.rotation=Quaternion.LookRotation(rampEnd-rampStart,Vector3.up);ramp.localScale=new Vector3(.36f,.035f,Vector3.Distance(rampStart,rampEnd));
            var deck=cycle.transform.Find("Control Deck");deck.localScale=new Vector3(deck.localScale.x,.07f,deck.localScale.z);deck.position=new Vector3(deck.position.x,.53f,deck.position.z);
            var chuteMaterial=ramp.GetComponent<Collider>().sharedMaterial;
            foreach(Transform t in cycle.transform)if(t.name=="Chute Side"||t.name=="Chute Back"||t.name=="Outlet Side")t.GetComponent<Collider>().sharedMaterial=chuteMaterial;
            var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/PickDoll/Materials/Glass.mat");
            const string canopyPath="Assets/PickDoll/Materials/Week06 Canopy.mat";
            var canopy=AssetDatabase.LoadAssetAtPath<Material>(canopyPath);
            if(!canopy){canopy=new Material(glass);AssetDatabase.CreateAsset(canopy,canopyPath);}
            canopy.SetColor("_BaseColor",new Color(.95f,.65f,.78f,.13f));
            canopy.SetFloat("_BlendModePreserveSpecular",0);canopy.SetFloat("_SpecularHighlights",0);canopy.SetFloat("_EnvironmentReflections",0);canopy.SetFloat("_ReceiveShadows",0);
            canopy.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);canopy.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            canopy.DisableKeyword("_ALPHAPREMULTIPLY_ON");canopy.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");canopy.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");canopy.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(canopy);
            var roof=cycle.transform.Find("Roof").GetComponent<Renderer>();roof.sharedMaterial=canopy;roof.shadowCastingMode=ShadowCastingMode.Off;
            foreach(Transform t in cycle.transform)if(t.name=="Frame")t.localScale=new Vector3(.024f,t.localScale.y,.024f);
            Physics.SyncTransforms();PlayerSettings.bundleVersion="0.6.0";PlayerSettings.Android.bundleVersionCode=8;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();Debug.Log("PICKDOLL_WEEK06_SCENE_READY");
        }
        [MenuItem("PickDoll/Week 06/Validate Delivery")]
        public static void Check()
        {
            var previous=Physics.simulationMode;var report=new StringBuilder();bool passed=true;
            try {
                Generate();Physics.simulationMode=SimulationMode.Script;
                EditorSceneManager.OpenScene(WeekFiveBuilder.ScenePath);var baseline=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
                var oldSpan=OpeningSpan(baseline);var oldDecay=Decay(baseline);EditorSceneManager.OpenScene(ScenePath);var tuned=UnityEngine.Object.FindAnyObjectByType<ClawCycle>();var newSpan=OpeningSpan(tuned);var newDecay=Decay(tuned);
                bool geometry=FingerScale>.66f&&FingerScale<.68f&&newSpan>oldSpan*1.5f;passed&=geometry;report.AppendLine($"shorter_wider_claw={geometry}; length_scale={FingerScale:F3}; tip_span {oldSpan:F3}->{newSpan:F3}");
                bool damping=newDecay.x<oldDecay.x*.8f&&newDecay.y<oldDecay.y*.8f;passed&=damping;
                report.AppendLine($"decay_after_same_kick={damping}; tail_sway {oldDecay.x:F4}->{newDecay.x:F4}; tail_twist {oldDecay.y:F4}->{newDecay.y:F4}");
                EditorSceneManager.OpenScene(ScenePath);
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
                foreach(var slot in new[]{new Vector2(-.20f,.16f),new Vector2(.17f,-.16f),new Vector2(.18f,.16f),new Vector2(-.325f,.325f),new Vector2(.325f,.325f),new Vector2(.325f,-.325f)}) {
                    cycle.machine.ResetPosition();bool corner=Mathf.Abs(slot.x)>.3f&&Mathf.Abs(slot.y)>.3f;
                    if(!corner){var aim=cycle.machine.carriage.position;aim.x=slot.x;aim.z=slot.y;cycle.machine.carriage.position=aim;cycle.ResetCycle();}
                    for(int i=1;i<cycle.prizes.Length;i++){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
                    var prize=cycle.prizes[0];prize.position=new Vector3(slot.x,.727f,slot.y);Physics.SyncTransforms();
                    if(corner){for(int step=0;step<500;step++){var delta=slot-cycle.machine.Aim;if(delta.magnitude<.004f){cycle.machine.Move(Vector2.zero,.01f,false);break;}cycle.machine.Move(delta.normalized,.01f,false);Sim(cycle,.01f);}cycle.machine.ReleaseControls();}
                    Sim(cycle,1.2f);
                    int before=receiver.Count;report.AppendLine($"open_before_press={cycle.OpenFingerCount}; angles={string.Join(",",cycle.fingers.Select(f=>f.angle.ToString("F1")))}");
                    cycle.Press();float apex=prize.position.y;bool returned=false,closed=false;
                    for(int k=0;k<1500;k++){Sim(cycle,.01f);apex=Mathf.Max(apex,prize.position.y);if(cycle.Phase==ClawCycle.Stage.Raising&&!closed){closed=true;report.AppendLine($"after_close prize={prize.position}; hub={cycle.hub.position}; angles={string.Join(",",cycle.fingers.Select(f=>f.angle.ToString("F1")))}; targets={string.Join(",",cycle.fingers.Select(f=>f.spring.targetPosition.ToString("F1")))}");}if(cycle.Phase==ClawCycle.Stage.Returning&&!returned){returned=true;report.AppendLine($"at_return prize={prize.position}; hub={cycle.hub.position}; angles={string.Join(",",cycle.fingers.Select(f=>f.angle.ToString("F1")))}");}}
                    report.AppendLine($"apex={apex:F3}; returned={returned}");bool success=receiver.Count==before+1&&cycle.Phase==ClawCycle.Stage.Ready;passed&=success;
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
                Directory.CreateDirectory("Builds/Week06/Checks");File.WriteAllText("Builds/Week06/Checks/editor-delivery.txt",(passed?"PASS\n":"FAIL\n")+report);
                if(!passed)throw new Exception("Delivery tuning required\n"+report);
                EditorSceneManager.OpenScene(ScenePath);Physics.simulationMode=previous;Debug.Log("PICKDOLL_WEEK06_VALIDATION_PASS\n"+report);
                if(Application.isBatchMode)EditorApplication.Exit(0);
            }catch(Exception e){Physics.simulationMode=previous;Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);else throw;}
            finally {Physics.simulationMode=previous;}
        }
        private static float OpeningSpan(ClawCycle cycle) {
            Physics.SyncTransforms();var tips=cycle.fingers.Select(f=>f.GetComponentsInChildren<BoxCollider>().Last().transform.TransformPoint(Vector3.up*.5f)).ToArray();
            float span=0;foreach(var a in tips)foreach(var b in tips)span=Mathf.Max(span,Vector3.Distance(a,b));return span;
        }
        private static Vector2 Decay(ClawCycle cycle) {
            cycle.Initialize();foreach(var body in cycle.prizes){body.isKinematic=true;body.position=new Vector3(4,2,4);}
            cycle.hub.linearVelocity=Vector3.right*.5f;cycle.hub.angularVelocity=Vector3.up*1.5f;Vector2 tail=Vector2.zero;
            for(int i=0;i<400;i++){Sim(cycle,.01f);if(i>=300){tail.x=Mathf.Max(tail.x,new Vector2(cycle.hub.position.x,cycle.hub.position.z).magnitude);tail.y=Mathf.Max(tail.y,Mathf.Abs(cycle.hub.angularVelocity.y));}}
            return tail;
        }
        private static void Sim(ClawCycle cycle,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds/.01f);i++){cycle.Step(.01f);Physics.Simulate(.01f);cycle.receiver.Probe();}}
        [MenuItem("PickDoll/Week 06/Build Windows")]
        public static void BuildWindows()=>Build(BuildTarget.StandaloneWindows64,"Builds/Week06/Windows/PickDoll.exe");
        [MenuItem("PickDoll/Week 06/Build Android")]
        public static void BuildAndroid(){PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});EditorUserBuildSettings.buildAppBundle=false;Build(BuildTarget.Android,"Builds/Week06/Android/PickDoll-week06.apk");}
        private static void Build(BuildTarget target,string path)
        {
            var mobile=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=mobile;QualitySettings.SetQualityLevel(0,false);QualitySettings.renderPipeline=mobile;
            Generate();Directory.CreateDirectory(Path.GetDirectoryName(path));var result=BuildPipeline.BuildPlayer(new[]{ScenePath},path,target,BuildOptions.Development);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Week06 build failed: "+result.summary.result);
            Debug.Log(target==BuildTarget.Android?"PICKDOLL_WEEK06_ANDROID_READY":"PICKDOLL_WEEK06_WINDOWS_READY");
        }
    }
}
