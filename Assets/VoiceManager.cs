using Unity.Netcode;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        if (IsOwner)
        {
            await InitializeVivoxAsync();
        }
    }

    private async System.Threading.Tasks.Task InitializeVivoxAsync()
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

            await VivoxService.Instance.InitializeAsync();
            await VivoxService.Instance.LoginAsync();

            Channel3DProperties positionalOptions = new Channel3DProperties(maxDistance, minDistance, distanceFactor, AudioFadeModel.LinearByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, positionalOptions);

            // Başlangıçta mikrofonu sessize alıyoruz (Push-to-Talk için)
            MuteMicrophone();

            Debug.Log("Vivox 3D Sesli Sohbet Başarıyla Bağlandı!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Vivox Bağlantı Hatası: " + e.Message);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (VivoxService.Instance.IsLoggedIn)
        {
            VivoxService.Instance.Set3DPosition(gameObject, channelName);
        }

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
        IsTalking = true;
        VivoxService.Instance.UnmuteInputDevice();
        Debug.Log("Mikrofon Açıldı");
    }

    private void MuteMicrophone()
    {
        IsTalking = false;
        VivoxService.Instance.MuteInputDevice();
        Debug.Log("Mikrofon Kapatıldı");
    }

    public override async void OnDestroy()
    {
        base.OnDestroy();

        if (IsOwner && VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
        {
            await VivoxService.Instance.LeaveAllChannelsAsync();
            await VivoxService.Instance.LogoutAsync();
        }
    }
}