using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float holdBeforeLoad = 0.05f;

    private void Awake()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>(true);
        }
    }

    public void Configure(Image image)
    {
        fadeImage = image;
    }

    public void FadeAndReload(string sceneName)
    {
        DontDestroyOnLoad(gameObject);
        SetAlpha(1f);
        StartCoroutine(FadeRoutine(sceneName));
    }

    public void PlayTransition(
        string sceneName,
        float fadeInTime,
        float holdTime,
        float fadeOutTime,
        System.Action onComplete = null)
    {
        DontDestroyOnLoad(gameObject);
        StopAllCoroutines();
        StartCoroutine(TransitionRoutine(sceneName, fadeInTime, holdTime, fadeOutTime, onComplete));
    }

    private IEnumerator TransitionRoutine(
        string sceneName,
        float fadeInTime,
        float holdTime,
        float fadeOutTime,
        System.Action onComplete)
    {
        SetAlpha(0f);
        yield return FadeAlpha(0f, 1f, fadeInTime);
        yield return new WaitForSecondsRealtime(holdTime);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load != null)
        {
            yield return load;
        }

        yield return null;
        yield return FadeAlpha(1f, 0f, fadeOutTime);

        SetAlpha(0f);
        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(to);
    }

    private IEnumerator FadeRoutine(string sceneName)
    {
        yield return new WaitForSecondsRealtime(holdBeforeLoad);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load != null)
        {
            yield return load;
        }

        yield return null;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        SetAlpha(0f);
        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
        fadeImage.raycastTarget = alpha > 0.001f;
    }
}
