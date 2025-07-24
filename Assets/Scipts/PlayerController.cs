using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Bewegung")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float slideDrag = 0.5f;
    [SerializeField] private float normalDrag = 1f;
    [SerializeField] private float airControlMultiplier = 0.6f;

    [Header("Springen")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private float coyoteTime = 0.2f; // Grace period for jumping after leaving ground

    [Header("Doppelsprung")]
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpForce = 4f; // Separate force for double jump

    [Header("Fliegen")]
    [SerializeField] private float flyForce = 6f;
    [SerializeField] private float maxFlyHeight = 20f;
    [SerializeField] private float flyHorizontalDamping = 0.8f;

    [Header("Flugenergie")]
    [SerializeField] private float maxFlyEnergy = 3f;
    [SerializeField] private float flyDepletionRate = 1f;
    [SerializeField] private float flyRegenRate = 1f;
    [SerializeField] private float flyRegenDelay = 1f; // Delay before regen starts

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip doubleJumpSound;
    [SerializeField] private AudioClip landSound;

    [Header("Effects")]
    [SerializeField] private ParticleSystem jumpEffect;
    [SerializeField] private ParticleSystem slideEffect;
    [SerializeField] private TrailRenderer flyTrail;

    // Private fields
    private Rigidbody rb;
    private Vector2 movementInput;
    private bool jumpRequested;
    private bool flyInputActive;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasDoubleJumped;
    private float coyoteTimeCounter;
    private float flyRegenTimer;
    private Coroutine slideCoroutine;

    // Public properties für bessere Kapselung
    public float FlyEnergy => flyEnergy;
    public float MaxFlyEnergy => maxFlyEnergy;
    public bool IsGrounded => isGrounded;
    public bool IsSprinting { get; private set; }
    public bool IsSliding { get; private set; }
    public bool IsFlying { get; private set; }
    public Vector3 Velocity => rb ? rb.linearVelocity : Vector3.zero;

    [HideInInspector] public float flyEnergy;

    // Events für UI und andere Systeme
    public System.Action<float, float> OnFlyEnergyChanged;
    public System.Action<bool> OnGroundedChanged;
    public System.Action<bool> OnFlyingChanged;

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

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        rb.linearDamping = normalDrag;
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

        UpdateGroundState();
        HandleJumping();
        UpdateFlightSystem();
        UpdateCoyoteTime();
    }

    void FixedUpdate()
    {
        if (!rb) return;

        HandleMovement();
        HandleFlight();
        UpdateDrag();
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
        Vector3 origin = transform.position + Vector3.down * 0.5f;
        isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down, 
            out RaycastHit hit, groundCheckDistance, groundLayer);

        // Debug visualization
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
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
        if (!jumpRequested) return;

        bool canJump = coyoteTimeCounter > 0f;
        bool canDoubleJump = enableDoubleJump && !hasDoubleJumped && !isGrounded;

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

        jumpRequested = false;
    }

    private void Jump(float force)
    {
        // Reset Y velocity before jumping for consistent jump height
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);
        
        if (jumpEffect)
            jumpEffect.Play();
    }

    private void UpdateFlightSystem()
    {
        bool wasFlying = IsFlying;

        if (flyInputActive && flyEnergy > 0f)
        {
            IsFlying = true;
            flyEnergy -= flyDepletionRate * Time.deltaTime;
            flyEnergy = Mathf.Max(0, flyEnergy);
            flyRegenTimer = 0f;
        }
        else
        {
            IsFlying = false;
            
            // Regeneration mit Delay
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

        // Events nur bei Änderung auslösen
        if (IsFlying != wasFlying)
        {
            OnFlyingChanged?.Invoke(IsFlying);
            
            if (flyTrail)
                flyTrail.emitting = IsFlying;
        }

        OnFlyEnergyChanged?.Invoke(flyEnergy, maxFlyEnergy);
    }

    private void HandleMovement()
    {
        Vector3 move = new Vector3(movementInput.x, 0f, movementInput.y).normalized;
        float currentSpeed = moveSpeed;

        // Speed modifiers
        if (IsSprinting && !IsSliding) 
            currentSpeed *= sprintMultiplier;
        
        if (!isGrounded && !IsFlying) 
            currentSpeed *= airControlMultiplier;

        // Apply movement force
        rb.AddForce(move * currentSpeed, ForceMode.Acceleration);
    }

    private void HandleFlight()
    {
        if (!IsFlying) return;

        // Vertical flight force (only if below max height)
        if (transform.position.y < maxFlyHeight)
        {
            rb.AddForce(Vector3.up * flyForce, ForceMode.Acceleration);
        }

        // Horizontal damping während des Flugs für bessere Kontrolle
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(-horizontalVelocity * flyHorizontalDamping, ForceMode.Acceleration);
    }

    private void UpdateDrag()
    {
        rb.linearDamping = IsSliding ? slideDrag : normalDrag;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
            audioSource.PlayOneShot(clip);
    }

    // ===== Slide System =====
    private void StartSlide()
    {
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

        // Slide-Boost am Anfang
        Vector3 slideDirection = transform.forward;
        rb.AddForce(slideDirection * moveSpeed * 0.5f, ForceMode.Impulse);

        yield return null;
        slideCoroutine = null;
    }

    // ===== Input Actions =====
    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpRequested = true;
    }

    public void OnSprint(InputValue value)
    {
        IsSprinting = value.isPressed;
    }

    public void OnSlide(InputValue value)
    {
        if (value.isPressed)
            StartSlide();
        else
            StopSlide();
    }

    public void OnFly(InputValue value)
    {
        flyInputActive = value.isPressed;
    }

    // ===== Debug =====
    void OnDrawGizmosSelected()
    {
        // Ground check visualization
        Vector3 origin = transform.position + Vector3.down * 0.5f;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, groundCheckRadius);
        
        // Max fly height
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + Vector3.up * maxFlyHeight, new Vector3(1, 0.1f, 1));
    }
}