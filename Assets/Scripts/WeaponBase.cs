using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] protected float fireRate = 0.1f;
    [SerializeField] protected float damage = 20f;
    [SerializeField] protected GameObject muzzleFlashObject;
    [SerializeField] protected ParticleSystem[] muzzleFlashSystems;
    [SerializeField] protected ParticleSystem impactEffect;
    [SerializeField] protected GameObject impactMark;
    [SerializeField] protected AudioClip fireSound;
    
    protected AudioSource audioSource;
    protected float nextTimeToFire = 0f;
    protected float muzzleFlashDuration = 0.05f;  // Duration to show muzzle flash
    protected float muzzleFlashEndTime;

    protected virtual void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        if (muzzleFlashObject != null)
        {
            muzzleFlashSystems = muzzleFlashObject.GetComponentsInChildren<ParticleSystem>();
            muzzleFlashObject.SetActive(false);
        }
    }

    protected virtual void Update()
    {
        // Check if muzzle flash should be turned off
        if (muzzleFlashObject != null && muzzleFlashObject.activeSelf && Time.time >= muzzleFlashEndTime)
        {
            muzzleFlashObject.SetActive(false);
        }
    }

    public virtual void HandleUpdate()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            HandleFire();
        }
    }

    protected virtual void HandleFire()
    {
        if (fireSound) audioSource.PlayOneShot(fireSound);
    }

    protected void PlayMuzzleFlash()
    {
        if (muzzleFlashObject != null)
        {
            muzzleFlashObject.SetActive(true);
            muzzleFlashEndTime = Time.time + muzzleFlashDuration;
            
            foreach (var particleSystem in muzzleFlashSystems)
            {
                particleSystem.Stop();
                particleSystem.Play();
            }
        }
    }

    protected void CreateImpactEffects(RaycastHit hit)
    {
        Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
        GameObject mark = Instantiate(impactMark, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(-hit.normal));
        Destroy(mark, 5f);

        if (hit.collider.TryGetComponent<BaseEnemy>(out BaseEnemy enemy))
        {
            enemy.TakeDamage(damage, hit.point);
        }
    }
}