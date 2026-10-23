using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float vanishDuration = 0.5f;

    private Rigidbody2D body;
    private Collider2D hitCollider;
    private Vector3 originalScale;
    private Coroutine lifetimeRoutine;
    private bool vanishing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (lifetime > 0f)
        {
            lifetimeRoutine = StartCoroutine(LifetimeRoutine());
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();
        body.velocity = direction * speed;
        body.rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (vanishing || other == null || other.isTrigger)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerMovement2D>() != null)
        {
            LevelManager.RestartCurrentLevel();
            Destroy(gameObject);
            return;
        }

        if (other.GetComponentInParent<EnemyController>() != null)
        {
            return;
        }

        StartVanish();
    }

    private void StartVanish()
    {
        if (vanishing)
        {
            return;
        }

        vanishing = true;

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        if (hitCollider != null)
        {
            hitCollider.enabled = false;
        }

        body.velocity = Vector2.zero;
        body.simulated = false;

        StartCoroutine(VanishRoutine());
    }

    private IEnumerator VanishRoutine()
    {
        float elapsed = 0f;
        while (elapsed < vanishDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / vanishDuration);
            transform.localScale = originalScale * (1f - t * t);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        Destroy(gameObject);
    }
}
