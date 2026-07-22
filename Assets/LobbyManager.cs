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

    // Listeyi hemen burada başlatıyoruz ki boşta kalmasın
    public NetworkList<PlayerLobbyState> lobbyPlayers = new NetworkList<PlayerLobbyState>();

    private void Awake()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyButtonClicked);
            Debug.Log("Butona listener başarıyla eklendi.");
        }
        else
        {
            Debug.LogError("readyButton referansı boş!");
        }
    }

    public override void OnNetworkSpawn()
    {
        lobbyPlayers.OnListChanged += UpdateLobbyUI;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
            {
                if (id == NetworkManager.Singleton.LocalClientId) return;
                lobbyPlayers.Add(new PlayerLobbyState { ClientId = id, IsReady = false, PlayerName = "Player " + id });
            };

            lobbyPlayers.Add(new PlayerLobbyState { ClientId = NetworkManager.Singleton.LocalClientId, IsReady = false, PlayerName = "Host" });
        }

        UpdateLobbyUI(default);
    }

    public void OnReadyButtonClicked()
    {
        Debug.Log("Butona tıklandı! Gönderilen ClientID: " + NetworkManager.Singleton.LocalClientId);
        ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ToggleReadyServerRpc(ulong clientId)
    {
        Debug.Log("ServerRpc çalıştı! Gelen ClientID: " + clientId);

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            Debug.Log($"Liste taranıyor... Liste elemanı ID: {lobbyPlayers[i].ClientId}");
            if (lobbyPlayers[i].ClientId == clientId)
            {
                var playerInfo = lobbyPlayers[i];
                playerInfo.IsReady = !playerInfo.IsReady;
                lobbyPlayers[i] = playerInfo;
                Debug.Log("Oyuncu durumu değiştirildi. Yeni Durum: " + playerInfo.IsReady);
                break;
            }
        }
    }

    void UpdateLobbyUI(NetworkListEvent<PlayerLobbyState> changeEvent)
    {
        for (int i = 0; i < 4; i++)
        {
            playerNameTexts[i].text = "BOŞ SLOT";
            playerReadyTexts[i].text = "";
            playerReadyTexts[i].color = Color.gray;
        }

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (i >= 4) break;

            playerNameTexts[i].text = lobbyPlayers[i].PlayerName.ToString();

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

            if (lobbyPlayers[i].ClientId == NetworkManager.Singleton.LocalClientId)
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