using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PositionTracker : MonoBehaviour
{
    public Transform player;
    public Transform[] floors; // Array of floor transforms

    private int currentFloor = 0;

    void Update()
    {
        UpdatePlayerPosition();
    }

    void UpdatePlayerPosition()
    {
        Vector3 playerPos = player.position;

        // Update current floor
        for (int i = 0; i < floors.Length; i++)
        {
            if (playerPos.y > floors[i].position.y)
            {
                currentFloor = i;
            }
            else
            {
                break;
            }
        }

        // Log position (you can modify this to suit your needs)
        UnityEngine.Debug.Log($"Player is on floor {currentFloor + 1} at position: {playerPos}");
    }
}