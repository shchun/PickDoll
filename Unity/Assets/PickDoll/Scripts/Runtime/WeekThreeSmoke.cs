using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PickDoll
{
    public sealed class WeekThreeSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            if(!Environment.GetCommandLineArgs().Contains("--pickdoll-week03-test"))return;
            Application.runInBackground=true;new GameObject("Week03 Runtime Checks").AddComponent<WeekThreeSmoke>();
        }
        private IEnumerator Start()
        {
            yield return null;
            var cycle=FindFirstObjectByType<ClawCycle>();var machine=cycle.machine;
            var openRotations=cycle.fingers.Select(f=>Quaternion.Inverse(cycle.hub.rotation)*f.transform.rotation).ToArray();
            var output=Path.Combine(Application.dataPath,"..","Checks");Directory.CreateDirectory(output);
            var report=new StringBuilder();bool passed=cycle.PassiveSuspension;
            bool noTurn=!FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>().text.StartsWith("TURN"));
            passed&=noTurn;report.AppendLine("no_manual_turn_buttons="+noTurn);
            machine.ResetPosition();cycle.Press();
            var before=machine.Aim;
            for(int i=0;i<35;i++){machine.Move(Vector2.right,.01f,false);yield return new WaitForFixedUpdate();}
            bool loweringMove=cycle.Phase==ClawCycle.Stage.Lowering && machine.Aim.x-before.x>.06f;
            passed&=loweringMove;report.AppendLine("lowering_move="+loweringMove);
            machine.ResetPosition();yield return new WaitForSeconds(1);
            float reversalPeak=0;
            for(int i=0;i<100;i++) {
                machine.Move(i<50?Vector2.right:Vector2.left,.01f,false);yield return new WaitForFixedUpdate();
                reversalPeak=Mathf.Max(reversalPeak,Vector3.Angle(cycle.hub.transform.up,Vector3.up));
            }
            bool reversal=reversalPeak>1 && reversalPeak<50;passed&=reversal;
            report.AppendLine($"straight_reversal_swing={reversal}; peakTilt={reversalPeak:F2}");
            machine.ResetPosition();yield return new WaitForSeconds(1);
            float peak=0, movingEnergy=0;
            for(int i=0;i<450;i++)
            {
                float angle=i*.01f*7;
                machine.Move(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),.01f,false);
                yield return new WaitForFixedUpdate();
                peak=Mathf.Max(peak,Vector3.Angle(cycle.hub.transform.up,Vector3.up));
                if(i>=350)movingEnergy+=cycle.hub.angularVelocity.sqrMagnitude/100;
            }
            float yaw=cycle.hub.rotation.eulerAngles.y;
            float moving=Mathf.Sqrt(movingEnergy);
            yield return new WaitForSeconds(5);
            float restEnergy=0;for(int i=0;i<100;i++){yield return new WaitForFixedUpdate();restEnergy+=cycle.hub.angularVelocity.sqrMagnitude/100;}
            float settled=Mathf.Sqrt(restEnergy);
            bool swing=peak>2 && peak<50 && settled<moving;
            passed&=swing;report.AppendLine($"circular_swing={swing}; peakTilt={peak:F2}; yaw={yaw:F1}; angularMoving={moving:F3}; settled={settled:F3}");
            var slots=new[]{new Vector2(0,0),new Vector2(-.33f,-.33f),new Vector2(.33f,-.33f),new Vector2(-.33f,.33f),new Vector2(.33f,.33f),new Vector2(-.34f,0),new Vector2(.34f,0),new Vector2(0,-.34f),new Vector2(0,.34f)};
            int liftedCount=0, cornerLifts=0, wallLifts=0;
            for(int trial=0;trial<slots.Length;trial++)
            {
                machine.ResetPosition();
                for(int i=1;i<cycle.prizes.Length;i++){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(4+i,1.2f,4);}
                var prize=cycle.prizes[0];prize.position=new Vector3(slots[trial].x,1.202f,slots[trial].y);prize.rotation=Quaternion.identity;
                var aim=machine.carriage.position;aim.x=slots[trial].x;aim.z=slots[trial].y;machine.carriage.position=aim;
                yield return new WaitForSeconds(1.2f);float start=prize.position.y;
                cycle.Press();float deadline=Time.realtimeSinceStartup+9;
                while(cycle.Busy&&Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
                float height=prize.position.y;bool lifted=height>1.45f;if(lifted){liftedCount++;if(trial>=1&&trial<=4)cornerLifts++;if(trial>=5)wallLifts++;}
                float[] angles=cycle.fingers.Select((f,i)=>{
                    var open=openRotations[i];
                    var local=Quaternion.Inverse(cycle.hub.rotation)*f.transform.rotation;
                    return Vector3.SignedAngle(open*Vector3.down,local*Vector3.down,open*Vector3.forward);
                }).ToArray();
                bool multiple=angles.Count(a=>a<-5)>=2;
                bool contained=Mathf.Abs(prize.position.x)<.46f&&Mathf.Abs(prize.position.z)<.44f;
                passed&=contained&&(multiple||lifted);
                if(trial==0)passed&=lifted;
                report.AppendLine($"slot {trial} {slots[trial]}: lift={lifted}; y={height:F3}; multiple_closed={multiple}; contained={contained}; angles={string.Join(",",angles.Select(a=>a.ToString("F1")))}");
                if(trial==0||trial==4){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,trial==0?"week03-raised.png":"week03-corner.png"));}
                if(trial==0){var p=prize.position;for(int i=0;i<50;i++){machine.Move(Vector2.right,.01f,true);yield return new WaitForFixedUpdate();}bool carry=prize.position.y>1.45f;passed&=carry;report.AppendLine("center_carry="+carry);}
                cycle.Press();yield return new WaitForSeconds(1.5f);
                bool release=prize.position.y<1.37f;passed&=release;report.AppendLine("release="+release);
            }
            report.AppendLine("lifted_locations="+liftedCount+"/9 (geometry-dependent; wall contact retained)");
            passed&=cornerLifts>0&&wallLifts>0;
            // With no prize blocking the blades, all independent hinges must close.
            machine.ResetPosition();foreach(var p in cycle.prizes){p.isKinematic=true;p.position=new Vector3(5,1.2f,5);}
            yield return new WaitForSeconds(.7f);cycle.Press();yield return new WaitForSeconds(5.7f);
            bool emptyClosed=cycle.fingers.All(f=>Vector3.Angle(f.transform.up,cycle.hub.transform.up)<12);
            passed&=emptyClosed;report.AppendLine("empty_all_fingers_closed="+emptyClosed);
            // Reset restores the complete rig and all prizes.
            foreach(var p in cycle.prizes)p.isKinematic=false;machine.ResetPosition();
            bool reset=cycle.Phase==ClawCycle.Stage.Ready&&cycle.hub.linearVelocity.sqrMagnitude<.001f;
            passed&=reset;report.AppendLine("reset="+reset);
            report.Insert(0,passed?"PASS\n":"FAIL\n");File.WriteAllText(Path.Combine(output,"physics-runtime.txt"),report.ToString());Debug.Log(report.ToString());
            yield return new WaitForSeconds(.3f);Application.Quit(passed?0:1);
        }
    }
}
