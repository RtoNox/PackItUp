using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class AbilityWarp : NetworkBehaviour
{
    [Header("Ability Stats")]
    public float cooldownTime = 45f;
    public float selectionTimeLimit = 3f;
    private float lastUseTime = -999f;

    [Header("Input Settings")]
    public KeyCode assignedKey = KeyCode.F;

    [Header("Maze World Boundaries (Sesuaikan dengan tembok terluar asli: -50 sampai 50)")]
    public float mazeMinX = -50f;
    public float mazeMaxX = 50f;
    public float mazeMinZ = -50f;
    public float mazeMaxZ = 50f;

    private GameObject mapPanelInstance;
    private bool isSelectingLocation = false;
    private float selectionTimer = 0f;

    void Start()
    {
        if (!IsOwner) return;

        StartCoroutine(SetupWarpUIAsync());
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
            Debug.LogWarning("[Warp] Sprite 'MapImage' tidak ditemukan di folder Resources!");
        }

        RectTransform rectTransform = panelObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(500, 500);
        rectTransform.anchoredPosition = Vector2.zero;

        mapPanelInstance = panelObj;
        mapPanelInstance.SetActive(false);
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(assignedKey))
        {
            if (Time.time < lastUseTime + cooldownTime)
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
        isSelectingLocation = true;
        selectionTimer = selectionTimeLimit;

        if (mapPanelInstance != null) mapPanelInstance.SetActive(true);

        // Bebaskan kursor mouse agar bisa dipakai memilih peta
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[Warp] Panel map terbuka. Silakan klik area peta tujuan dalam 3 detik...");

        Vector3 selectedTargetPosition = Vector3.zero;
        bool hasChosenPosition = false;

        RectTransform mapRect = mapPanelInstance.GetComponent<RectTransform>();

        while (selectionTimer > 0f)
        {
            selectionTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0))
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
            lastUseTime = Time.time;
            Debug.Log("[Warp] Gagal memilih tempat dalam 3 detik! Skill dibatalkan dan masuk cooldown.");
        }

        isSelectingLocation = false;
    }

    [ServerRpc]
    private void TryWarpServerRpc(Vector3 targetPosition)
    {
        float maxBoundary = 45f;
        targetPosition.x = Mathf.Clamp(targetPosition.x, -maxBoundary, maxBoundary);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -maxBoundary, maxBoundary);

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

        CharacterController charController = GetComponent<CharacterController>();

        if (charController != null) charController.enabled = false;

        transform.position = targetPosition;

        if (charController != null) charController.enabled = true;

        lastUseTime = Time.time;
        Debug.Log($"[Warp] Berhasil warp ke koordinat aman: {targetPosition}");
    }
}