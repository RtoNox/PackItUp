using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class DuckKunUIController : MonoBehaviour
{
    public static DuckKunUIController Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject targetSelectionPanel;
    public Button[] playerButtons;
    public TMP_Text[] buttonTexts;

    private AbilityDuckKun ownerAbility;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(false);
        }
    }

    public void Init(AbilityDuckKun ability)
    {
        ownerAbility = ability;
    }

    public void OpenPanel()
    {
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(true);
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            RefreshPlayerList();
        }
    }

    public void ClosePanel()
    {
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(false);
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void RefreshPlayerList()
    {
        var spawnedObjects = NetworkManager.Singleton.SpawnManager.SpawnedObjects;
        
        int index = 0;
        foreach (var kvp in spawnedObjects)
        {
            NetworkObject netObj = kvp.Value;
            
            if (netObj.GetComponent<AbilityDuckKun>() != null)
            {
                if (index < playerButtons.Length)
                {
                    playerButtons[index].gameObject.SetActive(true);
                    
                    if (index < buttonTexts.Length && buttonTexts[index] != null)
                    {
                        PlayerName pName = netObj.GetComponent<PlayerName>();
                        if (pName != null && !string.IsNullOrEmpty(pName.PlayerNameValue))
                        {
                            buttonTexts[index].text = pName.PlayerNameValue;
                        }
                        else
                        {
                            buttonTexts[index].text = "Player " + netObj.OwnerClientId;
                        }
                    }

                    ulong targetId = netObj.NetworkObjectId;
                    playerButtons[index].onClick.RemoveAllListeners();
                    playerButtons[index].onClick.AddListener(() => OnPlayerButtonClicked(targetId));

                    index++;
                }
            }
        }

        for (int i = index; i < playerButtons.Length; i++)
        {
            if (playerButtons[i] != null)
            {
                playerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnPlayerButtonClicked(ulong targetNetworkObjectId)
    {
        if (ownerAbility != null)
        {
            ownerAbility.OnTargetSelected(targetNetworkObjectId);
        }
        ClosePanel();
    }
}