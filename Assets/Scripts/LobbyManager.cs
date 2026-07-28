using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Collections;
using System.Collections;

public struct PlayerLobbyState : INetworkSerializable, System.IEquatable<PlayerLobbyState>
{
    public ulong ClientId;
    public bool IsReady;
    public FixedString32Bytes PlayerName;
    public FixedString32Bytes SelectedRole;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref SelectedRole);
    }

    public bool Equals(PlayerLobbyState other)
    {
        return ClientId == other.ClientId && IsReady == other.IsReady && PlayerName.Equals(other.PlayerName) && SelectedRole.Equals(other.SelectedRole);
    }
}

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Paneller & Arayüz")]
    public GameObject lobiBaglantiPanel;
    public GameObject beklemeOdasiPanel;
    public GameObject readyCanvas; // Ready butonu ve slotların olduğu canvas
    public TextMeshProUGUI odaKoduText;

    [Header("Seçilen Rol Göstergesi (Butonların Üstü İçin)")]
    public TextMeshProUGUI selectedRoleDisplayText;

    [Header("Sol Alt Slotlar (Sadece İsim ve Ready)")]
    public TextMeshProUGUI[] playerNameTexts;
    public TextMeshProUGUI[] playerReadyTexts;

    [Header("Geçiş ve Yükleme Ekranı")]
    public TextMeshProUGUI countdownText;
    public GameObject loadingScreenCanvas;

    public NetworkList<PlayerLobbyState> lobbyPlayers;

    private bool _uiGuncellenecek = false;
    private bool _isGameStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        lobbyPlayers = new NetworkList<PlayerLobbyState>();
    }

    public override void OnNetworkSpawn()
    {
        lobbyPlayers.OnListChanged += (changeEvent) => { _uiGuncellenecek = true; };

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
            {
                if (id == NetworkManager.Singleton.LocalClientId) return;
                lobbyPlayers.Add(new PlayerLobbyState
                {
                    ClientId = id,
                    IsReady = false,
                    PlayerName = new FixedString32Bytes("Player " + id),
                    SelectedRole = new FixedString32Bytes("Seçilmedi")
                });
            };

            lobbyPlayers.Add(new PlayerLobbyState
            {
                ClientId = NetworkManager.Singleton.LocalClientId,
                IsReady = false,
                PlayerName = new FixedString32Bytes("Host"),
                SelectedRole = new FixedString32Bytes("Seçilmedi")
            });
        }

        // KESİN ÇÖZÜM 1: Lobiye geçerken her şeyi başa saran sinsi kodlar BURADAN SİLİNDİ.
        // Artık odayı kurduğunda sistem canvaslarını kapatmayacak.

        _uiGuncellenecek = true;
    }

    public override void OnNetworkDespawn()
    {
        lobbyPlayers.OnListChanged -= (changeEvent) => { _uiGuncellenecek = true; };
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (!_isGameStarted)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // KESİN ÇÖZÜM 2: OTOMATİK SENKRONİZASYON (Auto-Sync) BEKÇİSİ
            // Eğer Bekleme Odası açıksa ama Ready Canvas kapalıysa, HİÇ SORMADAN ZORLA AÇ!
            if (beklemeOdasiPanel != null && readyCanvas != null)
            {
                if (beklemeOdasiPanel.activeSelf && !readyCanvas.activeSelf)
                {
                    readyCanvas.SetActive(true);
                }
            }
        }

        if (_uiGuncellenecek)
        {
            UpdateLobbyUI();
            CheckIfAllPlayersReady();
            _uiGuncellenecek = false;
        }
    }

    private void SetLobbyPanelsActive(bool isActive)
    {
        if (beklemeOdasiPanel != null) beklemeOdasiPanel.SetActive(isActive);
        if (readyCanvas != null) readyCanvas.SetActive(isActive);
    }

    public void OpenLobbyAndShowCode(string joinCode)
    {
        if (lobiBaglantiPanel != null) lobiBaglantiPanel.SetActive(false);
        SetLobbyPanelsActive(true);

        if (odaKoduText != null)
        {
            odaKoduText.text = "Oda Kodun: " + joinCode;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SelectRoleDoktor() => RequestChangeRoleServerRpc("Doktor");
    public void SelectRoleStorage() => RequestChangeRoleServerRpc("Storage");
    public void SelectRoleRepair() => RequestChangeRoleServerRpc("Repair");

    public void SelectRoleRandom()
    {
        string[] roles = { "Doktor", "Storage", "Repair" };
        string chosenRole = roles[Random.Range(0, roles.Length)];
        RequestChangeRoleServerRpc(chosenRole);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestChangeRoleServerRpc(string newRole, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == senderId)
            {
                var player = lobbyPlayers[i];
                player.SelectedRole = new FixedString32Bytes(newRole);
                lobbyPlayers[i] = player;
                break;
            }
        }
    }

    public void OnReadyButtonClicked()
    {
        ToggleReadyRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ToggleReadyRpc(ulong clientId)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].ClientId == clientId)
            {
                var playerInfo = lobbyPlayers[i];
                playerInfo.IsReady = !playerInfo.IsReady;
                lobbyPlayers[i] = playerInfo;
                break;
            }
        }
    }

    void UpdateLobbyUI()
    {
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
            {
                playerNameTexts[i].text = lobbyPlayers[i].PlayerName.ToString();
            }

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

            if (lobbyPlayers[i].ClientId == NetworkManager.Singleton.LocalClientId)
            {
                if (selectedRoleDisplayText != null)
                {
                    selectedRoleDisplayText.text = "Seçtiğin Rol: " + lobbyPlayers[i].SelectedRole.ToString();
                }
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
            _isGameStarted = true;
            StartCoroutine(LobbyCountdownRoutine());
        }
    }

    private IEnumerator LobbyCountdownRoutine()
    {
        SetLobbyPanelsActive(false);

        ShowLoadingScreenClientRpc();

        for (int i = 5; i > 0; i--)
        {
            UpdateCountdownUIClientRpc(i.ToString());
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(1.5f);

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            try
            {
                if (client.PlayerObject != null)
                {
                    Vector3 newPos = PlayerSpawnManager.Instance.GetNextGameSpawnPosition();
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
                Debug.LogError($"Işınlanma hatası: {e.Message}");
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yield return new WaitForSeconds(1f);
        HideLoadingScreenClientRpc();
    }

    [ClientRpc]
    private void UpdateCountdownUIClientRpc(string timeText)
    {
        if (countdownText != null)
            countdownText.text = "Oyuna Aktarılıyor: " + timeText;
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