using UnityEngine;
using System.Collections;

public class DetachableHammer : WeaponBase
{
    [Header("Hammer Specific")]
    [SerializeField] private GameObject hammerHeadPrefab;
    [SerializeField] private GameObject completeHammerModel;
    [SerializeField] private float hammerSpeed = 20f;
    [SerializeField] private float returnSpeed = 15f;
    [SerializeField] private ParticleSystem throwFlashEffect;
    [SerializeField] private ParticleSystem returnFlashEffect;
    
    private GameObject activeHammerHead;
    private bool isReturning;
    private bool isThrown;

    protected override void Update()
    {
        base.Update();
        HandleUpdate();
    }

    protected override void HandleFire()
    {
        if (isThrown || activeHammerHead != null || isReturning) return;
        
        base.HandleFire();
        nextTimeToFire = Time.time + fireRate;
        
        ThrowHammer();
    }

    private void ThrowHammer()
    {
        isThrown = true;
        completeHammerModel.SetActive(false);
        
        // Play throw muzzle flash
        if (throwFlashEffect != null)
        {
            throwFlashEffect.Play();
        }
        
        activeHammerHead = Instantiate(hammerHeadPrefab, transform.position, transform.rotation);
        Rigidbody hammerRb = activeHammerHead.GetComponent<Rigidbody>();
        hammerRb.velocity = Camera.main.transform.forward * hammerSpeed;
        
        StartCoroutine(CheckHammerCollision());
    }
    
    private IEnumerator CheckHammerCollision()
    {
        HammerHeadCollision hammerCollision = activeHammerHead.GetComponent<HammerHeadCollision>();
        
        while (activeHammerHead != null && !isReturning)
        {
            if (hammerCollision.HasCollided)
            {
                // Check for enemy hit
                if (hammerCollision.LastHitCollider != null)
                {
                    if (hammerCollision.LastHitCollider.TryGetComponent<BaseEnemy>(out BaseEnemy enemy))
                    {
                        enemy.TakeDamage(damage, hammerCollision.LastHitPoint);
                    }
                }
                StartReturnSequence();
            }
            yield return null;
        }
    }
    
    public void StartReturnSequence()
    {
        isReturning = true;
        StartCoroutine(ReturnHammer());
    }
    
    private IEnumerator ReturnHammer()
    {
        while (activeHammerHead != null)
        {
            Vector3 direction = (transform.position - activeHammerHead.transform.position).normalized;
            activeHammerHead.GetComponent<Rigidbody>().velocity = direction * returnSpeed;
            
            if (Vector3.Distance(transform.position, activeHammerHead.transform.position) < 0.5f)
            {
                // Play return muzzle flash
                if (returnFlashEffect != null)
                {
                    returnFlashEffect.Play();
                }
                
                Destroy(activeHammerHead);
                completeHammerModel.SetActive(true);
                isReturning = false;
                isThrown = false;
                break;
            }
            
            yield return null;
        }
    }

    private void OnDisable()
    {
        if (activeHammerHead != null)
        {
            Destroy(activeHammerHead);
            completeHammerModel.SetActive(true);
            isReturning = false;
            isThrown = false;
        }
    }
}