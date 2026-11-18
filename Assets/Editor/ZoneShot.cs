using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Быстрый редакторный рендер для проверки стойки на платформах Зоны II.</summary>
public static class ZoneShot
{
    [MenuItem("Tools/Hamster/Снимки Зоны II")]
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Level01_Full.unity", OpenSceneMode.Single);
        var camera = Object.FindFirstObjectByType<Camera>();
        camera.aspect = 16f / 9f;
        var hero = GameObject.Find("Hamster");
        var girl = GameObject.Find("HamsterGirl");

        var shots = new (string name, float x, float y, float size, Vector2 hero)[]
        {
            ("islands", 283f, 5.5f, 8f, new Vector2(279f, 3.98f)),
            ("beach", 228f, 3.2f, 8f, new Vector2(222f, 1.58f)),
            ("stones", 252f, 3.0f, 8f, new Vector2(251f, 2.28f)),
            ("ledges", 370f, 8.0f, 8f, new Vector2(369f, 7.48f))
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
