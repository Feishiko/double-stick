using UnityEngine;

public class StickAfterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private StickController owner;
    private float lifetime;
    private float elapsed;
    private Color startColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Begin(StickController owner, float lifetime, float startAlpha)
    {
        this.owner = owner;
        this.lifetime = Mathf.Max(0.01f, lifetime);
        elapsed = 0f;
        startColor = spriteRenderer.color;
        startColor.a = startAlpha;
        spriteRenderer.color = startColor;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        Color color = startColor;
        color.a = Mathf.Lerp(startColor.a, 0f, t);
        spriteRenderer.color = color;

        if (t >= 1f)
        {
            if (owner != null)
            {
                owner.RecycleGhost(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
