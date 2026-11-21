using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickDoll.Editor
{
    public static class WeekOneChecks
    {
        private static void Require(bool condition,string message)
        {
            if(!condition)throw new Exception("PICKDOLL_CHECK_FAILED: "+message);
            Debug.Log("PICKDOLL_CHECK_PASS: "+message);
        }
        [MenuItem("PickDoll/Validate Week 01 Scene")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(WeekOneBuilder.ScenePath);
            var machine=UnityEngine.Object.FindFirstObjectByType<MachineController>();
            var orbit=UnityEngine.Object.FindFirstObjectByType<OrbitView>();
            Require(machine && orbit,"Scene contains machine and camera controllers");
            for(int i=0;i<4;i++)
            {
                orbit.NextPreset();machine.ResetPosition();machine.Move(Vector2.right,.5f,false);
                Require(Mathf.Abs(machine.Aim.x-.12f)<.0001f && Mathf.Abs(machine.Aim.y)<.0001f,"Right moves +X independently of camera preset "+i);
                machine.Move(Vector2.up,.5f,false);
                Require(Mathf.Abs(machine.Aim.y-.12f)<.0001f,"Back moves +Z independently of camera preset "+i);
            }
            machine.Move(Vector2.one,100,false);
            Require(machine.Aim.x==machine.travelLimit && machine.Aim.y==machine.travelLimit,"Travel bounds clamp both axes");
            machine.ResetPosition();machine.Move(Vector2.right,1,true);
            Require(Mathf.Abs(machine.Aim.x-machine.fineSpeed)<.0001f,"Precision uses configured fine speed");
            machine.claw.localRotation=Quaternion.Euler(10,60,-8);
            machine.Move(Vector2.zero,0,false);
            Require(Vector2.Distance(machine.Aim,new Vector2(machine.aimMarker.position.x,machine.aimMarker.position.z))<.0001f,"Aim marker follows carriage instead of claw orientation");
            Require(Shader.Find("Universal Render Pipeline/Lit")!=null,"URP shader available");
            var hud=UnityEngine.Object.FindFirstObjectByType<PrototypeHud>();
            Require(hud && hud.machine==machine && hud.orbit==orbit,"HUD references are wired");
            machine.ResetPosition();
            Directory.CreateDirectory("Builds/Checks");
            File.WriteAllText("Builds/Checks/week01-validation.txt","PASS: editor scene checks for machine axes across four views, travel bounds, fine speed, stable marker, URP and HUD references. Runtime UI is checked separately in built-player smoke test.\nUnity "+Application.unityVersion);
            Debug.Log("PICKDOLL_WEEK01_VALIDATION_PASS");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }
    }
}
