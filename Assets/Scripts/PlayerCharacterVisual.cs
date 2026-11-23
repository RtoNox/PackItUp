using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerCharacterVisual : NetworkBehaviour
{
    [Header("Character Model Holder")]
    [Tooltip("Empty child object inside the Player Prefab where the character mesh spawns.")]
    [SerializeField] private Transform modelHolder;

    [Tooltip("Optional fallback character shown if no character is equipped.")]
    [SerializeField] private GachaCharacterData fallbackCharacter;

    private GameObject currentModelInstance;

    private NetworkVariable<FixedString64Bytes> netCharacterID = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (modelHolder == null)
            modelHolder = transform;
    }

    private void Start()
    {
        // Offline showcase in MainMenu: OnNetworkSpawn() won't run, so apply local selection directly
        if (!IsSpawned)
        {
            ApplyLocalCharacter();

            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged += ApplyLocalCharacter;
        }
    }

    public override void OnNetworkSpawn()
    {
        netCharacterID.OnValueChanged += OnNetworkCharacterChanged;

        if (IsOwner)
        {
            SyncLocalCharacterToServer();

            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged += SyncLocalCharacterToServer;
        }

        // Apply current value immediately so late-joining clients see existing players
        SpawnCharacterModel(netCharacterID.Value.ToString());
    }

    public override void OnNetworkDespawn()
    {
        netCharacterID.OnValueChanged -= OnNetworkCharacterChanged;

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= SyncLocalCharacterToServer;
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= ApplyLocalCharacter;
            PlayerInventory.Instance.OnInventoryChanged -= SyncLocalCharacterToServer;
        }
    }

    private void ApplyLocalCharacter()
    {
        string charID = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetEquippedCharacterID()
            : "";

        SpawnCharacterModel(charID);
    }

    private void SyncLocalCharacterToServer()
    {
        if (!IsOwner || !IsSpawned) return;

        string charID = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetEquippedCharacterID()
            : "";

        SetEquippedCharacterServerRpc(new FixedString64Bytes(charID));
    }

    [ServerRpc]
    private void SetEquippedCharacterServerRpc(FixedString64Bytes characterID)
    {
        netCharacterID.Value = characterID;
    }

    private void OnNetworkCharacterChanged(FixedString64Bytes oldID, FixedString64Bytes newID)
    {
        SpawnCharacterModel(newID.ToString());
    }

    private void SpawnCharacterModel(string characterID)
    {
        PlayerAnimation playerAnim = GetComponentInChildren<PlayerAnimation>();
        if (playerAnim != null)
            playerAnim.ClearAnimator();

        // Remove any existing character mesh inside ModelHolder
        if (currentModelInstance != null)
        {
            Destroy(currentModelInstance);
            currentModelInstance = null;
        }
        else if (modelHolder != transform)
        {
            // Clear any placeholder mesh placed inside ModelHolder in the prefab
            for (int i = modelHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(modelHolder.GetChild(i).gameObject);
            }
        }

        GachaCharacterData characterData = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetCharacterAsset(characterID)
            : fallbackCharacter;

        if (characterData == null)
            characterData = fallbackCharacter;

        if (characterData == null || characterData.characterPrefab == null)
            return;

        currentModelInstance = Instantiate(characterData.characterPrefab, modelHolder);
        currentModelInstance.transform.localPosition = Vector3.zero;
        currentModelInstance.transform.localRotation = Quaternion.identity;
        currentModelInstance.transform.localScale = Vector3.one;

        BindAnimator(currentModelInstance);
    }

    private void LateUpdate()
    {
        if (currentModelInstance != null)
        {
            currentModelInstance.transform.localPosition = Vector3.zero;
            currentModelInstance.transform.localRotation = Quaternion.identity;
        }
    }

    private void BindAnimator(GameObject spawnedModel)
    {
        PlayerAnimation playerAnim = GetComponentInChildren<PlayerAnimation>();
        Animator rootAnimator = GetComponent<Animator>();
        Animator modelAnimator = spawnedModel.GetComponentInChildren<Animator>();

        if (modelAnimator != null)
        {
            if (modelAnimator.runtimeAnimatorController == null &&
                rootAnimator != null &&
                rootAnimator.runtimeAnimatorController != null)
            {
                modelAnimator.runtimeAnimatorController = rootAnimator.runtimeAnimatorController;
            }

            if (rootAnimator != null && rootAnimator != modelAnimator)
                rootAnimator.enabled = false;

            modelAnimator.enabled = true;

            if (playerAnim != null)
            {
                playerAnim.SetAnimator(modelAnimator);
            }
            else
            {
                modelAnimator.Rebind();
                modelAnimator.Update(0f);
            }
        }
        else if (rootAnimator != null)
        {
            rootAnimator.enabled = true;

            if (playerAnim != null)
            {
                playerAnim.SetAnimator(rootAnimator);
            }
            else
            {
                rootAnimator.Rebind();
                rootAnimator.Update(0f);
            }
        }
    }
}