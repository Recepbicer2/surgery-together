using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using Unity.Collections;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI Referansları")]
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;

    public TextMeshProUGUI[] playerNameTexts;
    public TextMeshProUGUI[] playerReadyTexts;

    public NetworkList<PlayerLobbyState> lobbyPlayers = new NetworkList<PlayerLobbyState>();

    // NETCODE ZAMANLAMA BUG'INI AŞMAK İÇİN GÜVENLİK BAYRAĞI
    private bool _uiGuncellenecek = false;

    // DİKKAT: Awake() metodunu tamamen sildik çünkü butonu zaten Inspector'dan bağlamışsın! Çift tıklamayı engelledik.

    public override void OnNetworkSpawn()
    {
        // Liste değiştiğinde UI'ı hemen yormuyoruz, sadece "güncelleme lazım" diye not alıyoruz.
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

        _uiGuncellenecek = true; // Oyuna girince UI ilk kez çizilsin
    }

    private void Update()
    {
        if (!IsSpawned) return;

        // Sadece ve sadece listede bir değişiklik olmuşsa UI güncellenir.
        if (_uiGuncellenecek)
        {
            UpdateLobbyUI();
            _uiGuncellenecek = false; // İşlem bitince bayrağı indir.
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

                // Üzerine yazınca OnListChanged otomatik tetiklenir ve bayrağı kaldırır.
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