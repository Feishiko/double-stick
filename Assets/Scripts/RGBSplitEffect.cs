using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RGBSplitEffect : MonoBehaviour
{
    [SerializeField] private float amount = 0.0025f;

    private Shader rgbSplitShader;
    private Material material;

    public void Configure(float newAmount)
    {
        amount = Mathf.Max(0f, newAmount);
    }

    private void Awake()
    {
        rgbSplitShader = Resources.Load<Shader>("ScreenRGBSplit");
        if (rgbSplitShader == null)
        {
            Debug.LogWarning("RGBSplitEffect: Resources/ScreenRGBSplit.shader not found.");
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (amount <= 0f || rgbSplitShader == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (material == null)
        {
            material = new Material(rgbSplitShader);
        }

        material.SetFloat("_Amount", amount);
        Graphics.Blit(source, destination, material);
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}
