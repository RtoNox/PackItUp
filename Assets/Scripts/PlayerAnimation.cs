using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private float movementThreshold = 0.1f;

    private PlayerMovement playerMovement;

    private static readonly int IsRunningHash =
        Animator.StringToHash("IsRunning");

    private static readonly int EmoteHash =
        Animator.StringToHash("Emote");

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogError("PlayerAnimation: Animator not found!", this);

        if (playerMovement == null)
            Debug.LogError("PlayerAnimation: PlayerMovement not found!", this);
    }

    private void Update()
    {
        if (animator == null || playerMovement == null)
            return;

        if (!playerMovement.IsOwner)
            return;

        animator.SetBool(IsRunningHash, playerMovement.IsMoving);

        // Press 1 to play the emote.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            animator.ResetTrigger(EmoteHash);
            animator.SetTrigger(EmoteHash);
        }
    }
}