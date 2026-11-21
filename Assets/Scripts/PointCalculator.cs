using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class PointCalculator : MonoBehaviour
{
    public List<PlayerResult> CalculateRankings()
{
    var rankedPlayers = FindObjectsOfType<PlayerPoints>().OrderByDescending(p => p.Points.Value).ToArray();

    List<PlayerResult> results = new();
    int currentRank = 1;

    for (int i = 0; i < rankedPlayers.Length; i++)
    {
        if (i > 0 && rankedPlayers[i].Points.Value < rankedPlayers[i - 1].Points.Value)
            currentRank = i + 1;

        var nameScript = rankedPlayers[i].GetComponent<PlayerName>();

        results.Add(new PlayerResult
        {
            clientId = rankedPlayers[i].OwnerClientId,
            playerName = nameScript.PlayerNameValue,
            points = rankedPlayers[i].Points.Value,
            rank = currentRank
        });
    }

    return results;
}
}

public class PlayerResult
{
    public ulong clientId;
    public string playerName;
    public int points;
    public int rank;
}
