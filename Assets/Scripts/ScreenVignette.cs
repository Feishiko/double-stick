using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScreenVignette : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Color color = Color.black;
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 0.55f;
    [Range(0f, 1.5f)]
    [SerializeField] private float innerRadius = 0.45f;
    [Range(0.1f, 2f)]
    [SerializeField] private float outerRadius = 1.25f;

    [Header("Setup")]
    [SerializeField] private int sortingOrder = -1;
    [SerializeField] private int textureSize = 256;

    private static ScreenVignette instance;
    private GameObject canvasObject;
    private Texture2D vignetteTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            return;
        }

        instance = this;
        BuildVignette();
    }

    private void BuildVignette()
    {
        if (canvasObject != null)
        {
            return;
        }

        vignetteTexture = BuildTexture();

        canvasObject = new GameObject(
            "ScreenVignetteCanvas",
            typeof(RectTransform),
            typeof(Canvas));
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        GameObject imageObject = new GameObject(
            "Vignette",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = vignetteTexture;
        image.color = color;
        image.raycastTarget = false;
    }

    private Texture2D BuildTexture()
    {
        int size = Mathf.Max(16, textureSize);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ScreenVignetteTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.DontSave;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            float ny = ((y + 0.5f) / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size) * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float t = Mathf.InverseLerp(innerRadius, outerRadius, distance);
                t = Mathf.SmoothStep(0f, 1f, t);
                pixels[y * size + x] = new Color(1f, 1f, 1f, t * intensity);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (vignetteTexture != null)
        {
            Destroy(vignetteTexture);
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}
