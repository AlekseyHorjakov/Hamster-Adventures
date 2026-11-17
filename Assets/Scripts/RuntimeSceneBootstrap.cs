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
        if (GameSession.HasSelection)
            GameSession.ApplyModeToPlayers();

        if (Object.FindFirstObjectByType<PauseMenu>() == null)
            new GameObject("PauseMenu").AddComponent<PauseMenu>();


        bool hasPlayers = Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
            .Any(health => health.name.Contains("Hamster"));

        if (hasPlayers && Object.FindFirstObjectByType<HealthHud>() == null)
            new GameObject("HealthHUD").AddComponent<HealthHud>();

        if (Object.FindFirstObjectByType<LevelFlowController>() != null
            && Object.FindFirstObjectByType<LevelArtController>() == null
            && GameObject.Find("BakedLevelArt") == null)
            new GameObject("LevelArt").AddComponent<LevelArtController>();

        if (System.Environment.GetCommandLineArgs().Contains("-diag"))
        {
            var go = new GameObject("Diag");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DiagRunner>();
        }
    }
}

/// <summary>Диагностика раскладки уровня в собранном билде (ключ -diag).</summary>
public sealed class DiagRunner : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(2.0f);
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            GameSession.SetMode(GameMode.Coop);
            GameProgress.StartNew(GameMode.Coop);
            yield return SceneManager.LoadSceneAsync("Level01_Full");
            yield return new WaitForSecondsRealtime(2.5f);
        }
        Camera cam = Camera.main;
        Debug.Log("[Diag] scene=" + SceneManager.GetActiveScene().name +
                  " cam=" + (cam == null ? "NULL" : cam.transform.position.ToString("F2") + " ortho=" + cam.orthographicSize.ToString("F2")));

        foreach (string n in new[] { "CampTent_Green", "CampTent_Beige", "CampTree_Oak", "CampChest_A", "Checkpoint_Camp", "LowRun_Tile", "GroundRuns" })
        {
            GameObject go = GameObject.Find(n);
            if (go == null) { Debug.Log("[Diag] " + n + "=NULL"); continue; }
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            Debug.Log("[Diag] " + n + " pos=" + go.transform.position.ToString("F2") +
                      (sr == null ? "" : " sprite=" + (sr.sprite == null ? "NULL" : sr.sprite.name) +
                       " bottom=" + sr.bounds.min.y.ToString("F2") + " top=" + sr.bounds.max.y.ToString("F2")));
        }

        HamsterMovement[] players = Object.FindObjectsByType<HamsterMovement>(FindObjectsInactive.Exclude);
        foreach (var pl in players)
        {
            var col = pl.GetComponent<Collider2D>();
            Debug.Log("[Diag] player " + pl.name + " pos=" + pl.transform.position.ToString("F2") +
                      " bottom=" + (col == null ? 0f : col.bounds.min.y).ToString("F2"));
        }

        string[] cli = System.Environment.GetCommandLineArgs();
        int xi = System.Array.IndexOf(cli, "-diagx");
        if (xi >= 0 && xi + 1 < cli.Length && float.TryParse(cli[xi + 1], out float spawnX))
        {
            var hero = players.FirstOrDefault(pl => pl.name == "Hamster");
            if (hero != null)
            {
                hero.Body.simulated = true;
                hero.Body.position = new Vector2(spawnX, 12f);
                hero.Body.linearVelocity = Vector2.zero;
            }
        }

        int wi = System.Array.IndexOf(cli, "-diagwalk");
        if (wi >= 0 && wi + 1 < cli.Length && float.TryParse(cli[wi + 1], out float walkX))
        {
            var hero = players.FirstOrDefault(pl => pl.name == "Hamster");
            if (hero != null)
            {
                hero.Body.simulated = true;
                hero.Body.position = new Vector2(walkX, 12f);
                hero.Body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(1.2f);
                hero.SetPartner(null);
                hero.DebugMoveOverride = 1f;
                for (int i = 0; i < 14; i++)
                {
                    yield return new WaitForSecondsRealtime(0.35f);
                    Debug.Log("[DiagWalk] x=" + hero.transform.position.x.ToString("F1") +
                              " y=" + hero.transform.position.y.ToString("F2"));
                }
                hero.DebugMoveOverride = 0f;
            }
        }

        if (cli.Contains("-diagpit"))
        {
            var hamster = players.FirstOrDefault(pl => pl.name == "Hamster");
            if (hamster != null)
            {
                hamster.Body.simulated = true;
                hamster.Body.position = new Vector2(105f, 0.6f);
                hamster.Body.linearVelocity = Vector2.zero;
                for (int i = 0; i < 16; i++)
                {
                    yield return new WaitForSecondsRealtime(0.25f);
                    var st = hamster.GetComponent<PlayerCombatState>();
                    Debug.Log("[DiagPit] t=" + (i * 0.25f).ToString("F2") +
                              " y=" + hamster.transform.position.y.ToString("F2") +
                              " x=" + hamster.transform.position.x.ToString("F2") +
                              " dead=" + (st != null && st.IsDead) +
                              " respawn=" + (st != null && st.IsRespawning) +
                              " camY=" + (Camera.main == null ? 0f : Camera.main.transform.position.y).ToString("F2"));
                }
            }
        }
    }
}


