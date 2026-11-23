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

    private GameObject mapPanelInstance;
    private bool isSelectingLocation = false;
    private float selectionTimer = 0f;
    private bool uiInitialized = false;

    public string AbilityName => abilityName;
    public Sprite AbilityIcon => abilityIcon;
    public float CooldownDuration => cooldownTime;
    public KeyCode AssignedKey => assignedKey;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        abilityLevel = Mathf.Clamp(AbilityDataHolder.GetLevel(abilityName), 1, maxAbilityLevel);

        if (IsOwner && !uiInitialized)
        {
            uiInitialized = true;
            StartCoroutine(SetupWarpUIAsync());
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

        GameObject existingPanel = GameObject.Find("WarpMapPanel");
        if (existingPanel != null)
        {
            mapPanelInstance = existingPanel;
            mapPanelInstance.SetActive(false);
            yield break;
        }

        GameObject canvasObj = new GameObject("WarpCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("WarpMapPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        Sprite mapSprite = Resources.Load<Sprite>("MapImage");
        if (mapSprite != null)
        {
            panelImage.sprite = mapSprite;
        }
        else
        {
            panelImage.color = new Color(0, 0.5f, 0, 0.8f);
        }

        RectTransform rectTransform = panelObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(500, 500);
        rectTransform.anchoredPosition = Vector2.zero;

        mapPanelInstance = panelObj;
        mapPanelInstance.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner || !IsSpawned) return;

        bool isOnCooldown = GetRemainingCooldown() > 0f;

        if (wasOnCooldown && !isOnCooldown)
        {
            Debug.Log("[Warp] Cooldown selesai! Ability Warp siap dipakai kembali.");
        }
        wasOnCooldown = isOnCooldown;

        if (Input.GetKeyDown(assignedKey))
        {
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
                StartCoroutine(OpenMapSelectionRoutine());
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

        while (selectionTimer > 0f)
        {
            selectionTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && mapRect != null)
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out localMousePos))
                {
                    float rectWidth = mapRect.rect.width;
                    float rectHeight = mapRect.rect.height;

                    float normalizedX = (localMousePos.x / rectWidth) + 0.5f;
                    float normalizedY = (localMousePos.y / rectHeight) + 0.5f;

                    normalizedX = Mathf.Clamp01(normalizedX);
                    normalizedY = Mathf.Clamp01(normalizedY);

                    float targetX = Mathf.Lerp(mazeMinX, mazeMaxX, normalizedX);
                    float targetZ = Mathf.Lerp(mazeMinZ, mazeMaxZ, normalizedY);
                    float targetY = transform.position.y;

                    selectedTargetPosition = new Vector3(targetX, targetY, targetZ);
                    hasChosenPosition = true;
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

        if (AbilityJammer.IsPlayerJammed(this, "Warp"))
        {
            return;
        }

        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxWarpClamp, maxWarpClamp);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -maxWarpClamp, maxWarpClamp);

        Vector3 rayOrigin = new Vector3(targetPosition.x, 20f, targetPosition.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 40f))
        {
            targetPosition = groundHit.point + Vector3.up * 0.1f;
        }
        else
        {
            Debug.LogWarning($"[Warp] Target berada di luar area lantai valid! Warp dibatalkan.");
            return;
        }

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