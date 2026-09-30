using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
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

    [Tooltip("Lock and hide the cursor for mouse look")]
    [SerializeField] private bool lockCursor = true;

    private float currentYaw = 0f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        currentYaw = transform.eulerAngles.y;

        if (lockCursor && useMouseRotation)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (lockCursor && useMouseRotation && Input.GetMouseButtonDown(0) 
            && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (useKeyboardRotation && Cursor.lockState == CursorLockMode.Locked)
        {
            if (Input.GetKey(KeyCode.Q))
                currentYaw -= keyboardRotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E))
                currentYaw += keyboardRotationSpeed * Time.deltaTime;
        }

        if (useMouseRotation && Cursor.lockState == CursorLockMode.Locked)
        {
            currentYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        }

        Quaternion rotation = Quaternion.Euler(fixedPitchAngle, currentYaw, 0f);

        Vector3 position = target.position + rotation * offset;

        transform.position = position;
        transform.rotation = rotation;
    }

    public void SetPitchAngle(float angle)
    {
        fixedPitchAngle = angle;
    }
}