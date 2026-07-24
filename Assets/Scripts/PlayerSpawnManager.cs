using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [Header("Lobi Ayarları")]
    public Transform lobbySpawnPoint; // Sahnede yarattığın Lobby_SpawnPos'u buraya sürükle

    [Header("Oyun İçi Doğum Noktaları")]
    public Transform[] gameSpawnPoints; // Oyun içindeki asıl doğma noktalarını (spawnpos_1, vb.) buraya at

    private int currentSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Oyuncuyu lobiye göndermek için koordinat verir
    public Vector3 GetLobbySpawnPosition()
    {
        if (lobbySpawnPoint != null)
        {
            Debug.Log("LOBİ KOORDİNATI GÖNDERİLİYOR: " + lobbySpawnPoint.position);
            return lobbySpawnPoint.position + new Vector3(0, 2f, 0);
        }

        Debug.LogError("KRİTİK HATA: lobbySpawnPoint boş dönüyor!");
        return Vector3.zero;
    }

    // Oyuncuyu asıl oyuna ışınlarken sıradaki noktayı verir
    public Vector3 GetNextGameSpawnPosition()
    {
        if (gameSpawnPoints == null || gameSpawnPoints.Length == 0)
        {
            Debug.LogWarning("PlayerSpawnManager: Hiç oyun içi spawn noktası atanmamış!");
            return Vector3.zero;
        }

        Transform targetSpawn = gameSpawnPoints[currentSpawnIndex % gameSpawnPoints.Length];
        currentSpawnIndex++;

        if (targetSpawn != null)
            return targetSpawn.position;

        return Vector3.zero;
    }
}