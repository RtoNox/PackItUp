using TMPro;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI entryText;

    public void SetData(PlayerResult result)
    {
        entryText.text = $"{result.rank}. {result.playerName} - {result.points} Points";
    }
}