using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour, IStickHittable
{
    public event System.Action<EnemyController> Died;

    public bool IsDead => isDead;
    public bool IsFrozen => frozen;

    [Header("Detection")]
    [SerializeField] private CircleCollider2D detectionCollider;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private float staggerDuration = 0.15f;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private int hitStopFrames = 2;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    private readonly Collider2D[] detectionHits = new Collider2D[32];

    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private Animator animator;
    private Transform spriteTransform;
    private SpriteRenderer spriteRenderer;
    private PlayerMovement2D player;
    private bool isDead;
    private bool frozen;
    private bool isAnimationMoving;
    private bool animationInitialized;
    private int health;
    private float staggerTimer;
    private float flashTimer;
    private SpriteRenderer flashRenderer;
    private Material flashMaterial;
    private AudioSource audioSource;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        spriteTransform = transform.Find("Sprite");
        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }

        health = maxHealth;
        CreateFlashOverlay();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (detectionCollider == null)
        {
            detectionCollider = GetComponent<CircleCollider2D>();
        }

        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<CircleCollider2D>();
            detectionCollider.radius = 6f;
        }

        detectionCollider.isTrigger = true;
        detectionCollider.offset = Vector2.zero;

        player = FindObjectOfType<PlayerMovement2D>();
    }

    private void Start()
    {
        UpdateAnimation(false);
    }

    private void Update()
    {
        if (flashTimer <= 0f)
        {
            if (flashRenderer != null && flashRenderer.gameObject.activeSelf)
            {
                flashRenderer.gameObject.SetActive(false);
            }

            return;
        }

        flashTimer -= Time.deltaTime;

        if (flashRenderer == null || spriteRenderer == null)
        {
            return;
        }

        flashRenderer.sprite = spriteRenderer.sprite;
        flashRenderer.flipX = spriteRenderer.flipX;
        flashRenderer.flipY = spriteRenderer.flipY;
        flashRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        flashRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Color color = flashRenderer.color;
        color.a = Mathf.Clamp01(flashTimer / Mathf.Max(flashDuration, 0.01f));
        flashRenderer.color = color;
    }

    private void FixedUpdate()
    {
        if (frozen)
        {
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }

            UpdateAnimation(false);
            return;
        }

        if (isDead || player == null)
        {
            UpdateAnimation(false);
            return;
        }

        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.fixedDeltaTime;
            body.velocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        if (!IsPlayerInDetectionRange())
        {
            body.velocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        if (moveSpeed <= 0.0001f)
        {
            body.velocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        Vector2 toPlayer = (Vector2)player.transform.position - body.position;
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            body.velocity = Vector2.zero;
            UpdateAnimation(false);
            return;
        }

        body.velocity = toPlayer.normalized * moveSpeed;
        UpdateAnimation(true);
        UpdateFacing();
    }

    public bool IsPlayerInDetectionRange()
    {
        if (detectionCollider == null || player == null)
        {
            return false;
        }

        Vector2 center = detectionCollider.bounds.center;
        int count = Physics2D.OverlapCircleNonAlloc(center, detectionCollider.radius, detectionHits);
        for (int i = 0; i < count; i++)
        {
            if (detectionHits[i].GetComponentInParent<PlayerMovement2D>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateFacing()
    {
        if (spriteRenderer != null && Mathf.Abs(body.velocity.x) > 0.01f)
        {
            spriteRenderer.flipX = body.velocity.x < 0f;
        }
    }

    private void UpdateAnimation(bool moving)
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.enabled)
        {
            return;
        }

        if (animationInitialized && moving == isAnimationMoving)
        {
            return;
        }

        animationInitialized = true;
        isAnimationMoving = moving;

        if (moving)
        {
            animator.speed = 1f;
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != 0)
        {
            animator.Play(state.fullPathHash, 0, 0f);
        }

        animator.speed = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead)
        {
            return;
        }

        if (collision.collider.GetComponentInParent<PlayerMovement2D>() != null)
        {
            LevelManager.RestartCurrentLevel();
        }
    }

    public void OnStickHit(StickController stick, Collider2D hitCollider)
    {
        if (isDead || staggerTimer > 0f)
        {
            return;
        }

        if (hitCollider == null || hitCollider.isTrigger)
        {
            return;
        }

        int damage = stick != null ? Mathf.Max(1, stick.Damage) : 10;
        health -= damage;
        PlaySound(hitSound);
        StartHitReaction();
        HitStop.Request(hitStopFrames);

        if (health <= 0)
        {
            Die();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    public void ConfigureDetection(CircleCollider2D detection)
    {
        detectionCollider = detection;
    }

    public void SetFrozen(bool value)
    {
        if (isDead)
        {
            return;
        }

        frozen = value;
        if (!frozen)
        {
            return;
        }

        if (body != null)
        {
            body.velocity = Vector2.zero;
        }

        UpdateAnimation(false);
    }

    private void StartHitReaction()
    {
        staggerTimer = staggerDuration;
        flashTimer = flashDuration;
        body.velocity = Vector2.zero;
        UpdateAnimation(false);

        if (flashRenderer == null || spriteRenderer == null)
        {
            return;
        }

        flashRenderer.gameObject.SetActive(true);
        flashRenderer.sprite = spriteRenderer.sprite;
        flashRenderer.flipX = spriteRenderer.flipX;
        flashRenderer.flipY = spriteRenderer.flipY;
        flashRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        flashRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        Color color = flashRenderer.color;
        color.a = 1f;
        flashRenderer.color = color;
    }

    private void CreateFlashOverlay()
    {
        if (spriteRenderer == null || spriteTransform == null)
        {
            return;
        }

        Shader shader = Resources.Load<Shader>("SpriteWhiteFlash");
        if (shader == null)
        {
            return;
        }

        GameObject overlay = new GameObject("HitFlash");
        overlay.transform.SetParent(spriteTransform, false);

        flashRenderer = overlay.AddComponent<SpriteRenderer>();
        flashMaterial = new Material(shader);
        flashMaterial.SetColor("_FlashColor", Color.white);

        flashRenderer.sharedMaterial = flashMaterial;
        flashRenderer.sprite = spriteRenderer.sprite;
        flashRenderer.color = new Color(1f, 1f, 1f, 0f);
        flashRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        flashRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        overlay.SetActive(false);
    }

    private void Die()
    {
        isDead = true;
        PlaySound(deathSound);
        body.velocity = Vector2.zero;
        body.simulated = false;

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (spriteTransform != null)
        {
            spriteTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        Died?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (flashMaterial != null)
        {
            Destroy(flashMaterial);
        }
    }
}
