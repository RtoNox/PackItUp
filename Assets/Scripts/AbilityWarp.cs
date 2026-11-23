using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class AbilityWarp : NetworkBehaviour, IAbilityCooldown
{
    [Header("Ability Info & Cooldown")]
    public string abilityName = "Warp";
    public Sprite abilityIcon;
    public float cooldownTime = 45f;
    public float selectionTimeLimit = 3f;

    [Header("Upgrade Settings (Max Lv 3)")]
    [Range(1, 3)] public int abilityLevel = 1;
    public int maxAbilityLevel = 3;

    private float lastUseTime = -999f;
    private float clientCooldownEndTime = -1f;
    private bool wasOnCooldown = false;

    private readonly NetworkVariable<double> cooldownEndServerTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.E;

    [Header("Maze World Boundaries")]
    public float mazeMinX = -50f;
    public float mazeMaxX = 50f;
    public float mazeMinZ = -50f;
    public float mazeMaxZ = 50f;
    public float maxWarpClamp = 45f;

    [Header("Ground Raycast Settings")]
    public float rayStartHeight = 50f;
    public float rayDistance = 120f;

    private GameObject mapPanelInstance;
    private bool isSelectingLocation = false;
    private float selectionTimer = 0f;
    private bool uiInitialized = false;
    private Coroutine selectionCoroutine;

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        abilityLevel = Mathf.Clamp(AbilityDataHolder.GetLevel(abilityName), 1, maxAbilityLevel);

        Debug.Log($"[Warp] OnNetworkSpawn | IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, Lv={abilityLevel}");

        if (IsOwner && !uiInitialized)
        {
            uiInitialized = true;
            StartCoroutine(SetupWarpUIAsync());
        }
    }

    public override void OnNetworkDespawn()
    {
        CleanupWarpSelection();
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        CleanupWarpSelection();
    }

    private void CleanupWarpSelection()
    {
        if (selectionCoroutine != null)
        {
            StopCoroutine(selectionCoroutine);
            selectionCoroutine = null;
        }

        isSelectingLocation = false;

        if (mapPanelInstance != null)
        {
            mapPanelInstance.SetActive(false);
        }

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public float GetRemainingCooldown()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            double nowServerTime = NetworkManager.Singleton.ServerTime.Time;
            float remainingNet = (float)(cooldownEndServerTime.Value - nowServerTime);
            if (remainingNet > 0f)
            {
                return remainingNet;
            }
        }

        if (IsServer)
        {
            float remainingServer = (lastUseTime + cooldownTime) - Time.time;
            return remainingServer > 0f ? remainingServer : 0f;
        }

        if (clientCooldownEndTime > 0f)
        {
            float remainingClient = clientCooldownEndTime - Time.time;
            return remainingClient > 0f ? remainingClient : 0f;
        }

        return 0f;
    }

    private IEnumerator SetupWarpUIAsync()
    {
        yield return new WaitForEndOfFrame();

        if (mapPanelInstance != null)
        {
            mapPanelInstance.SetActive(false);
            yield break;
        }

        GameObject canvasObj = new GameObject($"WarpCanvas_{OwnerClientId}");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject($"WarpMapPanel_{OwnerClientId}");
        panelObj.transform.SetParent(canvasObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        Sprite mapSprite = Resources.Load<Sprite>("MapImage");
        if (mapSprite != null)
        {
            panelImage.sprite = mapSprite;
        }
        else
        {
            panelImage.color = new Color(0f, 0.5f, 0f, 0.8f);
        }

        RectTransform rectTransform = panelObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(500, 500);
        rectTransform.anchoredPosition = Vector2.zero;

        mapPanelInstance = panelObj;
        mapPanelInstance.SetActive(false);

        Debug.Log($"[Warp] UI Map berhasil dibuat untuk OwnerClientId={OwnerClientId}. MapSpriteFound={mapSprite != null}");
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        if (isSelectingLocation && AbilityJammer.IsPlayerJammed(this, "Warp"))
        {
            CleanupWarpSelection();
            return;
        }

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Warp] Cooldown selesai! Ability Warp siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
            Debug.Log($"[Warp] Tombol {assignedKey} ditekan | isOnCooldown={isOnCooldown}, isSelectingLocation={isSelectingLocation}");

            if (AbilityJammer.IsPlayerJammed(this, "Warp"))
            {
                return;
            }

            if (isOnCooldown)
            {
                Debug.Log("[Warp] Skill masih cooldown!");
                return;
            }

            if (!isSelectingLocation)
            {
                selectionCoroutine = StartCoroutine(OpenMapSelectionRoutine());
            }
        }
    }

    private IEnumerator OpenMapSelectionRoutine()
    {
        if (mapPanelInstance == null)
        {
            yield return StartCoroutine(SetupWarpUIAsync());
        }

        isSelectingLocation = true;
        selectionTimer = selectionTimeLimit;

        if (mapPanelInstance != null) mapPanelInstance.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Vector3 selectedTargetPosition = Vector3.zero;
        bool hasChosenPosition = false;

        RectTransform mapRect = mapPanelInstance != null ? mapPanelInstance.GetComponent<RectTransform>() : null;

        Debug.Log("[Warp] Panel Map dibuka. Klik pada map dalam 3 detik.");

        while (selectionTimer > 0f)
        {
            if (!IsSpawned || AbilityJammer.IsPlayerJammed(this, "Warp"))
            {
                break;
            }

            selectionTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && mapRect != null)
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out localMousePos))
                {
                    float rectWidth = Mathf.Max(1f, mapRect.rect.width);
                    float rectHeight = Mathf.Max(1f, mapRect.rect.height);

                    float normalizedX = Mathf.Clamp01((localMousePos.x / rectWidth) + 0.5f);
                    float normalizedY = Mathf.Clamp01((localMousePos.y / rectHeight) + 0.5f);

                    float targetX = Mathf.Lerp(mazeMinX, mazeMaxX, normalizedX);
                    float targetZ = Mathf.Lerp(mazeMinZ, mazeMaxZ, normalizedY);
                    float targetY = transform.position.y;

                    selectedTargetPosition = new Vector3(targetX, targetY, targetZ);
                    hasChosenPosition = true;

                    Debug.Log($"[Warp] Koordinat dipilih: {selectedTargetPosition} (norm: {normalizedX:0.00}, {normalizedY:0.00})");
                    break;
                }
            }

            yield return null;
        }

        if (mapPanelInstance != null) mapPanelInstance.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (hasChosenPosition)
        {
            TryWarpServerRpc(selectedTargetPosition);
        }
        else
        {
            SetCooldownLocalAndServer(cooldownTime);
            Debug.Log("[Warp] Gagal memilih tempat dalam 3 detik! Skill dibatalkan dan masuk cooldown.");
        }

        isSelectingLocation = false;
        selectionCoroutine = null;
    }

    private void SetCooldownLocalAndServer(float duration)
    {
        clientCooldownEndTime = Time.time + duration;
        wasOnCooldown = true;

        if (IsServer)
        {
            lastUseTime = Time.time;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + duration;
            }
            TriggerCooldownClientRpc(duration);
        }
    }

    [ServerRpc]
    private void TryWarpServerRpc(Vector3 targetPosition)
    {
        if (!IsSpawned) return;

        Debug.Log($"[Warp] TryWarpServerRpc diterima di Server untuk OwnerClientId={OwnerClientId}, target={targetPosition}");

        if (AbilityJammer.IsPlayerJammed(this, "Warp"))
        {
            return;
        }

        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxWarpClamp, maxWarpClamp);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -maxWarpClamp, maxWarpClamp);

        Vector3 rayOrigin = new Vector3(targetPosition.x, targetPosition.y + rayStartHeight, targetPosition.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, rayDistance))
        {
            targetPosition = groundHit.point + Vector3.up * 0.1f;
        }
        else
        {
            Debug.LogWarning($"[Warp] Target berada di luar area lantai valid! RayOrigin={rayOrigin}, Warp dibatalkan.");
            return;
        }

        CharacterController serverCharController = GetComponent<CharacterController>();
        if (serverCharController != null) serverCharController.enabled = false;
        transform.position = targetPosition;
        if (serverCharController != null) serverCharController.enabled = true;

        ApplyWarpPositionClientRpc(targetPosition);

        lastUseTime = Time.time;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            cooldownEndServerTime.Value = NetworkManager.Singleton.ServerTime.Time + cooldownTime;
        }

        TriggerCooldownClientRpc(cooldownTime);
        Debug.Log($"[Warp Lv.{abilityLevel}] Berhasil warp ke koordinat aman: {targetPosition}");
    }

    [ClientRpc]
    private void ApplyWarpPositionClientRpc(Vector3 finalPosition)
    {
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null) charController.enabled = false;

        transform.position = finalPosition;

        if (charController != null) charController.enabled = true;
    }

    [ClientRpc]
    private void TriggerCooldownClientRpc(float duration)
    {
        if (IsOwner)
        {
            clientCooldownEndTime = Time.time + duration;
            wasOnCooldown = true;
        }
    }
}