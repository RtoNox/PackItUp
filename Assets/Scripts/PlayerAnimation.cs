using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using TMPro;

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

    public void ClearAnimator()
    {
        animator = null;
    }

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
        else
        {
            animator.SetBool(IsRunningHash, false);
        }
    }

    // Can also be hooked to a UI Button's OnClick() in MainMenu
    public void TriggerEmote()
    {
        if (animator == null)
            return;

        if (!IsSpawned)
        {
            animator.SetTrigger(EmoteHash);
        }
        else if (IsOwner)
        {
            TriggerEmoteServerRpc();
        }
    }

    private void Update()
    {
        // Re-acquire Animator if the character model spawned after Awake()
        if (animator == null)
        {
            Animator foundAnimator = GetComponentInChildren<Animator>();
            if (foundAnimator != null)
                SetAnimator(foundAnimator);
        }

        // Offline MainMenu showcase mode: stay in Idle and only allow Emote
        if (!IsSpawned)
        {
            if (animator != null)
                animator.SetBool(IsRunningHash, false);

            if (Input.GetKeyDown(KeyCode.Alpha1) && !IsTypingInInputField())
            {
                TriggerEmote();
            }
            return;
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
            if (animator != null)
                animator.SetBool(IsRunningHash, netIsRunning.Value);
        }
    }

    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            return false;

        return EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
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