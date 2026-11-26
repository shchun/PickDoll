using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace PickDoll
{
    public sealed class WeekElevenSmoke : MonoBehaviour
    {
        private ClawCycle cycle;private PrizeReceiver receiver;private bool passed=true;private int events;
        private StringBuilder report=new StringBuilder();private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate(){if(!Environment.GetCommandLineArgs().Any(a=>a=="--pickdoll-week11-test"||a=="--pickdoll-week11-ui"))return;Application.runInBackground=true;new GameObject("Week11 Runtime Checks").AddComponent<WeekElevenSmoke>();}
        private int fixtureIndex;
        private Rigidbody Fixture(Vector2 aim,Vector3 position,Quaternion rotation)
        {
            foreach(var body in cycle.prizes)body.isKinematic=false;
            cycle.machine.ResetPosition();var rig=cycle.machine.carriage.position;rig.x=aim.x;rig.z=aim.y;cycle.machine.carriage.position=rig;cycle.ResetCycle();
            for(int i=0;i<cycle.prizes.Length;i++)if(i!=fixtureIndex){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
            var prize=cycle.prizes[fixtureIndex];prize.position=position;prize.rotation=rotation;prize.linearVelocity=Vector3.zero;prize.angularVelocity=Vector3.zero;Physics.SyncTransforms();return prize;
        }
        private static bool stoppedReasonValid(string reason)=>reason=="PRIZE_CONTACT"||reason=="STRUCTURE"||reason=="DEPTH_LIMIT"||reason=="TIME_LIMIT";
        private IEnumerator WaitReceipt(int before,float timeout=10)
        {
            float limit=Time.realtimeSinceStartup+timeout;
            while(receiver.Count==before&&Time.realtimeSinceStartup<limit)yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(.15f);
        }
        private static Vector2[] CarrotOffsets()=>new[]{new Vector2(0,-.055f),new Vector2(0,.055f),new Vector2(.035f,-.035f),new Vector2(-.035f,-.035f),new Vector2(.055f,0),new Vector2(-.055f,0),new Vector2(.04f,.04f),new Vector2(-.04f,.04f)};
        private IEnumerator Targeting(){fixtureIndex=4;var slot=new Vector2(.17f,-.16f);bool success=false;var results=new StringBuilder();foreach(var offset in CarrotOffsets()){var body=cycle.prizes[fixtureIndex];var pose=Quaternion.identity;float y=.645f-PrizeStock.BoundsAt(body,Vector3.zero,pose).min.y;body=Fixture(slot+offset,new Vector3(slot.x,y,slot.y),pose);int before=receiver.Count;yield return new WaitForSeconds(1.2f);cycle.Press();float until=Time.realtimeSinceStartup+20;while(cycle.Phase!=ClawCycle.Stage.Ready&&Time.realtimeSinceStartup<until)yield return new WaitForFixedUpdate();yield return WaitReceipt(before,3);success=receiver.Count==before+1;results.AppendLine($"standing_carrot offset={offset} acquired={success} final={body.position}");if(success){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-carrot-target.png"));break;}}File.WriteAllText(Path.Combine(output,"targeting.txt"),(success?"PASS\n":"FAIL\n")+results);Application.Quit(success?0:1);}
                private IEnumerator Flow(){
            var auto=cycle.GetComponent<AutomaticRestock>();var feedback=cycle.GetComponent<AcquisitionFeedback>();var audio=cycle.GetComponent<ArcadeSound>();cycle.machine.ResetPosition();int before=receiver.Count;var tickets=cycle.prizes.Select(p=>p.GetComponent<PrizeIdentity>().Ticket).ToArray();foreach(var p in cycle.prizes){p.isKinematic=true;p.position=new Vector3(5,2,5);}yield return new WaitForSeconds(.8f);bool ok=auto.RefillCount==0;feedback.SetSound(false);int sounds=audio.AcquisitionPlays;
            for(int i=0;i<7;i++){var p=cycle.prizes[i];p.isKinematic=false;p.rotation=Quaternion.Euler(i%2==0?90:0,0,0);var shape=PrizeStock.BoundsAt(p,Vector3.zero,p.rotation);p.position=new Vector3(-.23f-shape.center.x,.46f,-.25f-shape.center.z);p.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return WaitReceipt(before+i,10);bool arrived=receiver.Count==before+i+1;ok&=arrived;report.AppendLine("batch_"+i+"="+arrived+"; pos="+p.position+"; rotation="+p.rotation.eulerAngles);}
            float limit=Time.realtimeSinceStartup+15;while(auto.RefillCount==0&&Time.realtimeSinceStartup<limit)yield return null;ok&=auto.RefillCount==1&&receiver.Count==before+7&&audio.AcquisitionPlays==sounds&&audio.RefillPlays==0&&cycle.prizes.All(p=>!p.GetComponent<PrizeIdentity>().Delivered&&!p.GetComponent<PrizeDeliveryPresentation>().Hidden&&!p.isKinematic&&p.GetComponents<Collider>().All(c=>c.enabled));report.AppendLine("refill="+auto.RefillCount+"; total="+receiver.Count+"; silence="+(audio.AcquisitionPlays==sounds));yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-restock.png"));yield return new WaitForSeconds(2);ok&=auto.RefillCount==1&&receiver.Count==before+7;File.WriteAllText(Path.Combine(output,"flow.txt"),(ok?"PASS\n":"FAIL\n")+report);Application.Quit(ok?0:1);
        }
        private IEnumerator Start()
        {
            yield return null;cycle=FindAnyObjectByType<ClawCycle>();receiver=cycle.receiver;receiver.Acquired+=_=>events++;cycle.gripVariationEnabled=false;var feedback=FindAnyObjectByType<AcquisitionFeedback>();
            output=Path.Combine(Application.dataPath,"..","Checks");Directory.CreateDirectory(output);if(Environment.GetCommandLineArgs().Contains("--pickdoll-week11-flow")){yield return Flow();yield break;}if(Environment.GetCommandLineArgs().Contains("--pickdoll-week11-targeting")){yield return Targeting();yield break;}
            // Test the saved startup scene before any fixture/reset can repair its poses.
            yield return new WaitForSeconds(2.5f);
            bool startup=Mathf.Abs(cycle.hub.position.y-cycle.topHeight)<.03f&&Vector2.Distance(cycle.machine.Aim,receiver.holeCenter)<.003f&&!cycle.CanPress;
            startup&=Vector3.Distance(cycle.suspensionAnchor.position,cycle.hub.position+Vector3.up*cycle.suspensionLength)<.03f;
            foreach(var finger in cycle.fingers)startup&=Vector3.Distance(finger.transform.position,cycle.hub.transform.TransformPoint(finger.connectedAnchor))<.02f;
            passed&=startup;report.AppendLine("startup_joint_assembly_stable="+startup);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-preview.png"));
            bool stock=cycle.prizes.Length==7&&cycle.prizes.Select(p=>p.GetComponent<PrizeIdentity>().SpeciesId).Distinct().Count()==7;
            foreach(var prize in cycle.prizes)stock&=prize.position.y>.60f&&Mathf.Abs(prize.position.x)<.42f&&Mathf.Abs(prize.position.z)<.42f;
            int lying=cycle.prizes.Count(p=>Mathf.Abs(Vector3.Dot(p.rotation*Vector3.up,Vector3.up))<.55f);
            stock&=lying>=2&&receiver.Count==0;passed&=stock;report.AppendLine("seven_prizes_seven_species_stable="+stock+"; lying="+lying);
            for(int i=0;i<3;i++){cycle.machine.ResetPosition();yield return new WaitForSeconds(2);bool resetHome=Vector2.Distance(cycle.machine.Aim,receiver.holeCenter)<.003f&&Mathf.Abs(cycle.hub.position.y-cycle.topHeight)<.03f&&!cycle.CanPress;passed&=resetHome;report.AppendLine("new_round_home_"+i+"="+resetHome);}
            bool clean=!feedback.Visible&&feedback.AcceptedCount==0;passed&=clean;report.AppendLine("no_success_on_start_or_new_round="+clean);
            var soundButton=GameObject.Find("Sound toggle").GetComponent<UnityEngine.UI.Button>();
            var hapticButton=GameObject.Find("Haptics toggle").GetComponent<UnityEngine.UI.Button>();
            bool oldSound=feedback.SoundEnabled,oldHaptic=feedback.HapticsEnabled;soundButton.onClick.Invoke();hapticButton.onClick.Invoke();feedback.LoadPreferences();
            bool preferences=feedback.SoundEnabled!=oldSound&&feedback.HapticsEnabled!=oldHaptic;passed&=preferences;report.AppendLine("sound_haptics_buttons_and_saved_preferences="+preferences);soundButton.onClick.Invoke();hapticButton.onClick.Invoke();
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
            float small=cycle.machine.Aim.x-cycle.machine.homePosition.x;joystick.OnPointerUp(touch);yield return new WaitForSeconds(.05f);var stopped=cycle.machine.Aim;yield return new WaitForSeconds(.15f);
            bool releaseStops=Vector2.Distance(stopped,cycle.machine.Aim)<.0001f;
            cycle.machine.ResetPosition();touch.position=center;joystick.OnPointerDown(touch);touch.position=center+scale;joystick.OnDrag(touch);yield return new WaitForSeconds(.6f);float full=cycle.machine.Aim.x-cycle.machine.homePosition.x;
            var stranger=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=102,position=center-scale};joystick.OnPointerUp(stranger);
            bool ownsPointer=cycle.machine.Analog.x>.9f;joystick.CancelTouch();
            bool analog=small>0&&small<full*.3f&&full>.08f&&releaseStops&&ownsPointer&&cycle.machine.Analog==Vector2.zero;
            passed&=analog;report.AppendLine($"analog_drag_precision_release_ownership={analog}; small={small:F4}; full={full:F4}");
            var ray=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();touch.position=center;
            eventSystem.RaycastAll(touch,ray);bool routes=ray.Count>0&&ray[0].gameObject==joystick.gameObject;passed&=routes;report.AppendLine("joystick_excludes_camera_raycast="+routes);
            ray.Clear();touch.position=new Vector2(Screen.safeArea.center.x,Screen.safeArea.yMax-Screen.height*.23f);
            eventSystem.RaycastAll(touch,ray);bool headerTouch=ray.Count>0&&ray[0].gameObject==cameraArea.gameObject;
            passed&=headerTouch;report.AppendLine("empty_header_routes_camera_gestures="+headerTouch);
            var first=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=201,position=new Vector2(200,600)};
            var second=new UnityEngine.EventSystems.PointerEventData(eventSystem){pointerId=202,position=new Vector2(300,600)};
            cameraArea.OnPointerDown(first);cameraArea.OnPointerDown(second);float yaw=orbit.yaw;
            second.position=new Vector2(400,600);cameraArea.OnDrag(second);
            bool pinch=Mathf.Abs(orbit.Zoom-2)<.001f&&Mathf.Abs(orbit.yaw-yaw)<.001f;
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-zoom.png"));
            second.position=new Vector2(800,600);cameraArea.OnDrag(second);pinch&=orbit.Zoom==2;
            second.position=new Vector2(225,600);cameraArea.OnDrag(second);pinch&=orbit.Zoom==1;
            orbit.ZoomByRatio(2);orbit.NextPreset();pinch&=orbit.Zoom==1;
            cameraArea.OnPointerUp(second);float afterPinch=orbit.yaw;first.position+=Vector2.right*40;cameraArea.OnDrag(first);pinch&=Mathf.Abs(orbit.yaw-afterPinch)<.001f;cameraArea.CancelTouches();
            passed&=pinch;report.AppendLine("pinch_1_to_2_limits_view_reset_no_yaw_jump="+pinch);
            bool removed=FindObjectsByType<HoldControl>().Length==0&&!FindObjectsByType<UnityEngine.UI.Text>().Any(t=>t.text.StartsWith("PRECISION"));
            passed&=removed;report.AppendLine("direction_and_precision_buttons_removed="+removed);
            orbit.yaw=12;orbit.elevation=55;orbit.Apply();yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-canopy.png"));
            orbit.elevation=15;orbit.Apply();cycle.machine.ResetPosition();
            var controls=(RectTransform)GameObject.Find("Touch controls").transform;
            var action=(RectTransform)GameObject.Find("GRAB!").transform;
            var corners=new Vector3[4];controls.GetWorldCorners(corners);
            bool layout=orbit.GetComponent<Camera>().pixelRect.yMin>=corners[2].y-.5f;
            foreach(var target in new[]{origin,action}){target.GetWorldCorners(corners);layout&=corners[0].x>=Screen.safeArea.xMin-.5f&&corners[2].x<=Screen.safeArea.xMax+.5f&&corners[0].y>=Screen.safeArea.yMin-.5f;}
            passed&=layout;report.AppendLine($"safe_touch_layout_{Screen.width}x{Screen.height}="+layout);
            if(Environment.GetCommandLineArgs().Contains("--pickdoll-week11-ui")) {
                var uiBook=FindAnyObjectByType<CollectionBook>();uiBook.SetOpen(true);yield return null;var modal=(RectTransform)GameObject.Find("Collection modal").transform;modal.GetWorldCorners(corners);bool bookFits=corners[0].x>=Screen.safeArea.xMin&&corners[2].x<=Screen.safeArea.xMax&&corners[0].y>=Screen.safeArea.yMin&&corners[2].y<=Screen.safeArea.yMax;passed&=bookFits;report.AppendLine("book_safe_area="+bookFits);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,$"book-layout-{Screen.width}x{Screen.height}.png"));uiBook.SetOpen(false);
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,$"ui-layout-{Screen.width}x{Screen.height}.png"));
                File.WriteAllText(Path.Combine(output,$"ui-layout-{Screen.width}x{Screen.height}.txt"),(passed?"PASS\n":"FAIL\n")+report);
                yield return new WaitForSeconds(.2f);Application.Quit(passed?0:1);yield break;
            }
            float[] pathYaw=new float[2];float[] twistPeak=new float[2];float[] twistTail=new float[2];
            for(int path=0;path<2;path++){
                cycle.machine.ResetPosition();foreach(var p in cycle.prizes){p.isKinematic=true;p.position=new Vector3(4,2,4);}
                float startYaw=cycle.hub.rotation.eulerAngles.y;
                foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down}){
                    cycle.machine.SetAnalog(path==0?direction:new Vector2(direction.y,direction.x));
                    float until=Time.realtimeSinceStartup+.35f;while(Time.realtimeSinceStartup<until){twistPeak[path]=Mathf.Max(twistPeak[path],Mathf.Abs(cycle.hub.angularVelocity.y));yield return new WaitForFixedUpdate();}
                }
                cycle.machine.ReleaseControls();float coast=Time.realtimeSinceStartup+.8f;while(Time.realtimeSinceStartup<coast){twistPeak[path]=Mathf.Max(twistPeak[path],Mathf.Abs(cycle.hub.angularVelocity.y));yield return new WaitForFixedUpdate();}pathYaw[path]=Mathf.DeltaAngle(startYaw,cycle.hub.rotation.eulerAngles.y);
                yield return new WaitForSeconds(2);twistTail[path]=Mathf.Abs(cycle.hub.angularVelocity.y);
            }
            bool pathResponse=Mathf.Abs(pathYaw[0]-pathYaw[1])>.02f&&twistTail[0]<twistPeak[0]&&twistTail[1]<twistPeak[1];
            passed&=pathResponse;report.AppendLine($"passive_path_twist_and_decay={pathResponse}; yaw={pathYaw[0]:F3},{pathYaw[1]:F3}; peak={twistPeak[0]:F3},{twistPeak[1]:F3}; tail={twistTail[0]:F3},{twistTail[1]:F3}");
            var chain=new ComboChain();bool comboBounds=chain.Accept("a",0)==1&&chain.Accept("b",10)==2&&chain.Accept("c",20.0001)==1&&chain.Accept("d",20.0001)==2&&chain.Accept("d",21)==2;chain.Reset();comboBounds&=chain.Accept("e",22)==1;
            passed&=comboBounds;report.AppendLine("combo_10s_boundary_timeout_simultaneous_dedup_reset="+comboBounds);
            var book=FindAnyObjectByType<CollectionBook>();book.SetOpen(true);book.Select(3);yield return null;var preview=GameObject.Find("Preview model").transform;var oldRotation=preview.rotation;book.Rotate(100);bool bookInput=Time.timeScale==0&&!cycle.machine.enabled&&Quaternion.Angle(oldRotation,preview.rotation)>20;book.SetOpen(false);bookInput&=Time.timeScale==1&&cycle.machine.enabled;passed&=bookInput;report.AppendLine("book_modal_preview_rotation_input_and_resume="+bookInput);
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
            body.isKinematic=false;yield return WaitReceipt(before);
            bool feedbackWin=feedback.AcceptedCount==1&&feedback.PresentedCount==1&&feedback.Visible&&feedback.CurrentName==body.GetComponent<PrizeIdentity>().displayName;
            int accepted=feedback.AcceptedCount;for(int replay=0;replay<10;replay++)feedback.Receive(body.GetComponent<PrizeIdentity>());
            feedbackWin&=feedback.AcceptedCount==accepted&&GameObject.Find("Collection counter").transform.localScale.x>1.01f;ray.Clear();touch.position=RectTransformUtility.WorldToScreenPoint(null,GameObject.Find("Acquisition card").transform.position);eventSystem.RaycastAll(touch,ray);feedbackWin&=ray.Count>0&&ray[0].gameObject==cameraArea.gameObject;passed&=feedbackWin;report.AppendLine("saved_acquisition_card_once="+feedbackWin);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-success.png"));
            bool delivered=receiver.Count==before+1;passed&=delivered;
            for(int i=0;i<20;i++)receiver.Probe();bool dedup=receiver.Count==before+1;passed&=dedup;report.AppendLine($"upright_delivery={delivered}; duplicate_probes_once={dedup}; pos={body.position}");
            foreach(float tilt in new[]{90f,180f}) {
                body=Fixture(Vector2.zero,new Vector3(-.23f,.925f,-.22f),Quaternion.Euler(0,0,tilt));before=receiver.Count;yield return WaitReceipt(before);
                bool ok=receiver.Count==before+1;passed&=ok;report.AppendLine($"gravity_pose_{tilt}={ok}; pos={body.position}");
            }
            body=Fixture(Vector2.zero,new Vector3(-.23f,.727f,.08f),Quaternion.Euler(-90,0,0));body.linearVelocity=new Vector3(0,0,-.15f);body.angularVelocity=new Vector3(-1,0,0);before=receiver.Count;
            yield return new WaitForSeconds(2);bool rolling=receiver.Count==before&&!body.GetComponent<PrizeIdentity>().EnteredChute;passed&=rolling;report.AppendLine($"transparent_guard_blocks_rolling={rolling}; pos={body.position}");
            // Final floor contact should count even before the whole prize clears the facade.
            body=Fixture(Vector2.zero,new Vector3(-.23f,.16f,-.82f),Quaternion.Euler(90,0,0));body.GetComponent<PrizeIdentity>().EnteredChute=true;before=receiver.Count;
            yield return WaitReceipt(before,3);bool edge=receiver.Count==before+1;passed&=edge;report.AppendLine("tray_edge_floor_contact_counts="+edge);
            int edgeCount=receiver.Count;yield return new WaitForSeconds(.4f);receiver.Probe();passed&=receiver.Count==edgeCount;
            body=Fixture(Vector2.zero,new Vector3(-.23f,.30f,-.55f),Quaternion.identity);body.GetComponent<PrizeIdentity>().EnteredChute=true;body.isKinematic=true;before=receiver.Count;
            yield return new WaitForSeconds(.3f);bool noFloor=receiver.Count==before;passed&=noFloor;report.AppendLine("chute_or_ramp_without_final_floor_not_counted="+noFloor);
            var slots=new[]{new Vector2(-.20f,.16f),new Vector2(.17f,-.16f),new Vector2(.18f,.16f)};
            for(int trial=0;trial<cycle.prizes.Length*2;trial++) {
                fixtureIndex=trial%cycle.prizes.Length;var slot=slots[trial%3];var pose=trial<cycle.prizes.Length?Quaternion.Euler(0,fixtureIndex==5?45:0,0):Quaternion.Euler(90,0,0);
                float restY=.645f-PrizeStock.BoundsAt(cycle.prizes[fixtureIndex],Vector3.zero,pose).min.y;
                body=Fixture(slot,new Vector3(slot.x,restY,slot.y),pose);before=receiver.Count;yield return new WaitForSeconds(1.2f);
                cycle.Press();limit=Time.realtimeSinceStartup+20;bool returnObserved=false,blockedInput=true,noEarlyCount=true,highRelease=true;float lowest=cycle.hub.position.y;string stopReason="";
                while((cycle.Phase!=ClawCycle.Stage.Ready||receiver.Count==before)&&Time.realtimeSinceStartup<limit) {
                    lowest=Mathf.Min(lowest,cycle.hub.position.y);if(cycle.DescentStop!="")stopReason=cycle.DescentStop;
                    if(cycle.Phase==ClawCycle.Stage.Returning&&!returnObserved) {
                        returnObserved=true;noEarlyCount=receiver.Count==before;
                        var old=cycle.machine.Aim;cycle.machine.Move(Vector2.right,1,false);blockedInput=Vector2.Distance(old,cycle.machine.Aim)<.0001f;
                        var phase=cycle.Phase;cycle.Press();blockedInput&=cycle.Phase==phase;
                        if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-carry.png"));}
                    }
                    if(cycle.Phase==ClawCycle.Stage.Opening||cycle.Phase==ClawCycle.Stage.Settling)highRelease&=cycle.hub.position.y>cycle.topHeight-.05f;
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForSeconds(.8f);
                bool win=receiver.Count==before+1&&cycle.Phase==ClawCycle.Stage.Ready;
                if(!win&&(fixtureIndex==4||fixtureIndex==5)&&trial<cycle.prizes.Length){report.AppendLine("center_grip_failed_try_visible_edges species="+body.GetComponent<PrizeIdentity>().SpeciesId);foreach(var offset in fixtureIndex==4?CarrotOffsets():new[]{new Vector2(.035f,.035f),new Vector2(-.035f,-.035f),new Vector2(.045f,-.045f),new Vector2(-.045f,.045f)}){body=Fixture(slot+offset,new Vector3(slot.x,restY,slot.y),pose);before=receiver.Count;yield return new WaitForSeconds(1.2f);cycle.Press();float retryLimit=Time.realtimeSinceStartup+20;while((cycle.Phase!=ClawCycle.Stage.Ready||receiver.Count==before)&&Time.realtimeSinceStartup<retryLimit)yield return new WaitForFixedUpdate();yield return new WaitForSeconds(.4f);win=receiver.Count==before+1&&cycle.Phase==ClawCycle.Stage.Ready;report.AppendLine($"shape_edge offset={offset}: acquired={win}; pos={body.position}");if(win)break;}}

                bool home=Vector2.Distance(cycle.machine.Aim,receiver.holeCenter)<.003f;bool tray=body.position.y>.06f&&body.position.z<-.60f;
                bool descentSafe=lowest>=cycle.bottomHeight-.05f&&stoppedReasonValid(stopReason);
                passed&=win&&home&&tray&&returnObserved&&blockedInput&&noEarlyCount&&highRelease&&descentSafe;
                report.AppendLine($"automatic_{trial}: acquired={win}; home={home}; visible_tray={tray}; busy_input_blocked={blockedInput}; no_early_count={noEarlyCount}; high_release={highRelease}; pos={body.position}; total={receiver.Count}; lowest={lowest:F3}; stop={stopReason}; force={cycle.PrizeContactForce:F2}; safe={descentSafe}");
                if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-acquired.png"));}
                for(int i=0;i<20;i++)receiver.Probe();passed&=receiver.Count==before+1;
            }
            // A prize still at the outlet must wait; each prize gets a receipt when it reaches the floor.
            fixtureIndex=1;body=Fixture(Vector2.zero,new Vector3(-.23f,.33f,-.71f),Quaternion.identity);body.isKinematic=true;body.GetComponent<PrizeIdentity>().EnteredChute=true;before=receiver.Count;
            var follower=cycle.prizes[3];follower.isKinematic=false;follower.position=new Vector3(-.23f,.22f,-1.1f);follower.rotation=Quaternion.identity;follower.GetComponent<PrizeIdentity>().EnteredChute=true;Physics.SyncTransforms();yield return WaitReceipt(before,3);
            bool hungFirst=receiver.Count==before+1&&!body.GetComponent<PrizeIdentity>().Delivered;
            body.isKinematic=false;body.position=new Vector3(-.38f,.23f,-1.28f);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return WaitReceipt(before+1,4);
            bool twoReceipts=hungFirst&&receiver.Count==before+2;passed&=twoReceipts;report.AppendLine("hung_rabbit_then_second_delivery_two_unique_receipts="+twoReceipts);yield return new WaitForSeconds(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-combo.png"));
            Fixture(Vector2.zero,new Vector3(5,1.2f,5),Quaternion.identity);foreach(var p in cycle.prizes){p.isKinematic=true;p.position=new Vector3(5,1.2f,5);}
            before=receiver.Count;cycle.Press();limit=Time.realtimeSinceStartup+18;
            while(cycle.Phase!=ClawCycle.Stage.Ready&&Time.realtimeSinceStartup<limit)yield return new WaitForFixedUpdate();
            bool empty=cycle.Phase==ClawCycle.Stage.Ready&&receiver.Count==before&&(cycle.DescentStop=="STRUCTURE"||cycle.DescentStop=="DEPTH_LIMIT");passed&=empty;report.AppendLine("empty_cycle_returns_without_acquisition="+empty);
            string oldTicket=cycle.prizes[0].GetComponent<PrizeIdentity>().Ticket;foreach(var p in cycle.prizes)p.isKinematic=false;cycle.machine.ResetPosition();
            bool restock=Vector2.Distance(cycle.machine.Aim,receiver.holeCenter)<.003f&&receiver.Count==before&&cycle.prizes[0].GetComponent<PrizeIdentity>().Ticket!=oldTicket;passed&=restock&&events==receiver.Count;report.AppendLine($"new_round_preserves_session_count={restock}; events={events}; total={receiver.Count}");
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
            book.SetOpen(true);for(int portrait=3;portrait<7;portrait++){book.Select(portrait);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,$"week11-species-{portrait}.png"));}book.Select(6);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-collection.png"));book.SetOpen(false);
            bool catalog=cycle.prizes.All(p=>loaded.CountFor(p.GetComponent<PrizeIdentity>().SpeciesId)>0);passed&=catalog;report.AppendLine("all_seven_species_preserved_and_discovered="+catalog);
            cycle.machine.ResetPosition();bool resetCombo=cycle.GetComponent<ComboTracker>().Chain.Count==0;passed&=resetCombo;report.AppendLine("new_round_resets_combo_only="+resetCombo);
            yield return new WaitForSeconds(4.2f);bool feedbackTotal=feedback.AcceptedCount==events&&feedback.PresentedCount==events&&!feedback.Visible;
            passed&=feedbackTotal;report.AppendLine("all_receipts_one_feedback_each_no_replay="+feedbackTotal+"; accepted="+feedback.AcceptedCount+"; events="+events);
            // Replay a complete seven-prize delivery without manually clearing the tray.
            cycle.machine.ResetPosition();var auto=cycle.GetComponent<AutomaticRestock>();var audio=cycle.GetComponent<ArcadeSound>();int refillBefore=auto.RefillCount;int totalBefore=receiver.Count;
            foreach(var prize in cycle.prizes){prize.isKinematic=true;prize.position=new Vector3(5,1.2f,5);}
            yield return new WaitForSeconds(.8f);bool waitsForPending=auto.RefillCount==refillBefore;passed&=waitsForPending;report.AppendLine("empty_display_waits_for_unfinished_delivery="+waitsForPending);
            feedback.SetSound(false);int soundsBefore=audio.AcquisitionPlays;int refillSoundsBefore=audio.RefillPlays;
            for(int i=0;i<cycle.prizes.Length;i++){var prize=cycle.prizes[i];prize.rotation=Quaternion.Euler(i%2==0?90:0,0,0);var shape=PrizeStock.BoundsAt(prize,Vector3.zero,prize.rotation);prize.position=new Vector3(-.23f-shape.center.x,.46f,-.25f-shape.center.z);prize.isKinematic=false;prize.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return WaitReceipt(totalBefore+i,8);bool arrived=receiver.Count==totalBefore+i+1;passed&=arrived;report.AppendLine("batch_delivery_"+i+"="+arrived+"; pos="+prize.position);}
            float refillLimit=Time.realtimeSinceStartup+15;while(auto.RefillCount==refillBefore&&Time.realtimeSinceStartup<refillLimit)yield return null;
            bool refilled=auto.RefillCount==refillBefore+1&&receiver.Count==totalBefore+7&&cycle.prizes.All(p=>!p.GetComponent<PrizeIdentity>().Delivered&&!p.GetComponent<PrizeDeliveryPresentation>().Hidden&&!p.isKinematic&&p.GetComponents<Collider>().All(c=>c.enabled));passed&=refilled;report.AppendLine("all_seven_delivered_auto_refill_once_preserves_count_restores_models="+refilled);
            bool silent=audio.AcquisitionPlays==soundsBefore&&audio.RefillPlays==refillSoundsBefore;passed&=silent;report.AppendLine("sound_off_mutes_acquisition_and_refill="+silent);feedback.SetSound(true);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week11-restock.png"));
            yield return new WaitForSeconds(2);bool noReplay=auto.RefillCount==refillBefore+1&&receiver.Count==totalBefore+7;passed&=noReplay;report.AppendLine("refill_notification_no_false_receipt_or_repeat="+noReplay);
            var checkAgain=new CollectionStore(receiver.Store.Path);passed&=checkAgain.Total==receiver.Count;report.AppendLine("final_save_total="+checkAgain.Total);
            report.Insert(0,passed?"PASS\n":"FAIL\n");File.WriteAllText(Path.Combine(output,"delivery-runtime.txt"),report.ToString());Debug.Log(report.ToString());yield return new WaitForSeconds(.3f);Application.Quit(passed?0:1);
        }
    }
}
