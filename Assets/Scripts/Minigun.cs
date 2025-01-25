using UnityEngine;

public class Minigun : WeaponBase
{
    [Header("Minigun Specific")]
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private Transform casingSpawnPoint;
    [SerializeField] private Animator minigunAnimator;
    [SerializeField] private float spinUpDuration = 0.5f;
    [SerializeField] private CasingSpawner casingSpawner;
    
    private bool isFiring;
    private float currentSpinSpeed;
    private static readonly int SpinSpeedParam = Animator.StringToHash("SpinSpeed");
    private static readonly int IsFiringParam = Animator.StringToHash("IsFiring");
    
    protected override void Update()
    {
        HandleUpdate();
        
        // Handle spin up/down animation
        if (Input.GetButtonDown("Fire1"))
        {
            isFiring = true;
            minigunAnimator.SetBool(IsFiringParam, true);
        }
        else if (Input.GetButtonUp("Fire1"))
        {
            isFiring = false;
            minigunAnimator.SetBool(IsFiringParam, false);
        }
        
        // Update spin speed
        currentSpinSpeed = Mathf.MoveTowards(
            currentSpinSpeed,
            isFiring ? 1 : 0,
            Time.deltaTime / spinUpDuration
        );
        
        minigunAnimator.SetFloat(SpinSpeedParam, currentSpinSpeed);
    }

    protected override void HandleFire()
    {
        // Only fire if spun up enough
        if (currentSpinSpeed < 0.9f) return;
        
        base.HandleFire();
        nextTimeToFire = Time.time + fireRate;
        
        PlayMuzzleFlash();
        
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit))
        {
            CreateImpactEffects(hit);
        }
        
        casingSpawner.SpawnCasing();
    }
} 