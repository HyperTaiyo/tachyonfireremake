using UnityEngine;

public class Shotgun : WeaponBase
{
    [Header("Shotgun Specific")]
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private Transform casingSpawnPoint;
    [SerializeField] private Animator shotgunAnimator;
    [SerializeField] private CasingSpawner casingSpawner;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float minDownwardAngle = 45f;
    [SerializeField] private int pelletsPerShot = 8;
    [SerializeField] private float spreadAngle = 5f;
    
    [Header("Boost Settings")]
    [SerializeField] private float boostDecayRate = 5f; // How quickly the boost fades
    [SerializeField] private float maxBoostDuration = 0.5f; // Maximum duration of the boost
    
    private CharacterController characterController;
    private static readonly int FireTrigger = Animator.StringToHash("Fire");
    private Vector3 boostVelocity;
    private float currentBoostTime;
    private bool isBoostActive;
    
    private void Start()
    {
        characterController = GetComponentInParent<CharacterController>();
    }

    protected override void Update()
    {
        HandleUpdate();
        UpdateBoostMovement();
    }

    private void UpdateBoostMovement()
    {
        if (isBoostActive)
        {
            currentBoostTime += Time.deltaTime;
            
            if (currentBoostTime >= maxBoostDuration)
            {
                isBoostActive = false;
                boostVelocity = Vector3.zero;
            }
            else
            {
                // Gradually decrease the boost velocity
                float decayFactor = 1f - (currentBoostTime / maxBoostDuration);
                Vector3 currentBoost = boostVelocity * decayFactor;
                characterController.Move(currentBoost * Time.deltaTime);
            }
        }
    }

    protected override void HandleFire()
    {
        base.HandleFire();
        nextTimeToFire = Time.time + fireRate;
        
        PlayMuzzleFlash();
        shotgunAnimator.SetTrigger(FireTrigger);
        
        // Fire multiple pellets
        for (int i = 0; i < pelletsPerShot; i++)
        {
            Vector3 spreadDirection = CalculateSpreadDirection();
            if (Physics.Raycast(Camera.main.transform.position, spreadDirection, out RaycastHit hit))
            {
                CreateImpactEffects(hit);
            }
        }
        
        casingSpawner.SpawnCasing(true);  // Right casing
        casingSpawner.SpawnCasing(false); // Left casing
        
        // Apply smooth boost if aiming down
        float angle = Vector3.Angle(Vector3.down, Camera.main.transform.forward);
        if (angle < minDownwardAngle)
        {
            InitiateBoost();
        }
    }

    private void InitiateBoost()
    {
        isBoostActive = true;
        currentBoostTime = 0f;
        boostVelocity = -Camera.main.transform.forward * jumpForce;
    }

    private Vector3 CalculateSpreadDirection()
    {
        float randomSpreadX = Random.Range(-spreadAngle, spreadAngle);
        float randomSpreadY = Random.Range(-spreadAngle, spreadAngle);
        
        Vector3 forward = Camera.main.transform.forward;
        Quaternion spreadRotation = Quaternion.Euler(randomSpreadX, randomSpreadY, 0);
        
        return spreadRotation * forward;
    }
}