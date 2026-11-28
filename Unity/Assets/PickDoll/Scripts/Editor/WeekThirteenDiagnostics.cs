using System;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickDoll.Editor
{
    public static class WeekThirteenDiagnostics
    {
        public static void Run()
        {
            var report = new StringBuilder();
            var mode = Physics.simulationMode;
            bool passed = true;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                foreach (bool tuned in new[] { false, true })
                {
                    EditorSceneManager.OpenScene(WeekTwelveBuilder.ScenePath);
                    var cycle = UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
                    cycle.receiver.persistentProgress = false;
                    if (tuned) WeekThirteenBuilder.ConfigureClaw(cycle);
                    cycle.Initialize();
                    cycle.gripVariationEnabled = false;
                    float max = 0, sustained = 0;
                    string worstState = "";
                    int completed = 0;
                    // Repeat real cycles with contact, lift, carriage acceleration and return.
                    // Do not attach prizes artificially or teleport separated fingers back.
                    for (int attempt = 0; attempt < 28; attempt++)
                    {
                        cycle.machine.ResetPosition();
                        var aim = new Vector2(attempt % 2 == 0 ? .28f : -.28f, attempt % 3 == 0 ? .28f : -.10f);
                        cycle.machine.carriage.position = new Vector3(aim.x, cycle.machine.carriage.position.y, aim.y);
                        var body = cycle.prizes[attempt % cycle.prizes.Length];
                        body.isKinematic = false;
                        body.rotation = Quaternion.Euler(attempt % 2 * 90, attempt * 37, 0);
                        var bounds = PrizeStock.BoundsAt(body, Vector3.zero, body.rotation);
                        body.position = new Vector3(aim.x - bounds.center.x, .645f - bounds.min.y, aim.y - bounds.center.z);
                        body.linearVelocity = body.angularVelocity = Vector3.zero;
                        Physics.SyncTransforms();
                        for (int k = 0; k < 150; k++) { cycle.Step(.01f); Physics.Simulate(.01f); }
                        cycle.Press();
                        float errorTime = 0;
                        for (int step = 0; step < 2200; step++)
                        {
                            cycle.Step(.01f);
                            Physics.Simulate(.01f);
                            cycle.StabilizeFingerAnchors();
                            float error = 0;
                            foreach (var finger in cycle.ActiveFingers) error = Mathf.Max(error, ClawDiagnostics.AnchorError(finger));
                            if(error>max){max=error;worstState=$"attempt={attempt} step={step} phase={cycle.Phase} hub={cycle.hub.position} finger={cycle.ActiveFingers[0].GetComponent<Rigidbody>().position}";}
                            errorTime = error > .015f ? errorTime + .01f : 0;
                            sustained = Mathf.Max(sustained, errorTime);
                            if (step > 200 && cycle.Phase == ClawCycle.Stage.Ready) { completed++; break; }
                        }
                    }
                    report.AppendLine($"tuned={tuned} attempts=28 completed={completed} max_anchor_error_m={max:F6} longest_error_over_15mm_s={sustained:F3}");
                    report.AppendLine(worstState);
                    if (tuned) passed &= completed == 28 && sustained < .25f;
                }
                var chain = new ComboChain();
                bool combo = ComboChain.Window == 15 && chain.Accept("a", 0) == 1 && chain.Accept("b", 15) == 2 &&
                    chain.Accept("b", 20) == 2 && chain.Accept("c", 30.0001) == 1;
                chain.Reset(); combo &= chain.Accept("d", 31) == 1;
                passed &= combo; report.AppendLine("combo_15s_boundary_timeout_dedup_reset=" + combo);
                Directory.CreateDirectory("Builds/Week13/Checks");
                File.WriteAllText("Builds/Week13/Checks/claw-stress.txt", (passed ? "PASS\n" : "FAIL\n") + report);
                if (!passed) throw new Exception("Week13 validation failed: " + report);
            }
            finally { Physics.simulationMode = mode; EditorSceneManager.OpenScene(WeekThirteenBuilder.ScenePath); }
        }
    }
}
