using System.Collections;
using UnityEngine;

public static class HitStop
{
    private static int framesRemaining;
    private static float previousTimeScale = 1f;
    private static HitStopRunner runner;

    public static void Request(int frames)
    {
        if (frames <= 0)
        {
            return;
        }

        if (framesRemaining > 0)
        {
            framesRemaining = Mathf.Max(framesRemaining, frames);
            return;
        }

        framesRemaining = frames;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (runner == null)
        {
            GameObject host = new GameObject("HitStopRunner");
            Object.DontDestroyOnLoad(host);
            runner = host.AddComponent<HitStopRunner>();
        }

        runner.StartCoroutine(Run());
    }

    private static IEnumerator Run()
    {
        while (framesRemaining > 0)
        {
            yield return new WaitForEndOfFrame();
            framesRemaining--;
        }

        Time.timeScale = previousTimeScale;
    }
}
