using UnityEngine;

public interface IStickHittable
{
    void OnStickHit(StickController stick, Collider2D hitCollider);
}
