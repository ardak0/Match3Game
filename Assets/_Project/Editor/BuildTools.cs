using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Match3.EditorTools
{
    /// <summary>
    /// Tools > Match3 > Build: one click for the two builds the project ships.
    ///   Android APK  -> Builds/Android/Match3.apk (Build And Run Android also installs it on a phone that is plugged in)
    ///   WebGL        -> Builds/WebGL (the web page) and Builds/Match3-WebGL.zip (what you upload to itch.io)
    /// Every build first applies the settings below, so the numbers live in code and a fresh machine builds the same way.
    /// The Builds folder is in .gitignore.
    /// </summary>
    public static class BuildTools
    {
        private const string AndroidPackageName = "com.ardakeles.match3";
        private const int WebCanvasWidth = 540;   // portrait 9:16, the shape of the game
        private const int WebCanvasHeight = 960;

        private const string AndroidApkPath = "Builds/Android/Match3.apk";
        private const string WebFolder = "Builds/WebGL";
        private const string WebZipPath = "Builds/Match3-WebGL.zip";

        // ---------- menu ----------

        [MenuItem("Tools/Match3/Build/Apply Build Settings")]
        private static void ApplyMenu()
        {
            ApplyBuildSettings();
            Debug.Log("Build settings applied (portrait, package " + AndroidPackageName + ", WebGL " + WebCanvasWidth + "x" + WebCanvasHeight + ", no compression).");
        }

        [MenuItem("Tools/Match3/Build/Android APK")]
        private static void BuildAndroid()
        {
            BuildApk(BuildOptions.None);
        }

        [MenuItem("Tools/Match3/Build/Build And Run Android (phone on USB)")]
        private static void BuildAndRunAndroid()
        {
            BuildApk(BuildOptions.AutoRunPlayer);
        }

        [MenuItem("Tools/Match3/Build/WebGL (itch.io zip)")]
        private static void BuildWebGl()
        {
            if (!IsModuleInstalled(BuildTargetGroup.WebGL, BuildTarget.WebGL, "WebGL Build Support")) return;
            ApplyBuildSettings();

            BuildTarget previousTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup previousGroup = BuildPipeline.GetBuildTargetGroup(previousTarget);

            string folder = ProjectPath(WebFolder);
            bool built = Build(BuildTarget.WebGL, BuildTargetGroup.WebGL, folder, BuildOptions.None);

            // The build switched the active platform to WebGL. Put it back, so the editor stays on Android afterwards.
            if (previousTarget != BuildTarget.WebGL) EditorUserBuildSettings.SwitchActiveBuildTarget(previousGroup, previousTarget);

            if (!built) return;

            string zipPath = ProjectPath(WebZipPath);
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // itch.io wants index.html at the top of the zip, so the zip is made from INSIDE the build folder.
            ZipFile.CreateFromDirectory(folder, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

            Debug.Log("WebGL zip ready for itch.io: " + zipPath);
            EditorUtility.RevealInFinder(zipPath);
        }

        // ---------- settings ----------

        // Everything a build needs that the Player Settings window would otherwise hold.
        private static void ApplyBuildSettings()
        {
            // Both platforms: the game is portrait only (the template default is "Auto Rotation").
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Android: a package name of our own instead of the template's "com.DefaultCompany.urp_2d".
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageName);

            // WebGL: a portrait canvas, and no compression. itch.io compresses its downloads itself, and a
            // file that is compressed twice (or with a format the server does not announce) fails to load in the browser.
            PlayerSettings.defaultWebScreenWidth = WebCanvasWidth;
            PlayerSettings.defaultWebScreenHeight = WebCanvasHeight;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
        }

        // ---------- building ----------

        private static void BuildApk(BuildOptions extraOptions)
        {
            if (!IsModuleInstalled(BuildTargetGroup.Android, BuildTarget.Android, "Android Build Support")) return;
            ApplyBuildSettings();

            Build(BuildTarget.Android, BuildTargetGroup.Android, ProjectPath(AndroidApkPath), extraOptions);
        }

        // Builds the enabled scenes of the Build Settings (Home first, then Game). Returns true on success.
        private static bool Build(BuildTarget target, BuildTargetGroup group, string outputPath, BuildOptions options)
        {
            Directory.CreateDirectory(target == BuildTarget.WebGL ? outputPath : Path.GetDirectoryName(outputPath));

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = target,
                targetGroup = group,
                options = options
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Build for " + target + " ended with " + summary.result + " (" + summary.totalErrors + " errors). See the Console above.");
                return false;
            }

            Debug.Log("Build for " + target + " succeeded: " + outputPath + " (" + (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalTime.TotalSeconds.ToString("0") + " s)");
            return true;
        }

        private static string[] GetEnabledScenes()
        {
            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) scenes.Add(scene.path);
            }

            return scenes.ToArray();
        }

        // A platform module is a separate install in Unity Hub. Without it, BuildPlayer fails with a long, confusing message.
        private static bool IsModuleInstalled(BuildTargetGroup group, BuildTarget target, string moduleName)
        {
            if (BuildPipeline.IsBuildTargetSupported(group, target)) return true;

            EditorUtility.DisplayDialog(
                "Platform module missing",
                "Unity has no \"" + moduleName + "\" installed for this editor version.\n\n"
                + "Open Unity Hub > Installs > the gear of this editor version > Add modules > tick \"" + moduleName + "\" > Install, "
                + "then restart Unity and run the menu item again.",
                "OK");
            return false;
        }

        // The project folder (the one with Assets/ in it).
        private static string ProjectPath(string relativePath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
        }
    }
}
