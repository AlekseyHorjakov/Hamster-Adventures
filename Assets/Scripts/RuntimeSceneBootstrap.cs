using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// RuntimeInitializeOnLoadMethod вызывается только при старте приложения,
/// поэтому после перезагрузки сцены заново создаём HUD, паузу и выбор режима.
/// </summary>
public static class RuntimeSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Subscribe()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Object.FindFirstObjectByType<PauseMenu>() == null)
            new GameObject("PauseMenu").AddComponent<PauseMenu>();

        if (Object.FindFirstObjectByType<ControlsHintHud>() == null)
            new GameObject("ControlsHintHUD").AddComponent<ControlsHintHud>();

        bool hasPlayers = Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
            .Any(health => health.name.Contains("Hamster"));

        if (hasPlayers && Object.FindFirstObjectByType<HealthHud>() == null)
            new GameObject("HealthHUD").AddComponent<HealthHud>();

        if (!GameSession.HasSelection && Object.FindFirstObjectByType<GameModeSelector>() == null)
            new GameObject("GameModeSelector").AddComponent<GameModeSelector>();
    }
}
