using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class YSort2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SortingGroup sortingGroup;

    [Tooltip("Extra offset added to the transform Y used for sorting.")]
    [SerializeField] private float yOffset;

    [Tooltip("Higher precision keeps sub-unit depth differences.")]
    [SerializeField] private float sortingPrecision = 100f;

    [Tooltip("Base order. Keep it above the floor tilemap's sorting order.")]
    [SerializeField] private int baseOrder = 10000;

    private int lastOrder = int.MinValue;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplySorting();
    }

    private void LateUpdate()
    {
        ApplySorting();
    }

    private void OnValidate()
    {
        ResolveReferences();
        if (!Application.isPlaying)
        {
            ApplySorting();
        }
    }

    private void ResolveReferences()
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }

    private void ApplySorting()
    {
        float precision = sortingPrecision > 0f ? sortingPrecision : 1f;
        int order = baseOrder - Mathf.RoundToInt((transform.position.y + yOffset) * precision);

        if (order == lastOrder)
        {
            return;
        }

        lastOrder = order;

        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
}
