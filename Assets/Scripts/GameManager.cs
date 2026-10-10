using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


public class GameManager : MonoBehaviour
{
    private float roundTime = 180f;

    private void Update()
    {
        roundTime -= Time.deltaTime;

        if(roundTime <= 0)
        {
            FinishRound();
        }
    }

    private void FinishRound()
    {
    finishPanel.SetActive(true);
    }
}
