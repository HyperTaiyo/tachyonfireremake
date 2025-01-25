using UnityEngine;

public class HammerHeadCollision : MonoBehaviour
{
    public bool HasCollided { get; private set; }
    public Collider LastHitCollider { get; private set; }
    public Vector3 LastHitPoint { get; private set; }
    
    private void OnCollisionEnter(Collision collision)
    {
        HasCollided = true;
        LastHitCollider = collision.collider;
        LastHitPoint = collision.GetContact(0).point;
    }
}