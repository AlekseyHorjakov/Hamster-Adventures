using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Быстрый редакторный рендер для проверки стойки на платформах Зоны II.</summary>
public static class ZoneShot
{
    [MenuItem("Tools/Хомяк/Зона II: снимки для проверки")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var camera = Object.FindFirstObjectByType<Camera>();
        camera.aspect = 16f / 9f;
        var hero = GameObject.Find("Hamster");
        var girl = GameObject.Find("HamsterGirl");

        var shots = new (string name, float x, float y, float size, Vector2 hero)[]
        {
            ("board", 269f, -0.5f, 7f, new Vector2(268f, -0.9f)),
            ("bridges", 368f, 7.5f, 9f, new Vector2(364f, 6.3f)),
            ("lakewide", 294f, 6f, 14f, new Vector2(288f, 3.6f)),
            ("waterfall", 181f, 9f, 14f, new Vector2(176f, 3.5f))
        };

        Directory.CreateDirectory("C:/Windows/Temp/zshots2");
        var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);

        foreach (var shot in shots)
        {
            camera.orthographicSize = shot.size;
            camera.transform.position = new Vector3(shot.x, shot.y, -10f);
            if (hero != null)
                hero.transform.position = new Vector3(shot.hero.x, shot.hero.y, 0f);
            if (girl != null)
                girl.transform.position = new Vector3(shot.hero.x + 1.3f, shot.hero.y + 0.05f, 0f);

            camera.targetTexture = rt;
            camera.Render();
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes("C:/Windows/Temp/zshots2/" + shot.name + ".png", texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        camera.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
        Debug.Log("[ZoneShot] готово");
    }
}
