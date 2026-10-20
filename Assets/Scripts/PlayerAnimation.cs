using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerAnimation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private OwnerNetworkAnimator networkAnimator;

    private PlayerMovement playerMovement;

    private static readonly int IsRunningHash =
        Animator.StringToHash("IsRunning");

    private static readonly int EmoteHash =
        Animator.StringToHash("Emote");

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (networkAnimator == null)
            networkAnimator =
                GetComponentInChildren<OwnerNetworkAnimator>();

        if (animator == null)
            Debug.LogError("PlayerAnimation: Animator not found!", this);

        if (networkAnimator == null)
            Debug.LogError(
                "PlayerAnimation: OwnerNetworkAnimator not found!",
                this
            );

        if (playerMovement == null)
            Debug.LogError(
                "PlayerAnimation: PlayerMovement not found!",
                this
            );
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
            "PlayerAnimation spawned. IsOwner: " + IsOwner
        );
    }

    private void Update()
    {
        // Only the player controlling this character
        // should read its local input.
        if (!IsOwner)
            return;

        if (animator == null ||
            networkAnimator == null ||
            playerMovement == null)
            return;

        // Owner-authoritative NetworkAnimator synchronizes
        // this parameter with the other players.
        animator.SetBool(
            IsRunningHash,
            playerMovement.IsMoving
        );

        // Press 1 to trigger the emote across the network.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            networkAnimator.SetTrigger(EmoteHash);
        }
    }
}