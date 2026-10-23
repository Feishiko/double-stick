using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private Animator animator;
    [SerializeField] private StickController stickController;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private bool isMoving;
    private bool animationInitialized;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (stickController == null)
        {
            stickController = GetComponent<StickController>();
        }
    }

    private void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")).normalized;

        UpdateAnimation(moveInput.sqrMagnitude > 0.0001f);
    }

    public void SetFrozen(bool frozen)
    {
        if (frozen)
        {
            moveInput = Vector2.zero;
            currentVelocity = Vector2.zero;

            if (body != null)
            {
                body.velocity = Vector2.zero;
            }

            PauseAnimation();
        }
        else
        {
            animationInitialized = false;
        }

        enabled = !frozen;
    }

    private void PauseAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != 0)
        {
            animator.Play(state.fullPathHash, 0, 0f);
        }

        animator.speed = 0f;
        animationInitialized = true;
        isMoving = false;
    }

    private void UpdateAnimation(bool moving)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (animationInitialized && moving == isMoving)
        {
            return;
        }

        animationInitialized = true;
        isMoving = moving;

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

    private void FixedUpdate()
    {
        Vector2 targetVelocity = moveInput * moveSpeed;
        Vector2 nextVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime);

        if (stickController != null)
        {
            nextVelocity = LimitByStickWall(nextVelocity);
        }

        currentVelocity = nextVelocity;
        body.velocity = currentVelocity;
    }

    private Vector2 LimitByStickWall(Vector2 nextVelocity)
    {
        Vector2 step = nextVelocity * Time.fixedDeltaTime;
        if (step.sqrMagnitude < 0.0000001f)
        {
            return nextVelocity;
        }

        Vector2 start = body.position;
        if (stickController.IsStuckAgainstWall())
        {
            if (Vector2.Dot(nextVelocity, stickController.TipDirection) < -0.01f)
            {
                return nextVelocity;
            }

            return Vector2.zero;
        }

        if (!stickController.WouldHitWallAt(start + step))
        {
            return nextVelocity;
        }

        Vector2 allowed = Vector2.zero;

        if (Mathf.Abs(step.x) > 0.000001f
            && !stickController.WouldHitWallAt(start + new Vector2(step.x, 0f)))
        {
            allowed.x = step.x;
        }

        if (Mathf.Abs(step.y) > 0.000001f
            && !stickController.WouldHitWallAt(start + new Vector2(allowed.x, step.y)))
        {
            allowed.y = step.y;
        }

        return allowed / Time.fixedDeltaTime;
    }
}
