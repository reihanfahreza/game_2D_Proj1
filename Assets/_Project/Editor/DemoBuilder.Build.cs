using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>P7 Praktikum 5: pengaturan sebelum build + build Windows 64-bit.</summary>
public static partial class DemoBuilder
{
    public const string BuildFolder = "Builds/PulauMonster_Win";

    static readonly string[] BuildSceneList =
    {
        BootstrapScenePath,   // 0 Bootstrap
        TitleScenePath,       // 1 Title
        WorldScenePath,       // 2 World
        BattleScenePath,      // 3 Battle
        EndScenePath,         // 4 End
    };

    static void SetSceneList()
    {
        EditorBuildSettings.scenes = BuildSceneList.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
    }

    static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Politeknik Negeri Batam";
        PlayerSettings.productName = "Pulau Monster";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;       // Windowed 1280 x 720, Resizable
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.visibleInBackground = true;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Pulau Monster/Build Windows (.exe)", priority = 20)]
    public static void BuildWindowsPlayer()
    {
        ConfigurePlayerSettings();
        SetSceneList();
        Directory.CreateDirectory(BuildFolder);
        var options = new BuildPlayerOptions
        {
            scenes = BuildSceneList,
            locationPathName = BuildFolder + "/PulauMonster.exe",
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"[DemoBuilder] Build {summary.result}: {summary.totalSize / (1024f * 1024f):F1} MB, " +
                  $"{summary.totalErrors} error, {summary.totalWarnings} warning -> {summary.outputPath}");
        if (summary.result != BuildResult.Succeeded) throw new Exception($"Build gagal: {summary.result}");
    }
}
