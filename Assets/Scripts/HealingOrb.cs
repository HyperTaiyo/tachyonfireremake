using UnityEngine;

public class HealingOrb : MonoBehaviour
{
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float homingStrength = 2f;
    
    private Transform target;
    private Vector3 velocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Update()
    {
        if (target == null) return;

        // Calculate direction to player with homing effect
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        velocity = Vector3.Lerp(velocity, directionToTarget * moveSpeed, Time.deltaTime * homingStrength);
        
        // Move orb
        transform.position += velocity * Time.deltaTime;
        
        // Rotate orb
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Check for collection
        if (Vector3.Distance(transform.position, target.position) < 1f)
        {
            target.GetComponent<PlayerHealth>()?.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}