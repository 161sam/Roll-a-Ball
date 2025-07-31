using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private PlayerStats playerStats;
    [Header("Ball Bewegung")]
    [SerializeField] private float moveForce = 10f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float airControlMultiplier = 0.5f;
    [SerializeField] private float ballDrag = 0.98f; // Ball rolling resistance

    [Header("Input Einstellungen")]

    [Header("Slide")]
    [SerializeField] private float slideImpulseMultiplier = 0.8f;
    [SerializeField] private float slideDuration = 0.5f;

    [Header("Springen")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckRadius = 0.6f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private float coyoteTime = 0.2f;

    [Header("Doppelsprung")]
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpForce = 6f;

    [Header("Fliegen")]
    [SerializeField] private float flyForce = 12f;
    [SerializeField] private float maxFlyHeight = 25f;
    [SerializeField] private float flyHorizontalDamping = 0.9f;

    [Header("Flugenergie")]
    [SerializeField] private float maxFlyEnergy = 3f;
    [SerializeField] private float flyDepletionRate = 1f;
    [SerializeField] private float flyRegenRate = 1.5f;
    [SerializeField] private float flyRegenDelay = 1f;

    [Header("Kamera Referenz")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool findCameraAutomatically = true;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera followCamera;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip landSound;

    [Header("Effects")]
    [SerializeField] private ParticleSystem jumpEffect;
    [SerializeField] private ParticleSystem slideEffect;
    [SerializeField] private TrailRenderer flyTrail;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Private fields
    private Rigidbody rb;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasDoubleJumped;
    private float coyoteTimeCounter;
    private float flyRegenTimer;
    private float flyEnergy;
    private Coroutine slideCoroutine;
    private int groundLayerMaskValue;
    
    // Input states from InputManager (new Input System)
    private Vector2 movementInput;
    private bool jumpPressed;
    private bool flyPressed;
    private bool sprintPressed;
    private bool slidePressed;

    // Public properties
    public float FlyEnergy => flyEnergy;
    public float MaxFlyEnergy => maxFlyEnergy;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting => sprintPressed && isGrounded;
    public bool IsSliding { get; private set; }
    public bool IsFlying { get; private set; }
    public Vector3 Velocity => rb ? rb.linearVelocity : Vector3.zero;

    // Events
    public System.Action<float, float> OnFlyEnergyChanged;
    public System.Action<bool> OnGroundedChanged;
    public System.Action<bool> OnFlyingChanged;

    void Awake()
    {
        if (playerStats != null)
        {
            moveForce = playerStats.moveForce;
            maxSpeed = playerStats.maxSpeed;
            sprintMultiplier = playerStats.sprintMultiplier;
            airControlMultiplier = playerStats.airControlMultiplier;
            ballDrag = playerStats.ballDrag;

            slideImpulseMultiplier = playerStats.slideImpulseMultiplier;
            slideDuration = playerStats.slideDuration;

            jumpForce = playerStats.jumpForce;
            groundCheckRadius = playerStats.groundCheckRadius;
            groundCheckDistance = playerStats.groundCheckDistance;
            groundLayer = playerStats.groundLayer;
            coyoteTime = playerStats.coyoteTime;

            enableDoubleJump = playerStats.enableDoubleJump;
            doubleJumpForce = playerStats.doubleJumpForce;

            flyForce = playerStats.flyForce;
            maxFlyHeight = playerStats.maxFlyHeight;
            flyHorizontalDamping = playerStats.flyHorizontalDamping;

            maxFlyEnergy = playerStats.maxFlyEnergy;
            flyDepletionRate = playerStats.flyDepletionRate;
            flyRegenRate = playerStats.flyRegenRate;
            flyRegenDelay = playerStats.flyRegenDelay;
        }

        groundLayerMaskValue = groundLayer.value;
    }

    void Start()
    {
        InitializeComponents();
        InitializeValues();
    }

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
        {
            Debug.LogError("PlayerController: Rigidbody component missing!");
            return;
        }

        // Setup Rigidbody for ball physics
        rb.mass = 1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.8f;

        // Find camera automatically if needed
        if (findCameraAutomatically && !cameraTransform)
        {
            // TODO: Inject camera reference via initializer to avoid scene search
            Camera mainCam = Camera.main;
            if (mainCam)
                cameraTransform = mainCam.transform;
            else
                cameraTransform = FindFirstObjectByType<Camera>()?.transform;
        }

        if (followCamera)
        {
            if (followCamera.Follow == null)
                followCamera.Follow = transform;
            if (followCamera.LookAt == null)
                followCamera.LookAt = transform;
            cameraTransform = followCamera.transform;
        }

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
    }

    private void InitializeValues()
    {
        flyEnergy = maxFlyEnergy;
        flyRegenTimer = 0f;
        coyoteTimeCounter = 0f;
        hasDoubleJumped = false;
        
        OnFlyEnergyChanged?.Invoke(flyEnergy, maxFlyEnergy);
    }

    void Update()
    {
        if (!rb) return;

        HandleInput();
        UpdateGroundState();
        UpdateFlightSystem();
        UpdateCoyoteTime();
        HandleJumping();

        if (showDebugInfo)
            ShowDebugInfo();
    }

    void FixedUpdate()
    {
        if (!rb) return;

        HandleBallMovement();
        HandleFlight();
        ApplyBallPhysics();
    }

    private void HandleInput()
    {
        if (RollABall.InputSystem.InputManager.Instance)
        {
            var input = RollABall.InputSystem.InputManager.Instance;
            movementInput = input.Movement;
            jumpPressed = input.JumpPressed;
            flyPressed = input.FlyHeld;
            sprintPressed = input.SprintHeld;

            if (input.SlidePressed)
                StartSlide();
            else if (input.SlideReleased)
                StopSlide();
        }
    }

    private void UpdateGroundState()
    {
        wasGrounded = isGrounded;
        CheckGrounded();

        if (isGrounded != wasGrounded)
        {
            OnGroundedChanged?.Invoke(isGrounded);
            
            if (isGrounded && !wasGrounded)
            {
                // Just landed
                hasDoubleJumped = false;
                PlaySound(landSound);
                
                if (jumpEffect)
                    jumpEffect.Play();
            }
        }
    }

    private void CheckGrounded()
    {
        // Check if ball is touching ground
        Vector3 center = transform.position;
        isGrounded = Physics.CheckSphere(center, groundCheckRadius, groundLayerMaskValue);

        // Alternative raycast method for more precise detection
        if (!isGrounded)
        {
            Vector3 rayStart = center + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.1f, groundLayerMaskValue);
        }
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumping()
    {
        if (!jumpPressed) return;

        bool canJump = coyoteTimeCounter > 0f;
        bool canDoubleJump = enableDoubleJump && !hasDoubleJumped && !isGrounded && rb.linearVelocity.y < 1f;

        if (canJump)
        {
            Jump(jumpForce);
            hasDoubleJumped = false;
            coyoteTimeCounter = 0f;
            PlaySound(jumpSound);
        }
        else if (canDoubleJump)
        {
            Jump(doubleJumpForce);
            hasDoubleJumped = true;
            PlaySound(doubleJumpSound);
        }
    }

    private void Jump(float force)
    {
        // Reset Y velocity for consistent jumps
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        
        // Apply jump force
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        
        if (jumpEffect)
            jumpEffect.Play();
    }

    private void UpdateFlightSystem()
    {
        bool wasFlying = IsFlying;

        if (flyPressed && flyEnergy > 0f)
        {
            IsFlying = true;
            flyEnergy -= flyDepletionRate * Time.deltaTime;
            flyEnergy = Mathf.Max(0, flyEnergy);
            flyRegenTimer = 0f;
        }
        else
        {
            IsFlying = false;
            
            // Energy regeneration with delay
            if (isGrounded && flyEnergy < maxFlyEnergy)
            {
                flyRegenTimer += Time.deltaTime;
                if (flyRegenTimer >= flyRegenDelay)
                {
                    flyEnergy += flyRegenRate * Time.deltaTime;
                    flyEnergy = Mathf.Min(maxFlyEnergy, flyEnergy);
                }
            }
        }

        // Events only on change
        if (IsFlying != wasFlying)
        {
            OnFlyingChanged?.Invoke(IsFlying);
            
            if (flyTrail)
                flyTrail.emitting = IsFlying;
        }

        OnFlyEnergyChanged?.Invoke(flyEnergy, maxFlyEnergy);
    }

    private void HandleBallMovement()
    {
        if (movementInput.magnitude < 0.1f) return;

        // Get camera-relative movement direction
        Vector3 moveDirection = GetCameraRelativeMovement(movementInput);
        moveDirection.y = 0f; // Keep movement horizontal
        moveDirection = moveDirection.normalized;

        // Calculate movement force
        float currentMoveForce = moveForce;
        
        // Apply modifiers
        if (IsSprinting && !IsSliding)
            currentMoveForce *= sprintMultiplier;
        
        if (!isGrounded && !IsFlying)
            currentMoveForce *= airControlMultiplier;

        // Check if we're at max speed
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentMaxSpeed = maxSpeed * (IsSprinting ? sprintMultiplier : 1f);
        
        if (horizontalVelocity.magnitude < currentMaxSpeed)
        {
            rb.AddForce(moveDirection * currentMoveForce, ForceMode.Acceleration);
        }
    }

    private Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (!cameraTransform)
            return new Vector3(input.x, 0, input.y);

        // Get camera forward and right vectors (projected on horizontal plane)
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        
        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        // Calculate movement direction relative to camera
        return cameraForward * input.y + cameraRight * input.x;
    }

    private void HandleFlight()
    {
        if (!IsFlying) return;

        // Vertical flight force (only if below max height)
        if (transform.position.y < maxFlyHeight)
        {
            rb.AddForce(Vector3.up * flyForce, ForceMode.Acceleration);
        }

        // Horizontal damping for better flight control
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(-horizontalVelocity * flyHorizontalDamping, ForceMode.Acceleration);
    }

    private void ApplyBallPhysics()
    {
        // Apply ball rolling resistance
        if (isGrounded && !IsSliding)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.linearVelocity = new Vector3(
                horizontalVelocity.x * ballDrag,
                rb.linearVelocity.y,
                horizontalVelocity.z * ballDrag
            );
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
        
        if (AudioManager.Instance != null && clip)
        {
            if (clip == jumpSound)
                AudioManager.Instance.PlaySoundAtPlayer("Jump");
            else if (clip == doubleJumpSound)
                AudioManager.Instance.PlaySoundAtPlayer("DoubleJump");
            else if (clip == landSound)
                AudioManager.Instance.PlaySoundAtPlayer("Land");
        }
    }

    // ===== Slide System =====
    private void StartSlide()
    {
        if (!isGrounded || IsSliding) return;
        
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
        
        slideCoroutine = StartCoroutine(SlideRoutine());
    }

    private void StopSlide()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }
        
        IsSliding = false;
        
        if (slideEffect)
            slideEffect.Stop();
    }

    private IEnumerator SlideRoutine()
    {
        IsSliding = true;
        
        if (slideEffect)
            slideEffect.Play();

        // Slide boost in current movement direction
        Vector3 slideDirection = rb.linearVelocity.normalized;
        if (slideDirection.magnitude > 0.1f)
        {
            slideDirection.y = 0f; // Keep slide horizontal
            rb.AddForce(slideDirection * moveForce * slideImpulseMultiplier, ForceMode.Impulse);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySoundAtPlayer("Slide");

        yield return new WaitForSeconds(slideDuration);
        
        slideCoroutine = null;
    }

    private void ShowDebugInfo()
    {
        // TODO: Replace Debug.Log spam with a dedicated debug overlay
        Debug.Log($"Ball Status - Grounded: {isGrounded}, Flying: {IsFlying}, Speed: {rb.linearVelocity.magnitude:F2}");
        Debug.Log($"Input: {movementInput}, Jump: {jumpPressed}, Fly: {flyPressed}");
        Debug.Log($"Fly Energy: {flyEnergy:F2}/{maxFlyEnergy}");
    }

    // ===== Public Methods =====
    public void ResetBall()
    {
        if (!rb) return;
        
        PhysicsUtils.ResetMotion(rb);
        flyEnergy = maxFlyEnergy;
        hasDoubleJumped = false;
        IsSliding = false;
        IsFlying = false;
        
        OnFlyEnergyChanged?.Invoke(flyEnergy, maxFlyEnergy);
    }

    public void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (rb)
            rb.AddForce(force, mode);
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        if (rb)
        {
            PhysicsUtils.ResetMotion(rb);
        }
    }

    // ===== Debug Visualization =====
    void OnDrawGizmosSelected()
    {
        // TODO: Disable gizmo drawing in production builds
        // Ground check visualization
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
        
        // Max fly height
        Gizmos.color = Color.blue;
        Vector3 flyHeightPos = transform.position;
        flyHeightPos.y = maxFlyHeight;
        Gizmos.DrawWireCube(flyHeightPos, new Vector3(2, 0.1f, 2));
        
        // Movement direction
        if (Application.isPlaying && movementInput.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Vector3 moveDir = GetCameraRelativeMovement(movementInput);
            Gizmos.DrawRay(transform.position, moveDir * 2f);
        }
    }

    // ===== Integration with other systems =====
    void OnCollisionEnter(Collision collision)
    {
        // Handle bouncing effects or special collision behaviors
        if (collision.gameObject.CompareTag("Bouncy"))
        {
            Vector3 bounceForce = collision.contacts[0].normal * -jumpForce * 0.5f;
            rb.AddForce(bounceForce, ForceMode.Impulse);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Handle collectibles, checkpoints, etc.
        if (other.CompareTag("Collectible"))
        {
            // This will be handled by the collectible itself
            // but we can add ball-specific reactions here
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySoundAtPlayer("Collect");
        }
    }

    // ===== Event cleanup =====
    void OnDestroy()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        // CLAUDE: FIXED - PlayerController primarily sends events rather than subscribing
        // No external event subscriptions to clean up in this implementation
        // Events: OnFlyEnergyChanged, OnGroundedChanged, OnFlyingChanged are automatically cleaned up
    }

    void OnValidate()
    {
        if (playerStats != null)
        {
            moveForce = playerStats.moveForce;
            maxSpeed = playerStats.maxSpeed;
            sprintMultiplier = playerStats.sprintMultiplier;
            airControlMultiplier = playerStats.airControlMultiplier;
            ballDrag = playerStats.ballDrag;

            slideImpulseMultiplier = playerStats.slideImpulseMultiplier;
            slideDuration = playerStats.slideDuration;

            jumpForce = playerStats.jumpForce;
            groundCheckRadius = playerStats.groundCheckRadius;
            groundCheckDistance = playerStats.groundCheckDistance;
            groundLayer = playerStats.groundLayer;
            coyoteTime = playerStats.coyoteTime;

            enableDoubleJump = playerStats.enableDoubleJump;
            doubleJumpForce = playerStats.doubleJumpForce;

            flyForce = playerStats.flyForce;
            maxFlyHeight = playerStats.maxFlyHeight;
            flyHorizontalDamping = playerStats.flyHorizontalDamping;

            maxFlyEnergy = playerStats.maxFlyEnergy;
            flyDepletionRate = playerStats.flyDepletionRate;
            flyRegenRate = playerStats.flyRegenRate;
            flyRegenDelay = playerStats.flyRegenDelay;
        }

        groundLayerMaskValue = groundLayer.value;
    }
}
