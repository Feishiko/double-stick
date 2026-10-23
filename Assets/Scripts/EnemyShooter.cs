using System.Collections;
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireInterval = 1.5f;
    [SerializeField] private float fireRange = 5f;

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject white;
    [SerializeField] private int whiteVisibleFrames = 4;
    [SerializeField] private Transform spriteTransform;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;

    private EnemyController enemy;
    private PlayerMovement2D player;
    private AudioSource audioSource;
    private SpriteRenderer enemySprite;
    private SpriteRenderer whiteRenderer;
    private int bulletSortingOrder = 10002;
    private float fireTimer;
    private float muzzleLocalX;
    private Coroutine whiteRoutine;

    private void Awake()
    {
        enemy = GetComponent<EnemyController>();
        player = FindObjectOfType<PlayerMovement2D>();

        if (spriteTransform == null)
        {
            spriteTransform = transform.Find("Sprite");
        }

        if (spriteTransform != null)
        {
            enemySprite = spriteTransform.GetComponent<SpriteRenderer>();
        }

        if (firePoint != null)
        {
            muzzleLocalX = Mathf.Abs(firePoint.localPosition.x);
        }

        if (white != null)
        {
            whiteRenderer = white.GetComponent<SpriteRenderer>();
        }

        if (bulletPrefab != null)
        {
            SpriteRenderer bulletSprite = bulletPrefab.GetComponentInChildren<SpriteRenderer>(true);
            if (bulletSprite != null)
            {
                bulletSortingOrder = bulletSprite.sortingOrder;
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (white != null)
        {
            white.SetActive(false);
        }

        fireTimer = fireInterval;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (enemy != null && (enemy.IsDead || enemy.IsFrozen))
        {
            return;
        }

        bool inRange = IsPlayerInRange();
        if (inRange)
        {
            UpdateFacing();
        }

        if (!inRange)
        {
            fireTimer = fireInterval;
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f)
        {
            return;
        }

        Fire();
        fireTimer = fireInterval;
    }

    private void UpdateFacing()
    {
        if (player == null)
        {
            return;
        }

        float sign = player.transform.position.x < transform.position.x ? -1f : 1f;

        if (spriteTransform != null)
        {
            Vector3 scale = spriteTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * sign;
            spriteTransform.localScale = scale;
        }

        if (firePoint != null)
        {
            Vector3 localPosition = firePoint.localPosition;
            localPosition.x = muzzleLocalX * sign;
            firePoint.localPosition = localPosition;
        }
    }

    private bool IsPlayerInRange()
    {
        if (enemy != null)
        {
            return enemy.IsPlayerInDetectionRange();
        }

        return Vector2.Distance(transform.position, player.transform.position) <= fireRange;
    }

    private void Fire()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        ShowWhite();

        if (bulletPrefab == null)
        {
            Debug.LogWarning("EnemyShooter: no bullet prefab assigned.");
            return;
        }

        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;
        Vector2 direction = (Vector2)player.transform.position - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        GameObject bulletObject = Instantiate(bulletPrefab, origin, Quaternion.identity);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(direction);
        }
    }

    private void ShowWhite()
    {
        if (white == null)
        {
            return;
        }

        if (whiteRenderer != null && enemySprite != null)
        {
            whiteRenderer.sortingLayerID = enemySprite.sortingLayerID;
            whiteRenderer.sortingOrder = Mathf.Max(enemySprite.sortingOrder, bulletSortingOrder) + 1;
        }

        if (whiteRoutine != null)
        {
            StopCoroutine(whiteRoutine);
        }

        whiteRoutine = StartCoroutine(WhiteFlashRoutine());
    }

    private IEnumerator WhiteFlashRoutine()
    {
        white.SetActive(true);

        for (int i = 0; i < Mathf.Max(1, whiteVisibleFrames); i++)
        {
            yield return null;
        }

        white.SetActive(false);
        whiteRoutine = null;
    }

    private void OnDisable()
    {
        if (whiteRoutine != null)
        {
            StopCoroutine(whiteRoutine);
            whiteRoutine = null;
        }

        if (white != null)
        {
            white.SetActive(false);
        }
    }
}
