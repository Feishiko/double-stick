using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ScreenRGBSplit : MonoBehaviour
{
    [SerializeField] private bool enable = true;
    [SerializeField]
    [Range(0f, 0.02f)]
    private float amount = 0.0025f;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RemoveFromCurrentCamera();
    }

    private void Start()
    {
        EnsureOnCamera();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureOnCamera();
    }

    private void EnsureOnCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        RGBSplitEffect effect = targetCamera.GetComponent<RGBSplitEffect>();

        if (!enable || amount <= 0f)
        {
            if (effect != null)
            {
                Destroy(effect);
            }

            return;
        }

        if (effect == null)
        {
            effect = targetCamera.gameObject.AddComponent<RGBSplitEffect>();
        }

        effect.Configure(amount);
    }

    private void RemoveFromCurrentCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        RGBSplitEffect effect = targetCamera.GetComponent<RGBSplitEffect>();
        if (effect != null)
        {
            Destroy(effect);
        }
    }
}
