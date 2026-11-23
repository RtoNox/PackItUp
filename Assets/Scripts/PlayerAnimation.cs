using UnityEngine;
using Unity.Netcode;

public class PlayerAnimation : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Fallback Controller")]
    [Tooltip("Automatically applied if a spawned character prefab's Animator has no Controller assigned.")]
    [SerializeField] private RuntimeAnimatorController fallbackController;

    private PlayerMovement playerMovement;
    private bool lastSentIsRunning;

    private NetworkVariable<bool> netIsRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private static readonly int IsRunningHash =
        Animator.StringToHash("IsRunning");

    private static readonly int EmoteHash =
        Animator.StringToHash("Emote");

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && fallbackController == null)
            fallbackController = animator.runtimeAnimatorController;

        if (playerMovement == null)
            Debug.LogError("PlayerAnimation: PlayerMovement not found!", this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (animator == null)
            SetAnimator(GetComponentInChildren<Animator>());

        if (IsOwner && playerMovement != null)
        {
            lastSentIsRunning = playerMovement.IsMoving;
            SetIsRunningServerRpc(lastSentIsRunning);
        }
    }

    // Called by PlayerCharacterVisual right before destroying the old character model
    public void ClearAnimator()
    {
        animator = null;
    }

    // Called by PlayerCharacterVisual right after spawning the new character model
    public void SetAnimator(Animator newAnimator)
    {
        if (newAnimator == null)
            return;

        if (newAnimator.runtimeAnimatorController == null && fallbackController != null)
            newAnimator.runtimeAnimatorController = fallbackController;

        animator = newAnimator;
        animator.Rebind();
        animator.Update(0f);

        if (IsSpawned)
        {
            bool currentRunning = IsOwner && playerMovement != null
                ? playerMovement.IsMoving
                : netIsRunning.Value;

            animator.SetBool(IsRunningHash, currentRunning);
        }
    }

    private void Update()
    {
        // Do not run networked animation logic on the offline MainMenu showcase player
        if (!IsSpawned)
            return;

        // Re-acquire Animator if the character model spawned after Awake()
        if (animator == null)
        {
            Animator foundAnimator = GetComponentInChildren<Animator>();
            if (foundAnimator != null)
                SetAnimator(foundAnimator);
        }

        if (IsOwner)
        {
            if (playerMovement == null)
                return;

            bool isMoving = playerMovement.IsMoving;

            if (animator != null)
                animator.SetBool(IsRunningHash, isMoving);

            if (isMoving != lastSentIsRunning)
            {
                lastSentIsRunning = isMoving;
                SetIsRunningServerRpc(isMoving);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TriggerEmoteServerRpc();
            }
        }
        else
        {
            // Remote players apply the synced running state to their current character model
            if (animator != null)
                animator.SetBool(IsRunningHash, netIsRunning.Value);
        }
    }

    [ServerRpc]
    private void SetIsRunningServerRpc(bool isRunning)
    {
        netIsRunning.Value = isRunning;
    }

    [ServerRpc]
    private void TriggerEmoteServerRpc()
    {
        TriggerEmoteClientRpc();
    }

    [ClientRpc]
    private void TriggerEmoteClientRpc()
    {
        if (animator != null)
            animator.SetTrigger(EmoteHash);
    }
}