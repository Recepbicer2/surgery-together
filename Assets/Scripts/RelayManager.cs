using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay; // Dashboard paketi üzerinden doğrudan erişim
using Unity.Services.Relay.Models;
using UnityEngine;

// Çakışmayı önlemek için Relay paketini açıkça tanımlıyoruz:
using RelayService = Unity.Services.Relay.RelayService;
using RelayServiceException = Unity.Services.Relay.RelayServiceException;
using Allocation = Unity.Services.Relay.Models.Allocation;
using JoinAllocation = Unity.Services.Relay.Models.JoinAllocation;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        // Unity Services ve Anonim Giriş Başlatma
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Unity Services Giriş Yapıldı. Player ID: {AuthenticationService.Instance.PlayerId}");
        }
    }

    // HOST: Oda Oluştur ve Join Code Döndür
    public async Task<string> CreateRelay(int maxPlayers = 4)
    {
        try
        {
            // En fazla (maxPlayers - 1) kadar istemciye izin ver (Host hariç)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // NetworkManager üzerindeki Transport verilerini güncelle
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // Host olarak oyunu başlat
            NetworkManager.Singleton.StartHost();
            Debug.Log($"Relay Odası Oluşturuldu! Kod: {joinCode}");

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Oluşturma Hatası: {e.Message}");
            return null;
        }
    }

    // CLIENT: Katılım Kodu ile Odaya Bağlan
    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // NetworkManager üzerindeki Transport verilerini güncelle
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            // Client olarak oyunu başlat
            NetworkManager.Singleton.StartClient();
            Debug.Log($"Relay Odasına Katılındı: {joinCode}");

            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay Katılım Hatası: {e.Message}");
            return false;
        }
    }
}