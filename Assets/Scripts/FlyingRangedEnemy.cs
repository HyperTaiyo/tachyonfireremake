using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingRangedEnemy : FlyingEnemy
{
    [Header("Armor Settings")]
    [SerializeField] private float armorHealth = 50f;
    [SerializeField] private AudioClip armorBreakSound;
    [SerializeField] private GameObject armorBreakEffect;
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileDamage = 10f;
    
    private bool hasArmor = true;

    public override void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (hasArmor)
        {
            armorHealth -= damage;
            if (armorHealth <= 0)
            {
                BreakArmor();
                return;
            }
        }
        else
        {
            base.TakeDamage(damage, hitPoint);
        }
    }

    private void BreakArmor()
    {
        hasArmor = false;
        if (armorBreakSound) audioSource.PlayOneShot(armorBreakSound);
        if (armorBreakEffect)
        {
            Instantiate(armorBreakEffect, transform.position, Quaternion.identity);
        }
    }

    protected override void Update()
    {
        base.Update();
        
        if (Time.time >= nextAttackTime && isChasing)
        {
            ShootProjectile();
        }
    }

    private void ShootProjectile()
    {
        nextAttackTime = Time.time + attackRate;
        
        if (projectilePrefab != null && player != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, 
                transform.position, 
                Quaternion.LookRotation((player.position - transform.position).normalized));
            
            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            if (projectileRb != null)
            {
                projectileRb.velocity = projectile.transform.forward * projectileSpeed;
            }
            
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                projectileComponent.SetDamage(projectileDamage);
            }
        }
    }
}