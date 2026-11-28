using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PickDoll.Editor
{
    public static class WeekThirteenBuilder
    {
        public const string ScenePath = "Assets/PickDoll/Scenes/Week13.unity";
        public static void Generate()
        {
            var scene = EditorSceneManager.OpenScene(WeekTwelveBuilder.ScenePath);
            var cycle = UnityEngine.Object.FindAnyObjectByType<ClawCycle>();
            ConfigureClaw(cycle);
            cycle.gameObject.AddComponent<ClawDiagnostics>().cycle = cycle;
            ConfigureBranding(cycle);
            PlayerSettings.bundleVersion = "0.13.0";
            PlayerSettings.Android.bundleVersionCode = 17;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PICKDOLL_WEEK13_SCENE_READY");
        }
        static void ConfigureBranding(ClawCycle cycle)
        {
            const string root = "Assets/PickDoll/Art/Week13/";
            AssetDatabase.Refresh();
            var mascot = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "mascot.png");
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "icon.png");
            cycle.gameObject.AddComponent<StartupSplash>().mascot = mascot;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            foreach (var kind in PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android))
            {
                var slots = PlayerSettings.GetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android, kind);
                foreach (var slot in slots)
                {
                    if (slot.minLayerCount > 1)
                    {
                        var layers = new Texture2D[Math.Min(3, slot.maxLayerCount)];
                        // Unity adaptive icon order is background, then foreground.
                        layers[0] = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "icon-background.png");
                        layers[1] = mascot;
                        if (layers.Length > 2) layers[2] = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "icon-monochrome.png");
                        slot.SetTextures(layers);
                    }
                    else slot.SetTexture(icon);
                }
                PlayerSettings.SetPlatformIcons(UnityEditor.Build.NamedBuildTarget.Android, kind, slots);
            }
        }
        public static void ConfigureClaw(ClawCycle cycle)
        {
            cycle.stabilizeFingerAnchors = true;
            foreach (var finger in cycle.fingers)
            {
                var body = finger.GetComponent<Rigidbody>();
                body.solverIterations = 40;
                body.solverVelocityIterations = 20;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            var suspension = cycle.hub.GetComponent<ConfigurableJoint>();
            suspension.projectionMode = JointProjectionMode.PositionAndRotation;
            suspension.projectionDistance = .005f;
            suspension.projectionAngle = 10;
        }
        public static void Check()
        {
            Generate();
            WeekThirteenDiagnostics.Run();
            Debug.Log("PICKDOLL_WEEK13_VALIDATION_PASS");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        public static void BuildAll() { BuildWindows(); BuildAndroid(); }
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Week13/Windows/PickDoll.exe");
        public static void BuildAndroid()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Week13/Android/PickDoll-week13.apk");
        }
        static void Build(BuildTarget target, string path)
        {
            Generate();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, path, target, BuildOptions.Development);
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Week13 build failed");
            Debug.Log(target == BuildTarget.Android ? "PICKDOLL_WEEK13_ANDROID_READY" : "PICKDOLL_WEEK13_WINDOWS_READY");
        }
    }
}
