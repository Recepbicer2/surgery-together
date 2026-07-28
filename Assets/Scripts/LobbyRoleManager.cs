using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;

public struct PlayerLobbyData : INetworkSerializable, System.IEquatable<PlayerLobbyData>
{
    public ulong clientId;
    public FixedString32Bytes playerName;
    public FixedString32Bytes selectedRole;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref selectedRole);
    }

    public bool Equals(PlayerLobbyData other)
    {
        return clientId == other.clientId && selectedRole.Equals(other.selectedRole) && playerName.Equals(other.playerName);
    }
}

public class LobbyRoleManager : NetworkBehaviour
{
    public static LobbyRoleManager Instance;

    [Header("UI Referansları")]
    public GameObject lobbyPanel;
    public TMP_Dropdown levelDropdown;
    public Transform playerSlotContainer;
    public GameObject playerSlotUIPrefab;
    public Button startGameButton;

    public NetworkList<PlayerLobbyData> lobbyPlayers;
    public NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        lobbyPlayers = new NetworkList<PlayerLobbyData>();
    }

    public override void OnNetworkSpawn()
    {
        lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;
        isGameStarted.OnValueChanged += OnGameStartedChanged;

        if (IsServer)
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                AddPlayerToLobby(clientId, "Oyuncu " + clientId);
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // BAŞLANGIÇTA: Lobi paneli kapalı başlasın (Oda Kur / Katıl butonuna basana kadar görünmesin)
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(false);
        }

        UpdateLobbyUI();

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(IsServer);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
    }

    public override void OnNetworkDespawn()
    {
        lobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;
        isGameStarted.OnValueChanged -= OnGameStartedChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void Update()
    {
        // KİRİŞ KİLİDİ: Lobi paneli açık olduğu sürece imlecin başka bir script tarafından 
        // gizlenmesine veya kilitlenmesine izin vermiyoruz! Sürekli görünür ve serbest kalır.
        if (lobbyPanel != null && lobbyPanel.activeSelf && !isGameStarted.Value)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        AddPlayerToLobby(clientId, "Oyuncu " + clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        RemovePlayerFromLobby(clientId);
    }

    private void AddPlayerToLobby(ulong id, string name)
    {
        if (!IsServer) return;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == id) return;
        }
        lobbyPlayers.Add(new PlayerLobbyData { clientId = id, playerName = name, selectedRole = "Seçilmedi" });
    }

    private void RemovePlayerFromLobby(ulong id)
    {
        if (!IsServer) return;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == id)
            {
                lobbyPlayers.RemoveAt(i);
                break;
            }
        }
    }

    // --- BUTONLARA BAĞLANACAK FONKSİYONLAR ---

    // 1. "Oda Kur" veya "Oyuna Katıl" butonuna basıldığında bu çağrılacak ve lobi panelini açacak
    public void OpenLobbyPanel()
    {
        Debug.Log("OpenLobbyPanel butonuna tıklandı!");

        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("Lobby Panel başarıyla aktif edildi.");
        }
        else
        {
            Debug.LogError("HATA: Lobby Panel referansı boş (None) bırakılmış!");
        }
    }

    public void SelectRoleDoktor() => RequestChangeRoleServerRpc("Doktor");
    public void SelectRoleDepocu() => RequestChangeRoleServerRpc("Depocu");
    public void SelectRoleTamirci() => RequestChangeRoleServerRpc("Tamirci");

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestChangeRoleServerRpc(string newRole, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == senderId)
            {
                var player = lobbyPlayers[i];
                player.selectedRole = newRole;
                lobbyPlayers[i] = player;
                break;
            }
        }
    }

    private void OnStartGameClicked()
    {
        if (IsServer)
        {
            isGameStarted.Value = true;
        }
    }

    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            StartGameSequence();
        }
    }

    private void StartGameSequence()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);

        // Oyun başladı, imleci kilitle ve gizle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Oyun resmi olarak başladı! Sistemler aktif.");
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<PlayerLobbyData> changeEvent)
    {
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        if (playerSlotContainer == null) return;
        foreach (Transform child in playerSlotContainer) Destroy(child.gameObject);

        foreach (var player in lobbyPlayers)
        {
            if (playerSlotUIPrefab != null)
            {
                GameObject slot = Instantiate(playerSlotUIPrefab, playerSlotContainer);
                TextMeshProUGUI slotText = slot.GetComponentInChildren<TextMeshProUGUI>();
                if (slotText != null)
                {
                    slotText.text = $"{player.playerName}\nRol: <color=yellow>{player.selectedRole}</color>";
                }
            }
        }
    }
}