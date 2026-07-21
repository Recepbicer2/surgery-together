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
            Debug.LogWarning("PlayerSpawnManager: Hiç spawn noktası atanmamış! Vector3.zero dönüyor.");
            return Vector3.zero;
        }

        // Seçilen index'teki noktayı al
        Transform targetSpawn = spawnPoints[currentSpawnIndex % spawnPoints.Length];

        // Sıradaki oyuncu için index'i arttır
        currentSpawnIndex++;

        // Eğer Inspector'da eleman boş bırakılmadıysa pozisyonu döndür
        if (targetSpawn != null)
        {
            return targetSpawn.position;
        }

        return Vector3.zero;
    }
}