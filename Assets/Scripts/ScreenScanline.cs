using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScreenScanline : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Color color = Color.black;
    [Range(0f, 1f)]
    [SerializeField] private float intensity = 0.18f;
    [Min(0.1f)]
    [SerializeField] private float lineSpacing = 3f;
    [Range(0.01f, 1f)]
    [SerializeField] private float lineThickness = 0.35f;
    [SerializeField] private float scrollSpeed = 0f;

    [Header("Setup")]
    [SerializeField] private int sortingOrder = -2;

    private static ScreenScanline instance;
    private GameObject canvasObject;
    private Material scanlineMaterial;

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
        Build();
    }

    private void Build()
    {
        if (canvasObject != null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>("ScreenScanline");
        if (shader == null)
        {
            Debug.LogWarning("ScreenScanline: Resources/ScreenScanline.shader not found.");
            return;
        }

        scanlineMaterial = new Material(shader);
        ApplyProperties();

        canvasObject = new GameObject(
            "ScreenScanlineCanvas",
            typeof(RectTransform),
            typeof(Canvas));
        DontDestroyOnLoad(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        GameObject imageObject = new GameObject(
            "Scanline",
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
        image.texture = Texture2D.whiteTexture;
        image.material = scanlineMaterial;
        image.raycastTarget = false;
    }

    private void ApplyProperties()
    {
        if (scanlineMaterial == null)
        {
            return;
        }

        scanlineMaterial.SetColor("_Color", color);
        scanlineMaterial.SetFloat("_Intensity", intensity);
        scanlineMaterial.SetFloat("_LineSpacing", Mathf.Max(0.1f, lineSpacing));
        scanlineMaterial.SetFloat("_LineThickness", Mathf.Clamp(lineThickness, 0.01f, 1f));
        scanlineMaterial.SetFloat("_ScrollSpeed", scrollSpeed);
    }

    private void OnValidate()
    {
        ApplyProperties();
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }

        if (scanlineMaterial != null)
        {
            Destroy(scanlineMaterial);
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}
