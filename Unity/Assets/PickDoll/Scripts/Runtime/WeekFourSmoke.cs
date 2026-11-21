using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PickDoll
{
    public sealed class WeekFourSmoke : MonoBehaviour
    {
        private HoldControl input;
        private ClawCycle cycle;
        private StringBuilder report=new StringBuilder();
        private bool passed=true;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate(){if(!Environment.GetCommandLineArgs().Contains("--pickdoll-week04-test"))return;Application.runInBackground=true;new GameObject("Week04 Runtime Checks").AddComponent<WeekFourSmoke>();}
        private IEnumerator Drive(Vector2 direction,float seconds,bool fine=false)
        {
            if(cycle.machine.Fine!=fine)cycle.machine.ToggleFine();
            cycle.machine.Hold(input,direction,true);yield return new WaitForSeconds(seconds);
            cycle.machine.Hold(input,direction,false);yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
        }
        private void Fixture(Vector2 slot)
        {
            cycle.machine.ResetPosition();var aim=cycle.machine.carriage.position;aim.x=slot.x;aim.z=slot.y;cycle.machine.carriage.position=aim;cycle.ResetCycle();
            for(int i=1;i<cycle.prizes.Length;i++){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
            cycle.prizes[0].position=new Vector3(slot.x,1.202f,slot.y);Physics.SyncTransforms();
        }
        private IEnumerator Grab()
        {
            cycle.Press();float limit=Time.realtimeSinceStartup+10;
            while(cycle.Busy&&Time.realtimeSinceStartup<limit)yield return new WaitForFixedUpdate();
            passed&=cycle.Phase==ClawCycle.Stage.Carry;
        }
        private IEnumerator Start()
        {
            yield return null;cycle=FindAnyObjectByType<ClawCycle>();input=gameObject.AddComponent<HoldControl>();input.machine=cycle.machine;
            var output=Path.Combine(Application.dataPath,"..","Checks");Directory.CreateDirectory(output);
            bool noTurn=!FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>().text.StartsWith("TURN"));passed&=noTurn;report.AppendLine("no_manual_turn="+noTurn);
            cycle.gripVariationEnabled=false;Fixture(Vector2.zero);yield return new WaitForSeconds(.5f);
            yield return Drive(Vector2.right,.8f);float normal=cycle.machine.Aim.x;var stopped=cycle.machine.Aim;
            yield return new WaitForSeconds(.2f);bool stop=Vector2.Distance(stopped,cycle.machine.Aim)<.0001f;passed&=stop;
            Fixture(Vector2.zero);yield return Drive(Vector2.right,.2f,true);float precise=cycle.machine.Aim.x;passed&=precise>.003f&&precise<.012f&&normal>.16f;
            report.AppendLine($"input_speed: normal_0.8s={normal:F3}; precise_0.2s={precise:F4}; release_stops={stop}");
            Fixture(Vector2.zero);cycle.Press();yield return Drive(Vector2.right,.4f);bool lowering=cycle.Phase==ClawCycle.Stage.Lowering&&cycle.machine.Aim.x>.04f;passed&=lowering;report.AppendLine("lowering_move="+lowering);
            Fixture(Vector2.zero);yield return new WaitForSeconds(.7f);float movingEnergy=0,peak=0;
            for(int i=0;i<360;i++) {
                float angle=i*.01f*7;cycle.machine.Hold(input,new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),true);yield return new WaitForFixedUpdate();
                peak=Mathf.Max(peak,Vector3.Angle(cycle.hub.transform.up,Vector3.up));if(i>=260)movingEnergy+=cycle.hub.angularVelocity.sqrMagnitude/100;
            }
            cycle.machine.Hold(input,Vector2.zero,false);yield return new WaitForSeconds(5);float restEnergy=0;
            for(int i=0;i<100;i++){yield return new WaitForFixedUpdate();restEnergy+=cycle.hub.angularVelocity.sqrMagnitude/100;}
            bool swing=peak>2&&peak<50&&restEnergy<movingEnergy;passed&=swing;report.AppendLine($"swing={swing}; peak={peak:F2}; movingRms={Mathf.Sqrt(movingEnergy):F3}; settledRms={Mathf.Sqrt(restEnergy):F3}");
            var slots=new[]{Vector2.zero,new Vector2(-.33f,-.33f),new Vector2(.33f,-.33f),new Vector2(-.33f,.33f),new Vector2(.33f,.33f),new Vector2(-.34f,0),new Vector2(.34f,0),new Vector2(0,-.34f),new Vector2(0,.34f)};
            int lifts=0,corners=0,walls=0;bool cornerShot=false;
            for(int repeat=0;repeat<2;repeat++)for(int trial=0;trial<slots.Length;trial++) {
                Fixture(slots[trial]);yield return new WaitForSeconds(1.2f);float time=Time.time;yield return Grab();float duration=Time.time-time;
                var prize=cycle.prizes[0];bool lifted=prize.position.y>1.45f;
                if(lifted){lifts++;if(trial>=1&&trial<=4)corners++;if(trial>=5)walls++;}
                bool inside=Mathf.Abs(prize.position.x)<.46f&&Mathf.Abs(prize.position.z)<.44f;passed&=inside;
                report.AppendLine($"normal {repeat}/{trial}: lift={lifted}; y={prize.position.y:F3}; cycleSeconds={duration:F2}; contained={inside}");
                if(repeat==0&&trial==0){passed&=lifted;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week04-raised.png"));yield return Drive(Vector2.right,.55f);bool carry=prize.position.y>1.45f;passed&=carry;report.AppendLine($"normal_speed_carry={carry}; distance={cycle.machine.Aim.x:F3}");}
                if(!cornerShot&&lifted&&trial>=1&&trial<=4){cornerShot=true;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week04-corner.png"));}
                cycle.Press();yield return new WaitForSeconds(1.6f);bool release=prize.position.y<1.37f;passed&=release;report.AppendLine("open_releases="+release);
            }
            passed&=lifts>=12&&corners>=2&&walls>=4;report.AppendLine($"settled_lifts={lifts}/18; corners={corners}/8; walls={walls}/8; fixture resets suspension directly under aim (not comparable to week03's jumping-anchor test)");
            // Observe an immediate grab after real horizontal input; do not force success.
            Fixture(Vector2.zero);cycle.prizes[0].position=new Vector3(.17f,1.202f,0);yield return new WaitForSeconds(.5f);
            yield return Drive(Vector2.right,.7f);yield return Grab();
            report.AppendLine($"moving_entry_observation: aim={cycle.machine.Aim}; prizeHeight={cycle.prizes[0].position.y:F3}; residual swing is allowed");
            cycle.Press();yield return new WaitForSeconds(1.6f);passed&=cycle.prizes[0].position.y<1.37f;
            // Weak actuation is a force observation; mechanical support may still hold the toy.
            Fixture(Vector2.zero);cycle.gripVariationEnabled=true;cycle.weakGripChance=1;cycle.weakGripRange=new Vector2(.4f,.65f);yield return new WaitForSeconds(.8f);yield return Grab();
            float sampled=cycle.CurrentGripScale;float actual=cycle.fingers[0].spring.spring;bool weak=cycle.CurrentWeakGrip&&sampled>=.4f&&sampled<=.65f&&Mathf.Abs(actual-cycle.gripStiffness*sampled)<.001f;
            passed&=weak;report.AppendLine($"weak_force={weak}; multiplier={sampled:F2}; actualSpring={actual:F3}; height={cycle.prizes[0].position.y:F3}; no required drop/failure");
            yield return Drive(Vector2.right,.3f);passed&=Mathf.Abs(cycle.CurrentGripScale-sampled)<.0001f;
            cycle.Press();yield return new WaitForSeconds(1.6f);passed&=cycle.prizes[0].position.y<1.37f;
            cycle.weakGripChance=.25f;cycle.weakGripRange=new Vector2(.4f,.65f);foreach(var p in cycle.prizes)p.isKinematic=false;cycle.machine.ResetPosition();
            passed&=cycle.Phase==ClawCycle.Stage.Ready&&Mathf.Abs(cycle.hub.position.y-cycle.topHeight)<.001f;
            report.Insert(0,passed?"PASS\n":"FAIL\n");File.WriteAllText(Path.Combine(output,"physics-runtime.txt"),report.ToString());Debug.Log(report.ToString());yield return new WaitForSeconds(.3f);Application.Quit(passed?0:1);
        }
    }
}
