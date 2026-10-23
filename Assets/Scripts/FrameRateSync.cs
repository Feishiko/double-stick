using UnityEngine;

public static class FrameRateSync
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        double refreshRate = Screen.currentResolution.refreshRateRatio.value;
        if (refreshRate <= 0.0)
        {
            refreshRate = Screen.currentResolution.refreshRate;
        }

        if (refreshRate <= 0.0)
        {
            return;
        }

        Time.fixedDeltaTime = (float)(1.0 / refreshRate);
    }
}
