using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLobbyState : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string lobbySceneName = "RoomScene";

    [Header("Gameplay Components to Disable in Lobby")]
    [Tooltip("Drag your player movement, look/rotation, and input scripts here.")]
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [Tooltip("Drag the player's personal Camera and AudioListener here so the lobby camera stays active.")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener playerAudioListener;

    private void Start()
    {
        bool isLobby = SceneManager.GetActiveScene().name == lobbySceneName;

        if (!isLobby) return;

        // 1. Disable movement and any assigned gameplay scripts
        if (scriptsToDisable != null)
        {
            foreach (MonoBehaviour script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        // 2. Disable the player's own camera and audio listener
        if (playerCamera != null) playerCamera.enabled = false;
        if (playerAudioListener != null) playerAudioListener.enabled = false;

        // 3. Disable abilities so Q and E do nothing in the lobby
        DisableAbilities();

        // 4. Freeze physics so the character stands completely still
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // 5. Turn the character to face the lobby camera
        FaceLobbyCamera();
    }

    private void DisableAbilities()
    {
        PlayerAbilityManager abilityManager = GetComponent<PlayerAbilityManager>();
        if (abilityManager != null) abilityManager.enabled = false;

        if (TryGetComponent(out AbilitySteal steal)) steal.enabled = false;
        if (TryGetComponent(out AbilityStun stun)) stun.enabled = false;
        if (TryGetComponent(out AbilityJammer jammer)) jammer.enabled = false;
        if (TryGetComponent(out AbilityWarp warp)) warp.enabled = false;
        if (TryGetComponent(out AbilityBehindYou behindYou)) behindYou.enabled = false;
        if (TryGetComponent(out AbilityDuckKun duckKun)) duckKun.enabled = false;
    }

    private void FaceLobbyCamera()
    {
        Camera lobbyCam = Camera.main;
        if (lobbyCam == null) return;

        Vector3 directionToCamera = lobbyCam.transform.position - transform.position;
        directionToCamera.y = 0f; // Keep the character standing upright

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }
}