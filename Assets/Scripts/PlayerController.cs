using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Runner Movement")]
    [SerializeField] private float forwardSpeed = 6f;       // Constant forward running speed
    [SerializeField] private float horizontalSpeed = 8f;    // Steer speed left/right
    [SerializeField] private float dragSensitivity = 0.05f; // Mouse/Touch drag sensitivity
    [SerializeField] private float startForwardSpeed = 6f;
    [SerializeField] private float maxForwardSpeed = 6f;
    [SerializeField] private float speedRampStartZ = 0f;
    [SerializeField] private float speedRampEndZ = 999f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField, Range(0.05f, 1f)] private float jumpReleaseVelocityMultiplier = 0.35f;

    // Public properties to sync physics with PlayerCrowdManager
    public float ForwardSpeed => GetForwardSpeed();
    public float Gravity => gravity;
    public float JumpForce => jumpForce;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsAirborne => controller != null && !controller.isGrounded;
    public float VerticalVelocity => velocity.y;
    public float HeightAboveGround => Mathf.Max(0f, transform.position.y);

    public bool HasCleared(float obstacleHeight, float margin)
    {
        return HeightAboveGround >= obstacleHeight + Mathf.Max(0f, margin);
    }

    private Camera mainCamera;
    private CharacterController controller;
    private Vector3 velocity;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpCutApplied;

    // Input System references
    private Mouse mouse;
    private Keyboard keyboard;
    private Touchscreen touchscreen;

    private PlayerCrowdManager crowdManager;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        crowdManager = GetComponent<PlayerCrowdManager>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        // Release cursor lock so dragging on mobile/PC is natural
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Cache current devices each frame
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        touchscreen = Touchscreen.current;

        HandleMovement();
        HandleJumping();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        // Stop movement if game is not active OR if game over has been triggered (slow-mo phase)
        // A stale terminal flag must not freeze a crowd that has since been
        // replenished by a gate or scene-level recovery flow.
        bool gameOverTriggered = crowdManager != null &&
            (crowdManager.IsLeadFallGameOver ||
            (crowdManager.IsGameOver && crowdManager.ActiveRunnerCount <= 0));

        if (!UIManager.IsGameActive || gameOverTriggered)
        {
            // Only apply gravity if player is not grounded
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            return;
        }

        float inputX = 0f;
        float currentForwardSpeed = GetForwardSpeed();

        bool isFighting = crowdManager != null && crowdManager.IsFighting;

        if (isFighting)
        {
            // Slow down forward speed during combat to make it look like a struggle
            currentForwardSpeed = forwardSpeed * 0.25f;

            // Lock controls and pull player towards target enemy center on X axis
            float targetX = crowdManager.EnemyTargetX;
            float lerpedX = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * 5f);
            
            // Apply forward movement + vertical gravity
            Vector3 movement = (Vector3.forward * currentForwardSpeed + Vector3.up * velocity.y) * Time.deltaTime;
            
            // Calculate horizontal offset to apply via controller.Move
            Vector3 horizontalMoveOffset = Vector3.right * (lerpedX - transform.position.x);
            controller.Move(horizontalMoveOffset + movement);
        }
        else
        {
            // 1. Mouse/Touch Drag Input (Holding click & dragging left/right)
            if (mouse != null && mouse.leftButton.isPressed)
            {
                float dragDeltaX = mouse.delta.ReadValue().x;
                inputX = dragDeltaX * dragSensitivity;
            }

            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                inputX = touchscreen.primaryTouch.delta.ReadValue().x * dragSensitivity;
            }

            // 2. Keyboard Fallback (A/D or Left/Right Arrow Keys)
            if (keyboard != null && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed))
            {
                inputX = -1f;
            }
            if (keyboard != null && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed))
            {
                inputX = 1f;
            }

            // Constant forward velocity combined with user-controlled horizontal velocity
            Vector3 horizontalMove = Vector3.right * inputX * horizontalSpeed;
            Vector3 forwardMove = Vector3.forward * currentForwardSpeed;

            // Apply physical movement via CharacterController (including jump/gravity velocity)
            controller.Move((horizontalMove + forwardMove + Vector3.up * velocity.y) * Time.deltaTime);
        }

        // Keep character rotation locked completely forward at all times
        transform.rotation = Quaternion.identity;
    }

    private void HandleJumping()
    {
        // Coyote time — allow a short grace period after leaving ground
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // Jump buffer — queue a jump if pressed slightly before landing
        bool jumpPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            jumpPressed = true;
        }

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Execute jump when both timers are valid
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpForce;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
            jumpCutApplied = false;
        }

        // Releasing jump early cuts the upward velocity. Holding Space preserves
        // the full arc needed to clear the physical Level 5 road gap.
        bool jumpHeld = (keyboard != null && keyboard.spaceKey.isPressed) ||
            (touchscreen != null && touchscreen.primaryTouch.press.isPressed);
        if (controller.isGrounded)
        {
            jumpCutApplied = false;
        }
        else if (!jumpHeld && !jumpCutApplied && velocity.y > 0f)
        {
            velocity.y *= jumpReleaseVelocityMultiplier;
            jumpCutApplied = true;
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            // Small downward force to keep grounded
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    private float GetForwardSpeed()
    {
        if (maxForwardSpeed <= startForwardSpeed) return forwardSpeed;
        float progress = Mathf.Clamp01((transform.position.z - speedRampStartZ) / Mathf.Max(0.01f, speedRampEndZ - speedRampStartZ));
        return Mathf.Lerp(startForwardSpeed, maxForwardSpeed, progress);
    }
}
