using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI Referansları")]
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;

    public TextMeshProUGUI[] playerNameTexts;
    public TextMeshProUGUI[] playerReadyTexts;

    [Header("Geçiş ve Yükleme Ekranı")]
    public TextMeshProUGUI countdownText;
    public GameObject loadingScreenCanvas;

    public NetworkList<PlayerLobbyState> lobbyPlayers = new NetworkList<PlayerLobbyState>();

    private bool _uiGuncellenecek = false;
    private bool _isGameStarted = false;

    public override void OnNetworkSpawn()
    {
        lobbyPlayers.OnListChanged += (changeEvent) => { _uiGuncellenecek = true; };

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
            {
                if (id == NetworkManager.Singleton.LocalClientId) return;
                lobbyPlayers.Add(new PlayerLobbyState { ClientId = id, IsReady = false, PlayerName = "Player " + id });
            };

            lobbyPlayers.Add(new PlayerLobbyState { ClientId = NetworkManager.Singleton.LocalClientId, IsReady = false, PlayerName = "Host" });
        }

        _uiGuncellenecek = true;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (_uiGuncellenecek)
        {
            UpdateLobbyUI();
            CheckIfAllPlayersReady();
            _uiGuncellenecek = false;
        }
    }

    public void OnReadyButtonClicked()
    {
        Debug.Log("-> BUTONA TIKLANDI! Sunucuya hazır olma isteği gönderiliyor...");
        ToggleReadyRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ToggleReadyRpc(ulong clientId)
    {
        Debug.Log("-> SUNUCU RPC ÇALIŞTI. İstek Yapan ClientID: " + clientId);

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                var playerInfo = lobbyPlayers[i];
                playerInfo.IsReady = !playerInfo.IsReady;

                lobbyPlayers[i] = playerInfo;

                Debug.Log($"-> DURUM DEĞİŞTİ! {playerInfo.PlayerName} artık hazır mı?: {playerInfo.IsReady}");
                break;
            }
        }
    }

    void UpdateLobbyUI()
    {
        Debug.Log("-> UI EKRANI GÜNCELLENİYOR...");

        for (int i = 0; i < 4; i++)
        {
            if (i < playerNameTexts.Length && playerNameTexts[i] != null)
                playerNameTexts[i].text = "BOŞ SLOT";

            if (i < playerReadyTexts.Length && playerReadyTexts[i] != null)
            {
                playerReadyTexts[i].text = "";
                playerReadyTexts[i].color = Color.gray;
            }
        }

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (i >= 4) break;

            if (i < playerNameTexts.Length && playerNameTexts[i] != null)
                playerNameTexts[i].text = lobbyPlayers[i].PlayerName.ToString();

            if (i < playerReadyTexts.Length && playerReadyTexts[i] != null)
            {
                if (lobbyPlayers[i].IsReady)
                {
                    playerReadyTexts[i].text = "READY";
                    playerReadyTexts[i].color = Color.green;
                }
                else
                {
                    playerReadyTexts[i].text = "NOT READY";
                    playerReadyTexts[i].color = Color.red;
                }
            }

            if (lobbyPlayers[i].ClientId == NetworkManager.Singleton.LocalClientId && readyButtonText != null)
            {
                readyButtonText.text = lobbyPlayers[i].IsReady ? "CANCEL" : "READY";
            }
        }
    }

    private void CheckIfAllPlayersReady()
    {
        if (!IsServer || _isGameStarted || lobbyPlayers.Count == 0) return;

        bool allReady = true;
        foreach (var player in lobbyPlayers)
        {
            if (!player.IsReady)
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            Debug.Log("-> HERKES HAZIR! Geçiş başlatılıyor...");
            _isGameStarted = true;
            StartCoroutine(LobbyCountdownRoutine());
        }
    }

    private IEnumerator LobbyCountdownRoutine()
    {
        // 1. Geri sayım başlamadan önce yükleme ekranını/paneli açıyoruz
        ShowLoadingScreenClientRpc();

        // 2. 5 saniyelik geri sayım döngüsü
        for (int i = 5; i > 0; i--)
        {
            UpdateCountdownUIClientRpc(i.ToString());
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(1.5f);

        // 3. Karakterleri oyun alanına ışınlıyoruz (TRY-CATCH EKLENDİ)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            try
            {
                if (client.PlayerObject != null)
                {
                    Vector3 newPos = PlayerSpawnManager.Instance.GetNextGameSpawnPosition();

                    // YERE VE DUVARA SIKIŞMAMAK İÇİN 2 METRE YUKARI PAY EKLİYORUZ
                    newPos += new Vector3(0, 2f, 0);

                    var charController = client.PlayerObject.GetComponent<CharacterController>();
                    if (charController != null) charController.enabled = false;

                    if (client.PlayerObject.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var netTransform))
                    {
                        netTransform.Teleport(newPos, client.PlayerObject.transform.rotation, client.PlayerObject.transform.localScale);
                    }
                    else
                    {
                        client.PlayerObject.transform.position = newPos;
                    }

                    if (charController != null) charController.enabled = true;
                }
            }
            catch (System.Exception e)
            {
                // Eğer arkadaşın bağlanırken bir hata olursa oyun çökmeyecek, hatayı konsola yazdıracak!
                Debug.LogError($"İstemci ışınlanırken hata oluştu (ClientID {client.ClientId}): {e.Message}");
            }
        }

        yield return new WaitForSeconds(1f);

        // 4. Hata olsa bile yükleme ekranı ARTIK KAPANACAK!
        HideLoadingScreenClientRpc();
    }
    [ClientRpc]
    private void UpdateCountdownUIClientRpc(string timeText)
    {
        if (countdownText != null)
        {
            countdownText.text = "Oyuna Aktarılıyor: " + timeText;
        }
    }

    [ClientRpc]
    private void ShowLoadingScreenClientRpc()
    {
        if (loadingScreenCanvas != null)
            loadingScreenCanvas.SetActive(true);
    }

    [ClientRpc]
    private void HideLoadingScreenClientRpc()
    {
        if (loadingScreenCanvas != null)
            loadingScreenCanvas.SetActive(false);

        if (countdownText != null)
            countdownText.text = "";
    }
}

public struct PlayerLobbyState : INetworkSerializable, System.IEquatable<PlayerLobbyState>
{
    public ulong ClientId;
    public bool IsReady;
    public FixedString32Bytes PlayerName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref PlayerName);
    }

    public bool Equals(PlayerLobbyState other)
    {
        return ClientId == other.ClientId && IsReady == other.IsReady && PlayerName == other.PlayerName;
    }
}