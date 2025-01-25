using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float normalgravity = -9.81f;
    private float gravity;
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideTime = 1f;
    [SerializeField] private float slopeLimitAngle = 45f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask;

    [Header("Slide Settings")]
    [SerializeField] private float slideRotationSpeed = 2f;
    [SerializeField] private float slideDirectionChangeSpeed = 3f;

    [Header("Double Jump Settings")]
    [SerializeField] private bool hasDoubleJump = true;
    [SerializeField] private float doubleJumpForce = 6f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float footstepInterval = 0.5f;

    // Component references
    private CharacterController controller;
    private Camera playerCamera;
    private Transform cameraTransform;
    private AudioSource audioSource;
    private PlayerHealth playerHealth;

    // Movement variables
    private Vector3 moveDirection;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private bool canDoubleJump;
    private bool hasDoubleJumped;
    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;
    private Quaternion targetSlideRotation;
    private Vector3 currentSlideDirection;
    private float nextFootstepTime;

    // Ground check variables
    private bool wasGrounded;
    private readonly Collider[] groundCheckColliders = new Collider[4];

    // Camera variables
    private float verticalRotation;
    private float smoothRotationVelocityX;
    private float smoothRotationVelocityY;
    private float targetVerticalRotation;

    private void Start()
    {
        gravity = normalgravity;

        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        cameraTransform = playerCamera.transform;
        audioSource = gameObject.AddComponent<AudioSource>();
        playerHealth = GetComponent<PlayerHealth>();
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        currentSpeed = walkSpeed;
    }

    private void Update()
    {
        CheckGrounded();
        HandleMovementInput();
        HandleMouseLook();
        HandleJump();
        HandleSprint();
        HandleSlide();
        ApplyGravity();
        HandleSlopeMovement();
        
        // Reset double jump when landing
        if (isGrounded && !wasGrounded)
        {
            canDoubleJump = hasDoubleJump;
            hasDoubleJumped = false;
            if (landSound) audioSource.PlayOneShot(landSound);
        }
        
        wasGrounded = isGrounded;
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = transform.position + Vector3.up * (controller.radius);
        int numColliders = Physics.OverlapSphereNonAlloc(spherePosition, groundCheckRadius, groundCheckColliders, groundMask);
        
        isGrounded = numColliders > 0 || controller.isGrounded;
        
        if (!isGrounded)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, controller.height / 2 + 0.1f, groundMask))
            {
                isGrounded = true;
            }
        }
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        
        if (!isSliding)
        {
            gravity = normalgravity;

            moveDirection = move.normalized * currentSpeed;
            
            // Play footstep sounds when moving on ground
            if (isGrounded && moveDirection.magnitude > 0.1f && Time.time >= nextFootstepTime)
            {
                PlayFootstepSound();
            }
        }
        else
        {
            Vector3 targetDirection = move.normalized;
            if (targetDirection.magnitude > 0.1f)
            {
                currentSlideDirection = Vector3.Lerp(currentSlideDirection, targetDirection, Time.deltaTime * slideDirectionChangeSpeed);
                moveDirection = currentSlideDirection * slideSpeed;
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        
        AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
        audioSource.PlayOneShot(randomFootstep);
        nextFootstepTime = Time.time + footstepInterval;
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (isSliding)
        {
            gravity = normalgravity * 2;
            mouseX *= slideRotationSpeed;
        }

        targetVerticalRotation -= mouseY;
        targetVerticalRotation = Mathf.Clamp(targetVerticalRotation, -90f, 90f);
        verticalRotation = Mathf.SmoothDampAngle(verticalRotation, targetVerticalRotation, ref smoothRotationVelocityX, smoothTime);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleJump()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                canDoubleJump = hasDoubleJump;
                if (jumpSound) audioSource.PlayOneShot(jumpSound);
            }
            else if (canDoubleJump && !hasDoubleJumped)
            {
                velocity.y = Mathf.Sqrt(doubleJumpForce * -2f * gravity);
                canDoubleJump = false;
                hasDoubleJumped = true;
                if (jumpSound) audioSource.PlayOneShot(jumpSound);
            }
        }
    }

    private void HandleSprint()
    {
        if (Input.GetKey(KeyCode.LeftShift) && !isSliding)
        {
            currentSpeed = sprintSpeed;
        }
        else if (!isSliding)
        {
            currentSpeed = walkSpeed;
        }
    }

    private void HandleSlide()
    {
        if (Input.GetKeyDown(KeyCode.C) && !isSliding && controller.velocity.magnitude > 0)
        {
            isSliding = true;
            slideTimer = slideTime;
            currentSlideDirection = moveDirection.normalized;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0)
            {
                isSliding = false;
            }
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            isSliding = false;
        }
    }

    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleSlopeMovement()
    {
        if (isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, controller.height / 2 + 0.1f))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                
                if (slopeAngle <= slopeLimitAngle)
                {
                    Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, hit.normal);
                    moveDirection = slopeDirection;
                }
            }
        }
        
        controller.Move(moveDirection * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * (controller.radius), groundCheckRadius);
    }
}