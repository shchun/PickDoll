using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PickDoll
{
    // Opt-in validation of the built player; absent in ordinary gameplay.
    public sealed class PrototypeSmokeCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Activate()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--pickdoll-smoke")>=0)
                new GameObject("Build Smoke Check").AddComponent<PrototypeSmokeCapture>();
        }
        private IEnumerator Start()
        {
            yield return null; yield return new WaitForEndOfFrame();
            var output=Path.Combine(Application.dataPath,"..","Checks");Directory.CreateDirectory(output);
            var machine=FindFirstObjectByType<MachineController>();
            var orbit=FindFirstObjectByType<OrbitView>();
            bool passed=true;
            for(int i=0;i<4;i++)
            {
                orbit.NextPreset();machine.ResetPosition();machine.Move(Vector2.right,.5f,false);
                passed&=Mathf.Abs(machine.Aim.x-.12f)<.0001f && Mathf.Abs(machine.Aim.y)<.0001f;
            }
            machine.Move(Vector2.one,100,false);
            passed&=machine.Aim.x==machine.travelLimit && machine.Aim.y==machine.travelLimit;
            machine.ResetPosition();machine.Move(Vector2.right,1,true);
            passed&=Mathf.Abs(machine.Aim.x-machine.fineSpeed)<.0001f;
            machine.ResetPosition();orbit.yaw=12;orbit.elevation=15;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"week01-preview.png"));
            yield return null; yield return new WaitForEndOfFrame();
            passed&=File.Exists(Path.Combine(output,"week01-preview.png"));
            passed&=FindObjectsByType<Button>(FindObjectsSortMode.None).Length==9;
            File.WriteAllText(Path.Combine(output,"player-smoke.txt"),(passed?"PASS":"FAIL")+"\nUnity "+Application.unityVersion+"\nResolution "+Screen.width+"x"+Screen.height);
            Debug.Log("PICKDOLL_PLAYER_SMOKE_"+(passed?"PASS":"FAIL"));
            Application.Quit(passed?0:1);
        }
    }
}
