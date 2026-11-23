using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);

    [Header("Rotation Settings")]
    [Tooltip("Fixed vertical angle of the camera (in degrees)")]
    [SerializeField] private float fixedPitchAngle = 30f;

    [Tooltip("How fast the camera rotates with Q/E keys")]
    [SerializeField] private float keyboardRotationSpeed = 100f;

    [Header("Input Settings")]
    [Tooltip("Use Q/E keys to rotate camera")]
    [SerializeField] private bool useKeyboardRotation = true;

    [Tooltip("Use mouse X axis to rotate camera")]
    [SerializeField] private bool useMouseRotation = true;

    [SerializeField] private float mouseSensitivity = 3f;

    [Tooltip("Lock and hide the cursor when the game starts")]
    [SerializeField] private bool lockCursor = true;

    [Header("Collision Settings")]
    [Tooltip("Layers the camera will collide with")]
    [SerializeField] private LayerMask collisionMask = ~0;

    [Tooltip("Radius of the collision check sphere")]
    [SerializeField] private float collisionRadius = 0.3f;

    [Tooltip("Small gap kept between camera and wall")]
    [SerializeField] private float collisionPadding = 0.1f;

    [Tooltip("How fast the camera returns to original distance")]
    [SerializeField] private float returnSpeed = 10f;

    private float currentYaw = 0f;

    // Tracks the current distance the camera is pulled in
    private float currentDistance;

    void Start()
    {
        currentYaw = transform.eulerAngles.y;
        currentDistance = offset.magnitude;

        if (lockCursor && useMouseRotation)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // Ctrl toggles the cursor
        if (Input.GetKeyDown(KeyCode.LeftControl) ||
            Input.GetKeyDown(KeyCode.RightControl))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // Escape unlocks cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Find the local player if we don't have one yet
        if (target == null)
        {
            FindLocalPlayer();
        }

        // Keyboard camera rotation
        if (useKeyboardRotation)
        {
            if (Input.GetKey(KeyCode.Q))
                currentYaw -= keyboardRotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E))
                currentYaw += keyboardRotationSpeed * Time.deltaTime;
        }

        // Mouse camera rotation
        if (useMouseRotation &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            currentYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        }
    }

    void LateUpdate()
    {
        // Player hasn't spawned yet
        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(
            fixedPitchAngle,
            currentYaw,
            0f
        );

        // Direction from target to the ideal camera position
        Vector3 desiredDir = rotation * offset.normalized;
        float desiredDistance = offset.magnitude;

        // Cast a sphere from the target outward to detect walls
        float allowedDistance = desiredDistance;

        if (Physics.SphereCast(
                target.position,
                collisionRadius,
                desiredDir,
                out RaycastHit hit,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            allowedDistance = Mathf.Max(0f, hit.distance - collisionPadding);
        }

        // When a wall is present, snap in instantly (so we never clip)
        // When no wall is present, smoothly return to original distance
        if (allowedDistance < currentDistance)
        {
            // Wall in the way -> move in immediately
            currentDistance = allowedDistance;
        }
        else
        {
            // No wall -> smoothly move back out
            currentDistance = Mathf.Lerp(
                currentDistance,
                allowedDistance,
                returnSpeed * Time.deltaTime
            );
        }

        Vector3 position = target.position + desiredDir * currentDistance;

        transform.position = position;
        transform.rotation = rotation;
    }

    private void FindLocalPlayer()
    {
        // NetworkManager hasn't been created yet
        if (NetworkManager.Singleton == null)
            return;

        // Look through all NetworkObjects currently spawned
        NetworkObject[] players =
            FindObjectsByType<NetworkObject>(
                FindObjectsSortMode.None
            );

        foreach (NetworkObject player in players)
        {
            // Only use the player owned by this client
            if (player.IsOwner && player.CompareTag("Player"))
            {
                target = player.transform;
                break;
            }
        }
    }

    public void SetPitchAngle(float angle)
    {
        fixedPitchAngle = angle;
    }

    // Optional: visualize the collision sphere in the editor
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(fixedPitchAngle, currentYaw, 0f);
        Vector3 dir = rotation * offset.normalized;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position + dir * currentDistance, collisionRadius);
    }
}