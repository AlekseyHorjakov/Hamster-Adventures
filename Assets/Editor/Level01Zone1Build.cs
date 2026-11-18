using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Собирает Windows-билд первой зоны полного уровня.</summary>
public static class Level01Zone1Build
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/Scenes/Level01_Full.unity";
    private const string OutputDirectory = "D:/Build/HamsterAdventures_Level01_Zone1";
    private const string OutputExe = OutputDirectory + "/HamsterAdventures_Level01_Zone1.exe";

    [MenuItem("Tools/Хомяк/СБОРКА: Windows — Зона I")]
    public static void BuildWindows64()
    {
        string[] scenes = { MenuScenePath, LevelScenePath };
        if (scenes.Any(scene => AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null))
        {
            Debug.LogError("[Zone1Build] Нет одной из сцен: " + string.Join(", ", scenes));
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(OutputDirectory);

        PlayerSettings.productName = "Hamster Adventures";
        PlayerSettings.bundleVersion = "zone1-" + System.DateTime.Now.ToString("MMdd-HHmm");
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
            WriteBuildInfo();
            Debug.Log($"[Zone1Build] Готово: {OutputExe}, {summary.totalSize / (1024f * 1024f):0.0} МБ");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[Zone1Build] Ошибка сборки: " + summary.result + " errors=" + summary.totalErrors);
            EditorApplication.Exit(1);
        }
    }

    private static void WriteBuildInfo()
    {
        string controls = string.Join("\n", new[]
        {
            "HAMSTER ADVENTURES — LEVEL 01, ЗОНА I",
            "Комнаты 01–05: Лагерь → Выход из лагеря → Лесная поляна → Тренировочная площадка → Старый мост.",
            "",
            "ЗАПУСК",
            "HamsterAdventures_Level01_Zone1.exe",
            "",
            "УПРАВЛЕНИЕ",
            "Хомяк: A/D — движение, W — прыжок, F — атака, R — бросок, Q — супер, E — взаимодействие.",
            "Хомячиха: J/L — движение, I — прыжок, K — атака, P — бросок, U — супер, O — взаимодействие.",
            "",
            "КАК ПРОВЕРЯТЬ",
            "1. Новая игра → Один игрок или Два игрока.",
            "2. Пройти вправо через комнаты 01–05.",
            "3. В комнате 04 войти в зону боя: появятся две гиены, ворота закроются.",
            "4. Победить обеих гиен — ворота откроются.",
            "5. Разбить ящик и бочку, проверить награду-жёлудь.",
            "6. Упасть в яму/обрыв — пара должна вернуться к костру-чекпоинту.",
            "7. Перейти мост и дойти до правого края — появится экран завершения зоны I.",
            "",
            "Примечание: это контрольная точка зоны I. Комнаты 06–18 будут добавлены на следующих этапах."
        });
        File.WriteAllText(Path.Combine(OutputDirectory, "КАК_ТЕСТИРОВАТЬ.txt"), controls);
    }
}
