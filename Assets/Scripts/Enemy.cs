using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class BaseEnemy : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float damage = 20f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float attackRate = 1f;
    [SerializeField] protected float detectionRange = 20f;
    [SerializeField] protected float chaseSpeed = 5f;
    
    [Header("Effects")]
    [SerializeField] protected GameObject[] bloodEffectPrefabs;
    [SerializeField] protected GameObject[] deathEffectPrefabs;
    [SerializeField] protected float effectDuration = 2f;
    [SerializeField] protected GameObject healingOrb;
    [SerializeField] protected AudioClip hurtSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected AudioClip attackSound;

    [Header("Effect Variation Settings")]
    [SerializeField] protected Vector2 scaleVariation = new Vector2(0.8f, 1.2f);
    [SerializeField] protected Vector2 rotationVariation = new Vector2(-30f, 30f);

    protected float currentHealth;
    [SerializeField] protected Transform player;
    protected float nextAttackTime;
    protected AudioSource audioSource;
    protected bool isChasing = false;
    protected Vector3 lastKnownPlayerPosition;
    
    [Header("Chase Settings")]
    [SerializeField] private float minDistanceToPlayer = 1.5f;
    [SerializeField] private float pathUpdateRate = 0.2f;
    [SerializeField] private LayerMask obstacleLayer;
    private NavMeshAgent agent;
    private float nextPathUpdateTime;
    
    protected virtual void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        audioSource = gameObject.AddComponent<AudioSource>();
        ValidateEffectArrays();
    }

    private void ValidateEffectArrays()
    {
        // Ensure arrays are initialized
        if (bloodEffectPrefabs == null) bloodEffectPrefabs = new GameObject[0];
        if (deathEffectPrefabs == null) deathEffectPrefabs = new GameObject[0];
        
        // Log warnings if arrays are empty
        if (bloodEffectPrefabs.Length == 0)
            Debug.LogWarning($"No blood effects assigned to {gameObject.name}", this);
        if (deathEffectPrefabs.Length == 0)
            Debug.LogWarning($"No death effects assigned to {gameObject.name}", this);
    }

    protected virtual void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            if (HasLineOfSightToPlayer())
            {
                isChasing = true;
                lastKnownPlayerPosition = player.position;
            }
        }
        
        if (isChasing)
        {
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                Attack();
            }
            
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                isChasing = false;
            }
        }
        
        UpdateAnimationState(distanceToPlayer);
    }

    private void UpdatePathToPlayer()
    {
        if (!isChasing || player == null) return;
        
        // Update the destination only if we're actively chasing
        if (agent.isActiveAndEnabled)
        {
            agent.SetDestination(lastKnownPlayerPosition);
        }
    }

    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if there are any obstacles between enemy and player
        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, distanceToPlayer, obstacleLayer))
        {
            return false;
        }
        
        return true;
    }

    private void Attack()
    {
        nextAttackTime = Time.time + attackRate;
        
        // Get the exact direction to the player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        
        // Create a small forward thrust during attack
        if (agent.isActiveAndEnabled)
        {
            agent.velocity = directionToPlayer * agent.speed * 0.5f;
        }
        
        // Apply damage to player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            if (attackSound) audioSource.PlayOneShot(attackSound);
        }
    }

    private GameObject GetRandomEffect(GameObject[] effects)
    {
        if (effects == null || effects.Length == 0) return null;
        return effects[Random.Range(0, effects.Length)];
    }

    private void SpawnEffect(GameObject[] effectPrefabs, Vector3 position, Quaternion baseRotation)
    {
        GameObject selectedPrefab = GetRandomEffect(effectPrefabs);
        if (selectedPrefab == null) return;

        // Apply random variations
        float scale = Random.Range(scaleVariation.x, scaleVariation.y);
        float rotationOffset = Random.Range(rotationVariation.x, rotationVariation.y);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(0, rotationOffset, 0);

        // Spawn the effect
        GameObject effect = Instantiate(selectedPrefab, position, finalRotation);
        effect.transform.localScale *= scale;

        // Randomly flip the effect sometimes
        if (Random.value > 0.5f)
        {
            effect.transform.localScale = new Vector3(
                -effect.transform.localScale.x,
                effect.transform.localScale.y,
                effect.transform.localScale.z
            );
        }

        Destroy(effect, effectDuration);
    }

    public virtual void TakeDamage(float damage, Vector3 hitPoint)
    {
        Debug.Log("Took damage");
        currentHealth -= damage;
        
        if (bloodEffectPrefabs.Length > 0)
        {
            SpawnEffect(bloodEffectPrefabs, hitPoint, Quaternion.LookRotation(-transform.forward));
        }
        
        if (hurtSound) audioSource.PlayOneShot(hurtSound);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (deathEffectPrefabs.Length > 0)
        {
            int numEffects = Random.Range(1, 4);
            for (int i = 0; i < numEffects; i++)
            {
                Vector3 offsetPos = transform.position + Random.insideUnitSphere * 0.5f;
                SpawnEffect(deathEffectPrefabs, offsetPos, Quaternion.identity);
            }
        }
        
        if (deathSound) audioSource.PlayOneShot(deathSound);
        SpawnHealingOrb(transform.position + Vector3.up);
        Destroy(gameObject, 0.5f);
    }

    protected void SpawnHealingOrb(Vector3 spawnPosition)
    {
        if (healingOrb == null) return;

        GameObject orb = Instantiate(healingOrb, spawnPosition, Quaternion.identity);
        HealingOrb healingOrbComponent = orb.GetComponent<HealingOrb>();
        if (healingOrbComponent != null)
        {
            healingOrbComponent.SetTarget(player);
        }
    }

    private void UpdateAnimationState(float distanceToPlayer)
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsChasing", isChasing);
            animator.SetBool("IsAttacking", distanceToPlayer <= attackRange);
            animator.SetFloat("DistanceToPlayer", distanceToPlayer);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}