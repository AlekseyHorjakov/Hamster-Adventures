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

        int dpi = System.Array.IndexOf(cli, "-diagpike");
        if (dpi >= 0)
        {
            var hero = players.FirstOrDefault(pl => pl.name == "Hamster");
            var pike = Object.FindFirstObjectByType<PikeLeap>();
            if (hero != null)
            {
                hero.Body.simulated = true;
                hero.Body.position = new Vector2(346.8f, 1.2f);
                hero.Body.linearVelocity = Vector2.zero;
                var heroHealth = hero.GetComponent<Health>();
                float startHealth = heroHealth != null ? heroHealth.Current : -1f;
                float minHealth = startHealth;
                Vector2 startPos = hero.transform.position;
                float moved = 0f;
                for (int i = 0; i < 60; i++)
                {
                    yield return new WaitForSecondsRealtime(0.1f);
                    if (heroHealth != null)
                        minHealth = Mathf.Min(minHealth, heroHealth.Current);
                    moved = Mathf.Max(moved, Vector2.Distance(startPos, hero.transform.position));
                }
                Debug.Log("[Pike] pike=" + (pike != null ? pike.name : "NO") +
                          " hpStart=" + startHealth.ToString("F1") +
                          " hpMin=" + minHealth.ToString("F1") +
                          " moved=" + moved.ToString("F2") +
                          " pos=" + hero.transform.position.ToString("F2"));
            }
        }

        var heroForLog = players.FirstOrDefault(pl => pl.name == "Hamster");
        if (heroForLog != null && Camera.main != null)
        {
            yield return new WaitForSecondsRealtime(3f);
            var groundHits = Physics2D.OverlapBoxAll(new Vector2(heroForLog.transform.position.x, heroForLog.GetComponent<Collider2D>().bounds.min.y + 0.05f), new Vector2(0.4f, 0.15f), 0f);
            foreach (var hit in groundHits)
                Debug.Log("[Frame] under=" + hit.name + " type=" + hit.GetType().Name + " top=" + hit.bounds.max.y.ToString("F2") + " parent=" + (hit.transform.parent == null ? "-" : hit.transform.parent.name));
            Debug.Log("[Frame] cam=" + Camera.main.transform.position.ToString("F2") + " ortho=" + Camera.main.orthographicSize.ToString("F2") +
                      " hero=" + heroForLog.transform.position.ToString("F2") +
                      " ground=" + (heroForLog.GetComponent<Collider2D>() != null ? heroForLog.GetComponent<Collider2D>().bounds.min.y.ToString("F2") : "-"));
        }

        int si = System.Array.IndexOf(cli, "-shot");
        if (si >= 0 && si + 1 < cli.Length)
        {
            yield return new WaitForSecondsRealtime(2.5f);
            string path = cli[si + 1];
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(0.6f);
            Debug.Log("[Shot] saved " + path + " hero=" + (heroForLog != null ? heroForLog.transform.position.ToString("F2") : "-"));
        }

        int sti = System.Array.IndexOf(cli, "-diagstand");
        if (sti >= 0 && sti + 1 < cli.Length && float.TryParse(cli[sti + 1], out float standX))
        {
            var hero = players.FirstOrDefault(pl => pl.name == "Hamster");
            if (hero != null)
            {
                hero.Body.simulated = true;
                hero.Body.position = new Vector2(standX, 12f);
                hero.Body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(1.5f);
                for (int i = 0; i < 6; i++)
                {
                    var hits = Physics2D.OverlapBoxAll(new Vector2(hero.transform.position.x, hero.GetComponent<Collider2D>().bounds.min.y - 0.05f), new Vector2(0.5f, 0.12f), 0f);
                    string under = "none";
                    foreach (var h in hits)
                        if (h != null && !h.isTrigger && !h.transform.IsChildOf(hero.transform)) { var srUnder = h.GetComponentInChildren<SpriteRenderer>(true); under = h.name + " top=" + h.bounds.max.y.ToString("F2") + " sprite=" + (srUnder == null || srUnder.sprite == null ? "NONE" : srUnder.sprite.name + " spriteTop=" + srUnder.bounds.max.y.ToString("F2")); break; }
                    Debug.Log("[Stand] t=" + (i * 0.5f).ToString("F1") + " pos=" + hero.transform.position.ToString("F2") + " under=" + under);
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
        }

        int ji = System.Array.IndexOf(cli, "-diagjump");
        if (ji >= 0 && ji + 1 < cli.Length && float.TryParse(cli[ji + 1], out float jumpX))
        {
            var hero = players.FirstOrDefault(pl => pl.name == "Hamster");
            if (hero != null)
            {
                hero.Body.simulated = true;
                hero.Body.position = new Vector2(jumpX, 12f);
                hero.Body.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(2f);
                float startY = hero.transform.position.y;
                hero.DebugJump();
                float maxY = startY;
                for (int i = 0; i < 20; i++)
                {
                    yield return new WaitForSecondsRealtime(0.05f);
                    maxY = Mathf.Max(maxY, hero.transform.position.y);
                }
                Debug.Log("[Jump] x=" + jumpX + " start=" + startY.ToString("F2") + " max=" + maxY.ToString("F2") + " rise=" + (maxY - startY).ToString("F2"));
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








