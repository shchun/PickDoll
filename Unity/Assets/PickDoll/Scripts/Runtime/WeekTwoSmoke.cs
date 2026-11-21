using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PickDoll
{
    public sealed class WeekTwoSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--pickdoll-week02-test")<0)return;
            Application.runInBackground=true;
            new GameObject("Week02 Physical Pickup Test").AddComponent<WeekTwoSmoke>();
        }
        private IEnumerator Start()
        {
            yield return null;
            var cycle=FindFirstObjectByType<ClawCycle>();
            string output=Path.Combine(Application.dataPath,"..","Checks");Directory.CreateDirectory(output);
            var report=new StringBuilder();bool passed=cycle!=null; float totalLift=0;
            if(!cycle){File.WriteAllText(Path.Combine(output,"physics-runtime.txt"),"FAIL: No ClawCycle");Application.Quit(1);yield break;}
            yield return new WaitForSeconds(.7f);
            for(int trial=0;trial<3;trial++)
            {
                cycle.machine.ResetPosition();yield return new WaitForSeconds(.6f);
                var prize=cycle.prizes[trial];var p=cycle.machine.carriage.position;p.x=prize.position.x;p.z=prize.position.z;cycle.machine.carriage.position=p;
                yield return new WaitForSeconds(.3f);cycle.Press();
                float deadline=Time.realtimeSinceStartup+10;
                while(cycle.Busy && Time.realtimeSinceStartup<deadline)yield return new WaitForFixedUpdate();
                float lifted=prize.position.y; totalLift+=lifted;
                var before=prize.position;
                for(int i=0;i<80;i++){cycle.machine.Move(new Vector2(trial==2?-1:1,0),.01f,false);yield return new WaitForFixedUpdate();}
                float carried=Vector2.Distance(new Vector2(before.x,before.z),new Vector2(prize.position.x,prize.position.z));
                bool held=cycle.Phase==ClawCycle.Stage.Carry && lifted>1.48f && prize.position.y>1.45f && carried>.10f;
                report.AppendLine($"trial {trial}: held={held}, liftedY={lifted:F3}, carry={carried:F3}, angles={string.Join(",",cycle.fingers.Select(x=>x.angle.ToString("F1")))}");
                passed&=held;
                if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week02-raised.png"));}
                cycle.Press();yield return new WaitForSeconds(1.5f);
                bool released=prize.position.y<1.37f;
                passed&=released;report.AppendLine($"released={released}, releasedY={prize.position.y:F3}");
                if(trial==0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"week02-released.png"));}
            }
            // An open reset must work even in the middle of lowering.
            cycle.machine.ResetPosition();cycle.Press();yield return new WaitForSeconds(.6f);cycle.machine.ResetPosition();yield return new WaitForSeconds(.5f);
            bool reset=cycle.Phase==ClawCycle.Stage.Ready && Mathf.Abs(cycle.hub.position.y-cycle.topHeight)<.01f;
            passed&=reset;report.AppendLine("reset_during_cycle="+reset);
            // Observe weak actuation; contact geometry may still support the prize.
            float normal=cycle.gripStiffness;cycle.gripStiffness=.015f;cycle.machine.ResetPosition();
            yield return new WaitForSeconds(.6f);var weakPrize=cycle.prizes[1];
            var aim=cycle.machine.carriage.position;aim.x=weakPrize.position.x;aim.z=weakPrize.position.z;cycle.machine.carriage.position=aim;
            yield return new WaitForSeconds(.3f);cycle.Press();yield return new WaitForSeconds(5.6f);
            report.AppendLine($"weak_grip_observation: height={weakPrize.position.y:F3}; normal_average={totalLift/3:F3}; no required slip/drop outcome");
            cycle.gripStiffness=normal;cycle.machine.ResetPosition();
            for(int i=0;i<cycle.prizes.Length;i++) if(i!=1){cycle.prizes[i].isKinematic=true;cycle.prizes[i].position=new Vector3(3+i,1.2f,3);}
            var emptyAim=cycle.machine.carriage.position;emptyAim.x=.18f;emptyAim.z=.18f;cycle.machine.carriage.position=emptyAim;
            yield return new WaitForSeconds(.3f);cycle.Press();yield return new WaitForSeconds(5.6f);
            bool miss=cycle.prizes[1].position.y<1.37f;passed&=miss;report.AppendLine("empty_grab_did_not_lift="+miss);
            foreach(var prize in cycle.prizes)prize.isKinematic=false;cycle.machine.ResetPosition();
            report.Insert(0,passed?"PASS\n":"FAIL\n");
            File.WriteAllText(Path.Combine(output,"physics-runtime.txt"),report.ToString());Debug.Log(report.ToString());
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"week02-preview.png"));
            yield return null;yield return new WaitForEndOfFrame();
            Application.Quit(passed?0:1);
        }
    }
}
