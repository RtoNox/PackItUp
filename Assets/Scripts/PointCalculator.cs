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
        PlayerPoints[] players = FindObjectsOfType<PlayerPoints>();

        var rankedPlayers = players.OrderByDescending(p => p.Points.Value).ToArray();

        for (int i = 0; i < rankedPlayers.Length; i++)
        {
            int rank = i + 1;
        }

        List<PlayerResult> results = new();

        for (int i = 0; i < rankedPlayers.Length; i++)
        {
        results.Add(new PlayerResult
         {
            clientId = rankedPlayers[i].OwnerClientId,
            points = rankedPlayers[i].Points.Value,
            rank = i + 1
         });
        }

        int currentRank = 1;

        for (int i = 0; i < rankedPlayers.Length; i++)
        {
            if (i > 0 && rankedPlayers[i].Points.Value < rankedPlayers[i - 1].Points.Value)
         {
                currentRank = i + 1;
         }
        }

        return results;
     }
}

public class PlayerResult
{
    public ulong clientId;
    public int points;
    public int rank;
}
