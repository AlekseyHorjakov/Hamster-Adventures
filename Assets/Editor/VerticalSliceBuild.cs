using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Собирает вертикальный срез в готовый Windows-билд.</summary>
public static class VerticalSliceBuild
{
    private const string ScenePath = "Assets/Scenes/Level01_VerticalSlice.unity";
    private const string OutputDirectory = "D:/Build/HamsterAdventures";
    private const string OutputExe = OutputDirectory + "/HamsterAdventures.exe";

    [MenuItem("Tools/Hamster/Build Windows Vertical Slice")]
    public static void BuildWindows64()
    {
        string[] scenes = { ScenePath };
        if (scenes.Any(scene => AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null))
        {
            Debug.LogError("[Build] Нет сцены " + ScenePath);
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(OutputDirectory);

        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputExe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Готово: {OutputExe}, {summary.totalSize / (1024f * 1024f):0.0} МБ");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[Build] Ошибка сборки: " + summary.result + " errors=" + summary.totalErrors);
            EditorApplication.Exit(1);
        }
    }
}
