using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class VoiceManager : NetworkBehaviour
{
    public static VoiceManager Instance { get; private set; }

    [Header("Vivox Ayarları")]
    public string channelName = "Game3DVoiceChannel";
    private bool isInChannel = false;

    private void Awake()
    {
        // Singleton yapısı
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Sadece kendi karakterimiz doğduğunda Vivox'a bağlanıyoruz
        if (IsOwner)
        {
            await InitializeAndJoinVivoxAsync();
        }
    }

    private async Task InitializeAndJoinVivoxAsync()
    {
        try
        {
            // 1. Giriş kontrolü ve login
            if (!VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.InitializeAsync();
                await VivoxService.Instance.LoginAsync();
                Debug.Log("Vivox Giriş Yapıldı.");
            }

            // 2. Kanala katılma kontrolü
            if (!isInChannel)
            {
                // 3D Ses Konfigürasyonu (Duyulma Mesafesi vs.)
                Channel3DProperties positionalOptions = new Channel3DProperties(15, 2, 1.0f, AudioFadeModel.InverseByDistance);

                // En sade haliyle 3D kanala katılım işlemi
                await VivoxService.Instance.JoinPositionalChannelAsync(
                    channelName,
                    ChatCapability.TextAndAudio,
                    positionalOptions
                );

                isInChannel = true;
                Debug.Log("Vivox 3D Sesli Sohbet Başarıyla Bağlandı!");
            }

            MuteMicrophone();
        }
        catch (Exception ex)
        {
            // Olası bir hatada çökmesin, sadece konsola uyarı yazsın
            Debug.LogWarning($"Vivox Bağlantı Uyarısı (Oyun Akışını Etkilemez): {ex.Message}");
        }
    }

    public void MuteMicrophone()
    {
        try
        {
            VivoxService.Instance.MuteInputDevice();
            Debug.Log("Mikrofon Kapatıldı");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Mikrofon kapatılırken hata oluştu: " + e.Message);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CleanupVivox();
    }

    private async void CleanupVivox()
    {
        if (IsOwner && VivoxService.Instance != null)
        {
            try
            {
                if (isInChannel)
                {
                    await VivoxService.Instance.LeaveChannelAsync(channelName);
                    isInChannel = false;
                }

                if (VivoxService.Instance.IsLoggedIn)
                {
                    await VivoxService.Instance.LogoutAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Vivox temizlenirken uyarı: " + ex.Message);
            }
        }
    }

    // Editörde Play modundan çıkıldığında oturumu kesin kapatması için
    private async void OnApplicationQuit()
    {
        try
        {
            if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.LogoutAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Uygulama kapanırken Vivox çıkış hatası: " + ex.Message);
        }
    }
}