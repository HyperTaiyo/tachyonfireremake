using UnityEngine;

public class CasingSpawner : MonoBehaviour
{
    [Header("Casing Settings")]
    [SerializeField] private GameObject casingPrefab;
    [SerializeField] private float ejectionForce = 5f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float rotationForce = 10f;
    [SerializeField] private float destructionTime = 2f;
    [SerializeField] private bool isMinigun = false;
    [SerializeField] private float spawnOffset = 0.5f; // Distance from camera to spawn point
    [SerializeField] private float sideOffset = 0.2f;  // Horizontal offset from center

    private Transform mainCamera;
    private Transform weaponTransform;

    private void Start()
    {
        mainCamera = Camera.main.transform;
        weaponTransform = transform;
    }

    public void SpawnCasing(bool rightSide = true)
    {
        // Calculate spawn position based on weapon transform with forward offset
        Vector3 spawnPosition = mainCamera.position + mainCamera.forward * spawnOffset;
        
        // Add appropriate offsets based on weapon type
        if (isMinigun)
        {
            spawnPosition += weaponTransform.right * -sideOffset; // Left side for minigun
            spawnPosition += weaponTransform.up * sideOffset; // Slightly up
        }
        else
        {
            // For shotgun, spawn on either left or right side
            spawnPosition += weaponTransform.right * (rightSide ? sideOffset : -sideOffset);
        }

        // Spawn the casing with weapon's rotation
        GameObject casing = Instantiate(casingPrefab, spawnPosition, weaponTransform.rotation);
        Rigidbody casingRb = casing.GetComponent<Rigidbody>();

        if (casingRb != null)
        {
            Vector3 ejectionDirection;
            if (isMinigun)
            {
                // Minigun casings eject up and to the left
                ejectionDirection = (-weaponTransform.right + weaponTransform.up).normalized;
                casingRb.AddForce(ejectionDirection * ejectionForce + weaponTransform.up * upwardForce, ForceMode.Impulse);
            }
            else
            {
                // Shotgun casings eject sideways with upward arc
                Vector3 sideDirection = rightSide ? weaponTransform.right : -weaponTransform.right;
                ejectionDirection = (sideDirection + weaponTransform.up * 0.5f).normalized;
                casingRb.AddForce(ejectionDirection * ejectionForce + weaponTransform.up * upwardForce, ForceMode.Impulse);
            }

            // Add randomized rotation
            Vector3 randomRotation = new Vector3(
                Random.Range(-rotationForce, rotationForce),
                Random.Range(-rotationForce, rotationForce),
                Random.Range(-rotationForce, rotationForce)
            );
            casingRb.AddTorque(randomRotation, ForceMode.Impulse);

            // Add initial angular velocity for more natural spinning
            casingRb.angularVelocity = Random.insideUnitSphere * rotationForce;
        }

        Destroy(casing, destructionTime);
    }
}