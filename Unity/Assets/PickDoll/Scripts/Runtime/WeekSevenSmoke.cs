using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace PickDoll
{
    public sealed class WeekSevenSmoke : MonoBehaviour
    {
        private ClawCycle cycle;private PrizeReceiver receiver;private bool passed=true;private int events;
        private StringBuilder report=new StringBuilder();private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate(){if(!Environment.GetCommandLineArgs().Any(a=>a=="--pickdoll-week07-test"||a=="--pickdoll-week07-ui"))return;Application.runInBackground=true;new GameObject("Week07 Runtime Checks").AddComponent<WeekSevenSmoke>();}
        private int fixtureIndex;
        private Rigidbody Fixture(Vector2 aim,Vector3 position,Quaternion rotation)
        {
            foreach(var body in cycle.prizes)body.isKinematic=false;
            cycle.machine.ResetPosition();var rig=cycle.machine.carriage.position;rig.x=aim.x;rig.z=aim.y;cycle.machine.carriage.position=rig;cycle.ResetCycle();
            for(int i=0;i<cycle.prizes.Length;i++)if(i!=fixtureIndex){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
            var prize=cycle.prizes[fixtureIndex];prize.position=position;prize.rotation=rotation;prize.linearVelocity=Vector3.zero;prize.angularVelocity=Vector3.zero;Physics.SyncTransforms();return prize;
        }
        private IEnumerator WaitReceipt(int before,float timeout=10)
        {
            float limit=Time.realtimeSinceStartup+timeout;
            while(receiver.Count==before&&Time.realtimeSinceStartup<limit)yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(.7f);
        }
        private IEnumerator Start()
        {
            yield return null;cycle=FindAnyObjectByType<ClawCycle>();receiver=cycle.receiver;receiver.Acquired+=_=>events++;cycle.gripVariationEnabled=false;
            output=Path.Combine(Application.dataPath,"..","Checks");Directory.CreateDirectory(output);
            // Test the saved startup scene before any fixture/reset can repair its poses.
            yield return new WaitForSeconds(.8f);
            bool startup=Mathf.Abs(cycle.hub.position.y-cycle.topHeight)<.03f&&Vector2.Distance(cycle.machine.Aim,receiver.holeCenter)<.003f&&!cycle.CanPress;
            startup&=Vector3.Distance(cycle.suspensionAnchor.position,cycle.hub.position+Vector3.up*cycle.suspensionLength)<.03f;
            foreach(var finger in cycle.fingers)startup&=Vector3.Distance(finger.transform.position,cycle.hub.transform.TransformPoint(finger.connectedAnchor))<.02f;
            passed&=startup;report.AppendLine("startup_joint_assembly_stable="+startup);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week07-preview.png"));
            bool stock=cycle.prizes.Length==7&&cycle.prizes.Select(p=>p.GetComponent<PrizeIdentity>().SpeciesId).Distinct().Count()==3;
            foreach(var prize in cycle.prizes)stock&=prize.position.y>.65f&&Mathf.Abs(prize.position.x)<.42f&&Mathf.Abs(prize.position.z)<.42f;
            passed&=stock;report.AppendLine("seven_prizes_three_species_stable="+stock);
            var orbit=FindAnyObjectByType<OrbitView>();
            bool transparent=GameObject.Find("Header backdrop")==null&&orbit.headerHeight==0;
            transparent&=Mathf.Abs(orbit.GetComponent<Camera>().pixelRect.yMax-Screen.safeArea.yMax)<2;
            passed&=transparent;report.AppendLine("transparent_header_rendered_behind="+transparent);var joystick=FindAnyObjectByType<AnalogJoystick>();
            var cameraArea=FindAnyObjectByType<OrbitDragArea>();
            var eventSystem=UnityEngine.EventSystems.EventSystem.current;
            var origin=(RectTransform)joystick.transform;
            var touch=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=101};
            Vector2 center=RectTransformUtility.WorldToScreenPoint(null,origin.position);
            Vector2 scale=origin.TransformVector(Vector3.right*joystick.radius);
            cycle.machine.ResetPosition();touch.position=center;joystick.OnPointerDown(touch);
            touch.position=center+scale*.25f;joystick.OnDrag(touch);yield return new WaitForSeconds(.6f);
            float small=cycle.machine.Aim.x;joystick.OnPointerUp(touch);yield return new WaitForSeconds(.05f);var stopped=cycle.machine.Aim;yield return new WaitForSeconds(.15f);
            bool releaseStops=Vector2.Distance(stopped,cycle.machine.Aim)<.0001f;
            cycle.machine.ResetPosition();touch.position=center;joystick.OnPointerDown(touch);touch.position=center+scale;joystick.OnDrag(touch);yield return new WaitForSeconds(.6f);float full=cycle.machine.Aim.x;
            var stranger=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=102,position=center-scale};joystick.OnPointerUp(stranger);
            bool ownsPointer=cycle.machine.Analog.x>.9f;joystick.CancelTouch();
            bool analog=small>0&&small<full*.3f&&full>.08f&&releaseStops&&ownsPointer&&cycle.machine.Analog==Vector2.zero;
            passed&=analog;report.AppendLine($"analog_drag_precision_release_ownership={analog}; small={small:F4}; full={full:F4}");
            var ray=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();touch.position=center;
            eventSystem.RaycastAll(touch,ray);bool routes=ray.Count>0&&ray[0].gameObject==joystick.gameObject;passed&=routes;report.AppendLine("joystick_excludes_camera_raycast="+routes);
            ray.Clear();touch.position=new Vector2(Screen.safeArea.center.x,Screen.safeArea.yMax-Screen.height*.09f);
            eventSystem.RaycastAll(touch,ray);bool headerTouch=ray.Count>0&&ray[0].gameObject==cameraArea.gameObject;
            passed&=headerTouch;report.AppendLine("empty_header_routes_camera_gestures="+headerTouch);
            var first=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=201,position=new Vector2(200,600)};
            var second=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=202,position=new Vector2(300,600)};
            cameraArea.OnPointerDown(first);cameraArea.OnPointerDown(second);float yaw=orbit.yaw;
            second.position=new Vector2(400,600);cameraArea.OnDrag(second);
            bool pinch=Mathf.Abs(orbit.Zoom-2)<.001f&&Mathf.Abs(orbit.yaw-yaw)<.001f;
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week07-zoom.png"));
            second.position=new Vector2(800,600);cameraArea.OnDrag(second);pinch&=orbit.Zoom==2;
            second.position=new Vector2(225,600);cameraArea.OnDrag(second);pinch&=orbit.Zoom==1;
            orbit.ZoomByRatio(2);orbit.NextPreset();pinch&=orbit.Zoom==1;
            cameraArea.OnPointerUp(second);float afterPinch=orbit.yaw;first.position+=Vector2.right*40;cameraArea.OnDrag(first);pinch&=Mathf.Abs(orbit.yaw-afterPinch)<.001f;cameraArea.CancelTouches();
            passed&=pinch;report.AppendLine("pinch_1_to_2_limits_view_reset_no_yaw_jump="+pinch);
            bool removed=FindObjectsByType<HoldControl>().Length==0&&!FindObjectsByType<UnityEngine.UI.Text>().Any(t=>t.text.StartsWith("PRECISION"));
            passed&=removed;report.AppendLine("direction_and_precision_buttons_removed="+removed);
            orbit.yaw=12;orbit.elevation=55;orbit.Apply();yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week07-canopy.png"));
            orbit.elevation=15;orbit.Apply();cycle.machine.ResetPosition();
            var controls=(RectTransform)GameObject.Find("Touch controls").transform;
            var action=(RectTransform)GameObject.Find("GRAB!").transform;
            var corners=new Vector3[4];controls.GetWorldCorners(corners);
            bool layout=orbit.GetComponent<Camera>().pixelRect.yMin>=corners[2].y-.5f;
            foreach(var target in new[]{origin,action}){target.GetWorldCorners(corners);layout&=corners[0].x>=Screen.safeArea.xMin-.5f&&corners[2].x<=Screen.safeArea.xMax+.5f&&corners[0].y>=Screen.safeArea.yMin-.5f;}
            passed&=layout;report.AppendLine($"safe_touch_layout_{Screen.width}x{Screen.height}="+layout);
            if(Environment.GetCommandLineArgs().Contains("--pickdoll-week07-ui")) {
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,$"ui-layout-{Screen.width}x{Screen.height}.png"));
                File.WriteAllText(Path.Combine(output,$"ui-layout-{Screen.width}x{Screen.height}.txt"),(passed?"PASS\n":"FAIL\n")+report);
                yield return new WaitForSeconds(.2f);Application.Quit(passed?0:1);yield break;
            }
            // A visibly closed starting pose must open before any descent; one GRAB is sufficient.
            var body=Fixture(new Vector2(.17f,-.16f),new Vector3(.17f,.727f,-.16f),Quaternion.identity);
            var poses=cycle.fingers.Select(f=>Quaternion.Inverse(cycle.hub.rotation)*f.transform.rotation).ToArray();
            for(int i=0;i<cycle.fingers.Length;i++){var finger=cycle.fingers[i].GetComponent<Rigidbody>();finger.rotation=cycle.hub.rotation*poses[i]*Quaternion.Euler(0,0,cycle.closeAngle);finger.angularVelocity=Vector3.zero;}
            Physics.SyncTransforms();cycle.Press();bool preparation=cycle.Phase==ClawCycle.Stage.Preparing;float limit=Time.realtimeSinceStartup+3;
            while(cycle.Phase==ClawCycle.Stage.Preparing&&Time.realtimeSinceStartup<limit){passed&=cycle.hub.position.y>cycle.topHeight-.025f;yield return new WaitForFixedUpdate();}
            bool openBeforeDrop=preparation&&cycle.Phase==ClawCycle.Stage.Lowering&&cycle.OpenFingerCount>=2;passed&=openBeforeDrop;report.AppendLine("closed_start_opens_before_descent="+openBeforeDrop);
            body=Fixture(Vector2.zero,new Vector3(-.23f,.28f,-.90f),Quaternion.identity);int before=receiver.Count;
            yield return new WaitForSeconds(.4f);bool bypass=receiver.Count==before;passed&=bypass;report.AppendLine("exit_without_chute_not_counted="+bypass);
            // Pause in the entrance: entering the hole alone is not a win.
            body=Fixture(Vector2.zero,new Vector3(-.23f,.46f,-.22f),Quaternion.identity);body.isKinematic=true;before=receiver.Count;
            yield return new WaitForSeconds(.2f);bool entryOnly=body.GetComponent<PrizeIdentity>().EnteredChute&&receiver.Count==before;passed&=entryOnly;report.AppendLine("entry_only_not_counted="+entryOnly);
            body.isKinematic=false;yield return WaitReceipt(before);bool delivered=receiver.Count==before+1;passed&=delivered;
            for(int i=0;i<20;i++)receiver.Probe();bool dedup=receiver.Count==before+1;passed&=dedup;report.AppendLine($"upright_delivery={delivered}; duplicate_probes_once={dedup}; pos={body.position}");
            foreach(float tilt in new[]{90f,180f}) {
                body=Fixture(Vector2.zero,new Vector3(-.23f,.925f,-.22f),Quaternion.Euler(0,0,tilt));before=receiver.Count;yield return WaitReceipt(before);
                bool ok=receiver.Count==before+1;passed&=ok;report.AppendLine($"gravity_pose_{tilt}={ok}; pos={body.position}");
            }
            body=Fixture(Vector2.zero,new Vector3(-.23f,.727f,.08f),Quaternion.Euler(-90,0,0));body.linearVelocity=new Vector3(0,0,-.15f);body.angularVelocity=new Vector3(-1,0,0);before=receiver.Count;
            yield return new WaitForSeconds(2);bool rolling=receiver.Count==before&&!body.GetComponent<PrizeIdentity>().EnteredChute;passed&=rolling;report.AppendLine($"transparent_guard_blocks_rolling={rolling}; pos={body.position}");
            var slots=new[]{new Vector2(-.20f,.16f),new Vector2(.17f,-.16f),new Vector2(.18f,.16f)};
            for(int trial=0;trial<slots.Length;trial++) {
                fixtureIndex=trial;var slot=slots[trial];body=Fixture(slot,new Vector3(slot.x,.727f,slot.y),Quaternion.identity);before=receiver.Count;yield return new WaitForSeconds(1.2f);
                cycle.Press();limit=Time.realtimeSinceStartup+20;bool returnObserved=false,blockedInput=true,noEarlyCount=true,highRelease=true;
                while((cycle.Phase!=ClawCycle.Stage.Ready||receiver.Count==before)&&Time.realtimeSinceStartup<limit) {
                    if(cycle.Phase==ClawCycle.Stage.Returning&&!returnObserved) {
                        returnObserved=true;noEarlyCount=receiver.Count==before;
                        var old=cycle.machine.Aim;cycle.machine.Move(Vector2.right,1,false);blockedInput=Vector2.Distance(old,cycle.machine.Aim)<.0001f;
                        var phase=cycle.Phase;cycle.Press();blockedInput&=cycle.Phase==phase;
                        if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week07-carry.png"));}
                    }
                    if(cycle.Phase==ClawCycle.Stage.Opening||cycle.Phase==ClawCycle.Stage.Settling)highRelease&=cycle.hub.position.y>cycle.topHeight-.05f;
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForSeconds(.8f);
                bool win=receiver.Count==before+1&&cycle.Phase==ClawCycle.Stage.Ready;
                bool home=cycle.machine.Aim.magnitude<.002f;bool tray=body.position.y>.06f&&body.position.z<-.60f;
                passed&=win&&home&&tray&&returnObserved&&blockedInput&&noEarlyCount&&highRelease;
                report.AppendLine($"automatic_{trial}: acquired={win}; home={home}; visible_tray={tray}; busy_input_blocked={blockedInput}; no_early_count={noEarlyCount}; high_release={highRelease}; pos={body.position}; total={receiver.Count}");
                if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week07-acquired.png"));}
                for(int i=0;i<20;i++)receiver.Probe();passed&=receiver.Count==before+1;
            }
            cycle.machine.ResetPosition();foreach(var p in cycle.prizes){p.isKinematic=true;p.position=new Vector3(5,1.2f,5);}
            before=receiver.Count;cycle.Press();limit=Time.realtimeSinceStartup+18;
            while(cycle.Phase!=ClawCycle.Stage.Ready&&Time.realtimeSinceStartup<limit)yield return new WaitForFixedUpdate();
            bool empty=cycle.Phase==ClawCycle.Stage.Ready&&receiver.Count==before;passed&=empty;report.AppendLine("empty_cycle_returns_without_acquisition="+empty);
            string oldTicket=cycle.prizes[0].GetComponent<PrizeIdentity>().Ticket;foreach(var p in cycle.prizes)p.isKinematic=false;cycle.machine.ResetPosition();
            bool restock=receiver.Count==before&&cycle.prizes[0].GetComponent<PrizeIdentity>().Ticket!=oldTicket;passed&=restock&&events==receiver.Count;report.AppendLine($"new_round_preserves_session_count={restock}; events={events}; total={receiver.Count}");
            var counter=GameObject.Find("Collection counter");
            var digits=counter ? counter.GetComponentsInChildren<UnityEngine.UI.Text>().FirstOrDefault(t=>t.fontSize==38) : null;
            bool visibleCount=digits&&digits.text==receiver.Count.ToString()&&digits.color==Color.white;
            passed&=visibleCount;report.AppendLine("large_counter_matches_receipts="+visibleCount);
            // Guard the empty hole: a new grab there must not start descending.
            var point=cycle.machine.carriage.position;point.x=receiver.holeCenter.x;point.z=receiver.holeCenter.y;cycle.machine.carriage.position=point;cycle.Press();
            bool guard=cycle.Phase==ClawCycle.Stage.Ready&&!cycle.CanPress;passed&=guard;report.AppendLine("grab_over_hole_blocked="+guard);
            cycle.machine.ResetPosition();cycle.gripVariationEnabled=true;
            var loaded=new CollectionStore(receiver.Store.Path);
            bool saved=loaded.Total==receiver.Count&&loaded.CountFor("bear")>0&&loaded.CountFor("rabbit")>0&&loaded.CountFor("penguin")>0;
            saved&=File.Exists(receiver.Store.Path);passed&=saved;report.AppendLine("reload_preserves_total_and_species="+saved);
            report.Insert(0,passed?"PASS\n":"FAIL\n");File.WriteAllText(Path.Combine(output,"delivery-runtime.txt"),report.ToString());Debug.Log(report.ToString());yield return new WaitForSeconds(.3f);Application.Quit(passed?0:1);
        }
    }
}
