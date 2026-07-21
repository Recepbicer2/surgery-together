using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using System.Threading.Tasks;

public class VoiceManager : NetworkBehaviour
{
    public static VoiceManager Instance;

    [Header("Vivox Ayarları")]
    public string channelName = "Game3DVoiceChannel";
    public KeyCode pushToTalkKey = KeyCode.V;

    [Header("Proximity (3D Ses) Ayarları")]
    public int minDistance = 2;
    public int maxDistance = 20;
    public float distanceFactor = 1f;

    public bool IsTalking { get; private set; }
    private bool isInChannel = false; // Kanala başarıyla girilip girilmediğini takip eden bayrak

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override async void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Sadece kendi karakterimiz için Vivox'u başlatıyoruz
        if (IsOwner)
        {
            await InitializeVivoxAsync();
        }
    }

    private async Task InitializeVivoxAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (!VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.InitializeAsync();
                await VivoxService.Instance.LoginAsync();
            }

            Channel3DProperties positionalOptions = new Channel3DProperties(maxDistance, minDistance, distanceFactor, AudioFadeModel.LinearByDistance);

            // Kanala katılmayı bekle
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, positionalOptions);

            // Başarılı şekilde katıldık!
            isInChannel = true;

            // Başlangıçta mikrofonu sessize alıyoruz (Push-to-Talk için)
            MuteMicrophone();

            Debug.Log("Vivox 3D Sesli Sohbet Başarıyla Bağlandı!");
        }
        catch (System.Exception e)
        {
            isInChannel = false;
            Debug.LogError("Vivox Bağlantı Hatası: " + e.Message);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // KRİTİK DÜZELTME: Sadece giriş yapılmışsa VE kanala tamamen katılmışsak pozisyon güncelle!
        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && isInChannel)
        {
            VivoxService.Instance.Set3DPosition(gameObject, channelName);
        }

        // Push To Talk Kontrolleri
        if (Input.GetKeyDown(pushToTalkKey))
        {
            UnmuteMicrophone();
        }
        else if (Input.GetKeyUp(pushToTalkKey))
        {
            MuteMicrophone();
        }
    }

    private void UnmuteMicrophone()
    {
        if (!isInChannel) return;

        IsTalking = true;
        VivoxService.Instance.UnmuteInputDevice();
        Debug.Log("Mikrofon Açıldı");
    }

    private void MuteMicrophone()
    {
        if (!isInChannel) return;

        IsTalking = false;
        VivoxService.Instance.MuteInputDevice();
        Debug.Log("Mikrofon Kapatıldı");
    }

    public override async void OnDestroy()
    {
        base.OnDestroy();

        if (IsOwner && VivoxService.Instance != null)
        {
            isInChannel = false;

            if (VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.LeaveAllChannelsAsync();
                await VivoxService.Instance.LogoutAsync();
            }
        }
    }
}