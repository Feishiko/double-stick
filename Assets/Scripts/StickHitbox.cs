using UnityEngine;

public class StickHitbox : MonoBehaviour
{
    private StickController controller;

    private void Awake()
    {
        controller = GetComponentInParent<StickController>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (controller != null)
        {
            controller.NotifyHit(other);
        }
    }
}
