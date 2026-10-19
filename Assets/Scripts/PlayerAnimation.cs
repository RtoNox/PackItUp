using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private PlayerMovement playerMovement;

    private static readonly int IsRunningHash =
        Animator.StringToHash("IsRunning");

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerMovement == null)
            Debug.LogError("PlayerMovement not found in parent!", this);

        if (animator == null)
            Debug.LogError("Animator not found!", this);
    }

    private void Update()
    {
        if (animator == null || playerMovement == null)
            return;

        animator.SetBool(
            IsRunningHash,
            playerMovement.IsMoving
        );
    }
}