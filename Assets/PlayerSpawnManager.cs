using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Header("Doğum Noktaları")]
    public Transform[] spawnPoints;

    private int currentSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return Vector3.zero; // Eğer pozisyon atanmadıysa 0,0,0 döner
        }

        Vector3 selectedPos = spawnPoints[currentSpawnIndex % spawnPoints.Length].position;
        currentSpawnIndex++;
        return selectedPos;
    }
}