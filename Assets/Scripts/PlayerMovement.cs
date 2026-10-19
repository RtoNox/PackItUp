using UnityEngine;
using Unity.Netcode;

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

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    public float StaminaPercent =>
        maxStamina > 0f ? currentStamina / maxStamina : 0f;

    public bool IsSprinting => isSprinting;

    // Used by PlayerAnimation.cs
    public bool IsMoving { get; private set; }

    private CharacterController controller;
    private Vector3 velocity;

    private float currentStamina;
    private bool isSprinting;
    private float regenDelayTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
    }

    private void Start()
    {
        if (!IsOwner)
            return;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Destroy(this);
            return;
        }

        Debug.Log("PlayerMovement initialized for the local player.");
    }

    private void Update()
    {
        if (!IsOwner || controller == null)
            return;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform == null)
            return;

        // Read keyboard input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool hasMoveInput =
            Mathf.Abs(horizontal) > 0.1f ||
            Mathf.Abs(vertical) > 0.1f;

        // Expose movement state to PlayerAnimation.cs
        IsMoving = hasMoveInput;

        bool wantsToSprint =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        // Calculate movement direction relative to the camera
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection =
            (camForward * vertical + camRight * horizontal).normalized;

        // Sprint and stamina
        isSprinting =
            wantsToSprint && hasMoveInput && currentStamina > 0f;

        if (isSprinting)
        {
            currentStamina -=
                staminaConsumptionRate * Time.deltaTime;

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
                currentStamina +=
                    staminaRegenRate * Time.deltaTime;

                currentStamina =
                    Mathf.Min(currentStamina, maxStamina);
            }
        }

        float currentSpeed =
            isSprinting ? sprintSpeed : walkSpeed;

        // Move and rotate the character
        if (hasMoveInput)
        {
            controller.Move(
                moveDirection * currentSpeed * Time.deltaTime
            );

            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }
}