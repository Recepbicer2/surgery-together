using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class VoiceManager : NetworkBehaviour
{
    [Header("Vivox Ayarları")]
    public string channelName = "Game3DVoiceChannel";
    private bool isInChannel = false;

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

                await VivoxService.Instance.JoinPositionalChannelAsync(
                    channelName,
                    ChatCapability.TextAndAudio,
                    positionalOptions
                );

                isInChannel = true;
                Debug.Log("Vivox 3D Sesli Sohbet Başarıyla Bağlandı!");
            }

            // Oyuna girince mikrofon kapalı başlasın
            MuteMicrophone();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Vivox Bağlantı Uyarısı (Oyun Akışını Etkilemez): {ex.Message}");
        }
    }

    void Update()
    {
        // Sadece kendi karakterimiz için pozisyon bildirip tuş okuyacağız
        if (!IsOwner) return;

        // 1. 3D SES POZİSYON GÜNCELLEMESİ (Vivox'un bizim nerede olduğumuzu bilmesi için şart)
        if (isInChannel)
        {
            VivoxService.Instance.Set3DPosition(transform.gameObject, channelName);
        }

        // 2. BAS-KONUŞ (Push to Talk) SİSTEMİ - V Tuşu
        if (Input.GetKeyDown(KeyCode.V))
        {
            UnmuteMicrophone();
        }
        else if (Input.GetKeyUp(KeyCode.V))
        {
            MuteMicrophone();
        }
    }

    public void MuteMicrophone()
    {
        try
        {
            VivoxService.Instance.MuteInputDevice();
            // Konsol spama girmesin diye Debug.Log'u kaldırdım
        }
        catch (Exception e)
        {
            Debug.LogWarning("Mikrofon kapatılırken hata oluştu: " + e.Message);
        }
    }

    public void UnmuteMicrophone()
    {
        try
        {
            VivoxService.Instance.UnmuteInputDevice();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Mikrofon açılırken hata oluştu: " + e.Message);
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