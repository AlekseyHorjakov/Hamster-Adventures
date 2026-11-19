using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Щука у переправы: должна снимать ровно половину здоровья героя.
/// Как и остальные Play Mode тесты проекта, обращается к игровым типам через reflection.
/// </summary>
public sealed class PikeLeapPlayModeTests
{
    private static Type TypeOf(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name, false))
            .FirstOrDefault(type => type != null);
    }

    [UnityTest]
    public IEnumerator Pike_TakesHalfHealth_FromHeroInCrossing()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("Level01_Full");
        while (load != null && !load.isDone)
            yield return null;

        yield return new WaitForSecondsRealtime(1.5f);

        Type healthType = TypeOf("Health");
        Assert.IsNotNull(healthType, "нет типа Health");

        GameObject hero = GameObject.Find("Hamster");
        Assert.IsNotNull(hero, "нет объекта Hamster");

        Component health = hero.GetComponent(healthType);
        Assert.IsNotNull(health, "у героя нет Health");

        var currentProperty = healthType.GetProperty("Current");
        var maxProperty = healthType.GetProperty("Max");
        Assert.IsNotNull(currentProperty, "нет Health.Current");
        Assert.IsNotNull(maxProperty, "нет Health.Max");

        float max = (float)maxProperty.GetValue(health);
        float start = (float)currentProperty.GetValue(health);

        var body = hero.GetComponent<Rigidbody2D>();

        bool sawHalfHealthLoss = false;
        float minHealth = start;

        for (int i = 0; i < 60; i++)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            // Держим героя в зоне удара: движение героя сбрасывает gravityScale каждый кадр,
            // поэтому просто возвращаем его в промежуток между секциями моста.
            if (body != null)
                body.position = new Vector2(346.8f, 0.1f);
            hero.transform.position = new Vector3(346.8f, 0.1f, 0f);

            float now = (float)currentProperty.GetValue(health);
            minHealth = Mathf.Min(minHealth, now);
            if (Mathf.Abs(start - now - max * 0.5f) < 0.6f)
                sawHalfHealthLoss = true;
        }

        Debug.Log($"[PikeTest] start={start:F1} min={minHealth:F1} max={max:F1}");
        Assert.IsTrue(sawHalfHealthLoss, $"щука не сняла половину здоровья: start={start}, min={minHealth}");
    }
}
