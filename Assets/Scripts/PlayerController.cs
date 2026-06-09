using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Runner Movement")]
    [SerializeField] private float forwardSpeed = 6f;       // Constant forward running speed
    [SerializeField] private float horizontalSpeed = 8f;    // Steer speed left/right
    [SerializeField] private float dragSensitivity = 0.05f; // Mouse/Touch drag sensitivity

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    // Public properties to sync physics with PlayerCrowdManager
    public float ForwardSpeed => forwardSpeed;
    public float Gravity => gravity;

    private Camera mainCamera;
    private CharacterController controller;
    private Vector3 velocity;
    private float coyoteTimer;
    private float jumpBufferTimer;

    // Input System references
    private Mouse mouse;
    private Keyboard keyboard;

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

        if (mouse == null || keyboard == null) return;

        HandleMovement();
        HandleJumping();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        // Stop movement if game is not active OR if game over has been triggered (slow-mo phase)
        bool gameOverTriggered = crowdManager != null && crowdManager.IsGameOver;

        if (!UIManager.IsGameActive || gameOverTriggered)
        {
            // Only apply gravity if player is not grounded
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            return;
        }

        float inputX = 0f;
        float currentForwardSpeed = forwardSpeed;

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
            if (mouse.leftButton.isPressed)
            {
                float dragDeltaX = mouse.delta.ReadValue().x;
                inputX = dragDeltaX * dragSensitivity;
            }

            // 2. Keyboard Fallback (A/D or Left/Right Arrow Keys)
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                inputX = -1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
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
        if (keyboard.spaceKey.wasPressedThisFrame)
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
}
