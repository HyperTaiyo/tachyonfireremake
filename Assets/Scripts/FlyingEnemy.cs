using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemy : BaseEnemy
{
    [Header("Flying Settings")]
    [SerializeField] private float flyHeight = 5f;
    [SerializeField] private float verticalSpeed = 2f;
    [SerializeField] private float horizontalSpeed = 8f;
    [SerializeField] private float hoverAmplitude = 0.5f;
    [SerializeField] private float hoverFrequency = 2f;

    private Vector3 targetPosition;
    private float hoverOffset;

    protected override void Start()
    {
        base.Start();
        targetPosition = transform.position + Vector3.up * flyHeight;
    }

    protected override void Update()
    {
        base.Update();
        
        if (player == null) return;

        // Hover effect
        hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        
        // Update target position
        if (isChasing)
        {
            targetPosition = player.position + Vector3.up * flyHeight;
        }

        // Move towards target
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        transform.position = Vector3.Lerp(transform.position, 
            targetPosition + Vector3.up * hoverOffset, 
            horizontalSpeed * Time.deltaTime);

        // Look at player
        transform.LookAt(player);
    }
}

// Projectile class
public class Projectile : MonoBehaviour
{
    private float damage;
    private float lifeTime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}