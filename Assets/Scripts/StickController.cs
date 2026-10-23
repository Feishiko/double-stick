using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class StickController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform stickTransform;
    [SerializeField] private SpriteRenderer stickRenderer;
    [SerializeField] private SpriteRenderer playerRenderer;

    [Header("Aim")]
    [SerializeField] private float angleOffset;
    [SerializeField] private float maxAngularSpeed = 720f;

    [Header("Damage")]
    [SerializeField] private int damage = 10;

    [Header("Wall Blocking")]
    [SerializeField] private BoxCollider2D stickCollider;
    [SerializeField] private float wallCheckMinStep = 0.01f;
    [SerializeField] private float wallCheckThickness = 0.1f;
    [SerializeField] private float wallContactDistance = 0.15f;

    [Header("Afterimage")]
    [SerializeField] private float ghostAngleStep = 6f;
    [SerializeField] private float ghostLifetime = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float ghostStartAlpha = 0.6f;
    [SerializeField] private float ghostMinAngularSpeed = 120f;
    [SerializeField] private int maxGhostsPerFrame = 6;

    private readonly Stack<GameObject> ghostPool = new Stack<GameObject>();
    private readonly Collider2D[] wallHits = new Collider2D[16];
    private Transform ghostContainer;
    private int activeGhosts;
    private float previousAngle;
    private float accumulatedAngle;
    private bool hasPreviousAngle;

    public bool IsSwinging { get; private set; }
    public bool IsBlocked { get; private set; }
    public float AngularSpeed { get; private set; }
    public int ActiveGhostCount => activeGhosts;
    public int Damage => damage;

    public event Action<Collider2D> HitDetected;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (stickTransform == null)
        {
            stickTransform = transform.Find("Stick");
        }

        if (stickRenderer == null && stickTransform != null)
        {
            stickRenderer = stickTransform.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (stickCollider == null && stickTransform != null)
        {
            stickCollider = stickTransform.GetComponent<BoxCollider2D>();
        }

        if (playerRenderer == null)
        {
            Transform spriteTransform = transform.Find("Sprite");
            if (spriteTransform != null)
            {
                playerRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            }
        }

        ghostContainer = new GameObject("StickGhosts").transform;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || stickTransform == null || stickRenderer == null)
        {
            IsSwinging = false;
            IsBlocked = false;
            return;
        }

        Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (Vector2)(mouseWorld - stickTransform.position);
        if (direction.sqrMagnitude < 0.000001f)
        {
            AngularSpeed = 0f;
            IsSwinging = false;
            IsBlocked = false;
            return;
        }

        if (playerRenderer != null)
        {
            Vector3 scale = playerRenderer.transform.localScale;
            scale.x = direction.x < 0f ? -1f : 1f;
            playerRenderer.transform.localScale = scale;
        }

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
        float delta;
        float currentAngle;

        if (hasPreviousAngle)
        {
            float deltaToTarget = Mathf.DeltaAngle(previousAngle, targetAngle);
            float maxStep = maxAngularSpeed * Time.deltaTime;
            float step = Mathf.Clamp(deltaToTarget, -maxStep, maxStep);
            currentAngle = previousAngle + step;
            delta = step;
        }
        else
        {
            currentAngle = targetAngle;
            delta = 0f;
        }

        if (hasPreviousAngle && Mathf.Abs(delta) > wallCheckMinStep && WouldOverlapWall(currentAngle))
        {
            float freeAngle = previousAngle;
            float blockedAngle = currentAngle;
            for (int i = 0; i < 5; i++)
            {
                float midAngle = (freeAngle + blockedAngle) * 0.5f;
                if (WouldOverlapWall(midAngle))
                {
                    blockedAngle = midAngle;
                }
                else
                {
                    freeAngle = midAngle;
                }
            }

            currentAngle = freeAngle;
            delta = Mathf.DeltaAngle(previousAngle, freeAngle);
            IsBlocked = true;
        }
        else
        {
            IsBlocked = false;
        }

        hasPreviousAngle = true;
        currentAngle = Mathf.Repeat(currentAngle + 180f, 360f) - 180f;
        previousAngle = currentAngle;
        AngularSpeed = Mathf.Abs(delta) / Mathf.Max(Time.deltaTime, 0.0001f);
        stickTransform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        if (playerRenderer != null)
        {
            stickRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            stickRenderer.sortingOrder = playerRenderer.sortingOrder + 2;
        }

        if (AngularSpeed >= ghostMinAngularSpeed && ghostAngleStep > 0f)
        {
            accumulatedAngle += Mathf.Abs(delta);
            int spawned = 0;
            while (accumulatedAngle >= ghostAngleStep && spawned < maxGhostsPerFrame)
            {
                accumulatedAngle -= ghostAngleStep;
                SpawnGhost();
                spawned++;
            }

            if (spawned >= maxGhostsPerFrame)
            {
                accumulatedAngle = 0f;
            }
        }
        else
        {
            accumulatedAngle = 0f;
        }

        IsSwinging = activeGhosts > 0;
    }

    public void Configure(Transform stick, SpriteRenderer stickSprite, SpriteRenderer playerSprite)
    {
        stickTransform = stick;
        stickRenderer = stickSprite;
        playerRenderer = playerSprite;
    }

    private bool WouldOverlapWall(float angle)
    {
        if (stickTransform == null)
        {
            return false;
        }

        return OverlapsWall(stickTransform.position, angle);
    }

    public bool WouldHitWallAt(Vector2 pivotPosition)
    {
        if (stickCollider == null || stickTransform == null)
        {
            return false;
        }

        return OverlapsWall(pivotPosition, stickTransform.eulerAngles.z);
    }

    public bool IsStuckAgainstWall()
    {
        if (stickCollider == null || stickTransform == null)
        {
            return false;
        }

        float angle = stickTransform.eulerAngles.z;
        Vector2 probePosition = (Vector2)stickTransform.position
            + TipDirection * wallContactDistance;

        return OverlapsWall(probePosition, angle)
            || OverlapsWall(stickTransform.position, angle);
    }

    public Vector2 TipDirection
    {
        get
        {
            if (stickCollider == null || stickTransform == null)
            {
                return Vector2.right;
            }

            float dirSign = stickCollider.offset.x >= 0f ? 1f : -1f;
            return (Vector2)(Quaternion.Euler(0f, 0f, stickTransform.eulerAngles.z)
                * new Vector3(dirSign, 0f, 0f));
        }
    }

    private bool OverlapsWall(Vector2 pivotPosition, float angle)
    {
        if (stickCollider == null)
        {
            return false;
        }

        Vector3 scale = stickTransform.lossyScale;
        Vector3 localOffset = new Vector3(
            stickCollider.offset.x * scale.x,
            stickCollider.offset.y * scale.y,
            0f);
        Vector2 center = pivotPosition
            + (Vector2)(Quaternion.Euler(0f, 0f, angle) * localOffset);

        Vector2 size = new Vector2(
            Mathf.Abs(stickCollider.size.x * scale.x),
            Mathf.Abs(Mathf.Min(stickCollider.size.y, wallCheckThickness) * scale.y));

        int count = Physics2D.OverlapBoxNonAlloc(center, size, angle, wallHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = wallHits[i];
            if (hit == null || hit.isTrigger)
            {
                continue;
            }

            Transform hitTransform = hit.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            Rigidbody2D hitBody = hit.attachedRigidbody;
            if (hitBody != null && hitBody.bodyType != RigidbodyType2D.Static)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void NotifyHit(Collider2D other)
    {
        if (!IsSwinging || other == null)
        {
            return;
        }

        foreach (MonoBehaviour behaviour in other.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IStickHittable hittable)
            {
                hittable.OnStickHit(this, other);
            }
        }

        HitDetected?.Invoke(other);
    }

    private void SpawnGhost()
    {
        if (stickRenderer == null || stickRenderer.sprite == null)
        {
            return;
        }

        GameObject ghost = ghostPool.Count > 0 ? ghostPool.Pop() : CreateGhost();
        ghost.transform.SetPositionAndRotation(
            stickRenderer.transform.position,
            stickRenderer.transform.rotation);
        ghost.transform.localScale = stickRenderer.transform.lossyScale;
        ghost.layer = stickRenderer.gameObject.layer;

        SpriteRenderer ghostRenderer = ghost.GetComponent<SpriteRenderer>();
        ghostRenderer.sprite = stickRenderer.sprite;
        ghostRenderer.sharedMaterial = stickRenderer.sharedMaterial;
        ghostRenderer.flipX = stickRenderer.flipX;
        ghostRenderer.flipY = stickRenderer.flipY;
        ghostRenderer.sortingLayerID = stickRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = stickRenderer.sortingOrder - 1;
        ghostRenderer.color = stickRenderer.color;

        ghost.SetActive(true);
        ghost.GetComponent<StickAfterimage>().Begin(this, ghostLifetime, ghostStartAlpha);
        activeGhosts++;
    }

    private GameObject CreateGhost()
    {
        GameObject ghost = new GameObject("StickGhost");
        ghost.SetActive(false);
        ghost.transform.SetParent(ghostContainer, false);
        ghost.AddComponent<SpriteRenderer>();
        ghost.AddComponent<StickAfterimage>();
        return ghost;
    }

    internal void RecycleGhost(GameObject ghost)
    {
        if (ghost == null)
        {
            return;
        }

        activeGhosts = Mathf.Max(0, activeGhosts - 1);
        ghost.SetActive(false);
        ghostPool.Push(ghost);
    }

    private void OnDestroy()
    {
        if (ghostContainer != null)
        {
            Destroy(ghostContainer.gameObject);
        }
    }
}
