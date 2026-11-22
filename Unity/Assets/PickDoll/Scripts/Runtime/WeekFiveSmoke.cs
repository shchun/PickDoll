using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace PickDoll
{
    public sealed class WeekFiveSmoke : MonoBehaviour
    {
        private ClawCycle cycle;private PrizeReceiver receiver;private bool passed=true;private int events;
        private StringBuilder report=new StringBuilder();private string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate(){if(!Environment.GetCommandLineArgs().Contains("--pickdoll-week05-test"))return;Application.runInBackground=true;new GameObject("Week05 Runtime Checks").AddComponent<WeekFiveSmoke>();}
        private Rigidbody Fixture(Vector2 aim,Vector3 position,Quaternion rotation)
        {
            foreach(var body in cycle.prizes)body.isKinematic=false;
            cycle.machine.ResetPosition();var rig=cycle.machine.carriage.position;rig.x=aim.x;rig.z=aim.y;cycle.machine.carriage.position=rig;cycle.ResetCycle();
            for(int i=1;i<cycle.prizes.Length;i++){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
            var prize=cycle.prizes[0];prize.position=position;prize.rotation=rotation;prize.linearVelocity=Vector3.zero;prize.angularVelocity=Vector3.zero;Physics.SyncTransforms();return prize;
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
            bool startup=Mathf.Abs(cycle.hub.position.y-cycle.topHeight)<.03f;
            startup&=Vector3.Distance(cycle.suspensionAnchor.position,cycle.hub.position+Vector3.up*cycle.suspensionLength)<.03f;
            foreach(var finger in cycle.fingers)startup&=Vector3.Distance(finger.transform.position,cycle.hub.transform.TransformPoint(finger.connectedAnchor))<.02f;
            passed&=startup;report.AppendLine("startup_joint_assembly_stable="+startup);
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
                var slot=slots[trial];body=Fixture(slot,new Vector3(slot.x,.727f,slot.y),Quaternion.identity);before=receiver.Count;yield return new WaitForSeconds(1.2f);
                cycle.Press();limit=Time.realtimeSinceStartup+20;bool returnObserved=false,blockedInput=true,noEarlyCount=true,highRelease=true;
                while((cycle.Phase!=ClawCycle.Stage.Ready||receiver.Count==before)&&Time.realtimeSinceStartup<limit) {
                    if(cycle.Phase==ClawCycle.Stage.Returning&&!returnObserved) {
                        returnObserved=true;noEarlyCount=receiver.Count==before;
                        var old=cycle.machine.Aim;cycle.machine.Move(Vector2.right,1,false);blockedInput=Vector2.Distance(old,cycle.machine.Aim)<.0001f;
                        var phase=cycle.Phase;cycle.Press();blockedInput&=cycle.Phase==phase;
                        if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week05-carry.png"));}
                    }
                    if(cycle.Phase==ClawCycle.Stage.Opening||cycle.Phase==ClawCycle.Stage.Settling)highRelease&=cycle.hub.position.y>cycle.topHeight-.05f;
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForSeconds(.8f);
                bool win=receiver.Count==before+1&&cycle.Phase==ClawCycle.Stage.Ready;
                bool home=cycle.machine.Aim.magnitude<.002f;bool tray=body.position.y>.06f&&body.position.z<-.60f;
                passed&=win&&home&&tray&&returnObserved&&blockedInput&&noEarlyCount&&highRelease;
                report.AppendLine($"automatic_{trial}: acquired={win}; home={home}; visible_tray={tray}; busy_input_blocked={blockedInput}; no_early_count={noEarlyCount}; high_release={highRelease}; pos={body.position}; total={receiver.Count}");
                if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week05-acquired.png"));}
                for(int i=0;i<20;i++)receiver.Probe();passed&=receiver.Count==before+1;
            }
            cycle.machine.ResetPosition();foreach(var p in cycle.prizes){p.isKinematic=true;p.position=new Vector3(5,1.2f,5);}
            before=receiver.Count;cycle.Press();limit=Time.realtimeSinceStartup+18;
            while(cycle.Phase!=ClawCycle.Stage.Ready&&Time.realtimeSinceStartup<limit)yield return new WaitForFixedUpdate();
            bool empty=cycle.Phase==ClawCycle.Stage.Ready&&receiver.Count==before;passed&=empty;report.AppendLine("empty_cycle_returns_without_acquisition="+empty);
            string oldTicket=cycle.prizes[0].GetComponent<PrizeIdentity>().Ticket;foreach(var p in cycle.prizes)p.isKinematic=false;cycle.machine.ResetPosition();
            bool restock=receiver.Count==before&&cycle.prizes[0].GetComponent<PrizeIdentity>().Ticket!=oldTicket;passed&=restock&&events==receiver.Count;report.AppendLine($"new_round_preserves_session_count={restock}; events={events}; total={receiver.Count}");
            var counter=GameObject.Find("Collection counter");
            var digits=counter ? counter.GetComponentsInChildren<UnityEngine.UI.Text>().FirstOrDefault(t=>t.fontSize==56) : null;
            bool visibleCount=digits&&digits.text==receiver.Count.ToString()&&digits.color==Color.white;
            passed&=visibleCount;report.AppendLine("large_counter_matches_receipts="+visibleCount);
            // Guard the empty hole: a new grab there must not start descending.
            var point=cycle.machine.carriage.position;point.x=receiver.holeCenter.x;point.z=receiver.holeCenter.y;cycle.machine.carriage.position=point;cycle.Press();
            bool guard=cycle.Phase==ClawCycle.Stage.Ready&&!cycle.CanPress;passed&=guard;report.AppendLine("grab_over_hole_blocked="+guard);
            cycle.machine.ResetPosition();cycle.gripVariationEnabled=true;
            report.Insert(0,passed?"PASS\n":"FAIL\n");File.WriteAllText(Path.Combine(output,"delivery-runtime.txt"),report.ToString());Debug.Log(report.ToString());yield return new WaitForSeconds(.3f);Application.Quit(passed?0:1);
        }
    }
}
