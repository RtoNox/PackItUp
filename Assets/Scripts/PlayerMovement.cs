using System;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Animator))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaConsumptionRate = 5f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenDelay = 1f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
<<<<<<< Updated upstream
    [SerializeField] private PlayerAnimation playerAnimation;
=======
    [SerializeField] private Animator animator;

    // Animator parameter hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
>>>>>>> Stashed changes

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => currentStamina / maxStamina;
    public bool IsSprinting => isSprinting;

    private CharacterController controller;
    private Vector3 velocity;

    private float currentStamina;
    private bool isSprinting;
    private float regenDelayTimer;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentStamina = maxStamina;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (animator != null) animator.enabled = false;
            Destroy(this);
        }
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // WASD input detection
        bool hasMoveInput = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * vertical + camRight * horizontal).normalized;

<<<<<<< Updated upstream
        Debug.Log("PlayerMovement isMoving: " + isMoving);
        Debug.Log("PlayerAnimation found: " + (playerAnimation != null));

        if (playerAnimation != null)
            playerAnimation.SetRunning(isMoving);

        isSprinting = wantsToSprint && isMoving && currentStamina > 0f;
=======
        // Sprint only if input is held AND there is movement AND stamina remains
        isSprinting = wantsToSprint && hasMoveInput && currentStamina > 0f;
>>>>>>> Stashed changes

        if (isSprinting)
        {
            currentStamina -= staminaConsumptionRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            regenDelayTimer = staminaRegenDelay;
        }
        else
        {
            if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (hasMoveInput)
        {
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // ---- Animation Parameters ----
        // If WASD is pressed → play run animation
        // If no WASD input → stop and return to idle
        animator.SetBool(IsRunningHash, hasMoveInput);
    }
}