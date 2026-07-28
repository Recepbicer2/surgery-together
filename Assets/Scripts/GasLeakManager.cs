using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class GasLeakManager : NetworkBehaviour
{
    [Header("Arayüz (UI) Ayarları")]
    public UnityEngine.UI.Image gasVignetteImage;
    public TextMeshProUGUI timerText;

    [Header("Ses Ayarları")]
    public AudioSource hissAudioSource;
    public AudioSource alarmAudioSource;

    [Header("Sızıntı Ayarları")]
    public float timeLimit = 30f;

    private NetworkVariable<bool> isLeakActive = new NetworkVariable<bool>(false);
    private NetworkVariable<float> currentTimer = new NetworkVariable<float>(0f);

    private bool wasLeakActiveLastFrame = false;

    void Start()
    {
        if (gasVignetteImage != null)
        {
            Color startColor = gasVignetteImage.color;
            startColor.a = 0f;
            gasVignetteImage.color = startColor;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(StartGasLeakRoutine());
        }
    }

    private IEnumerator StartGasLeakRoutine()
    {
        // 1. DÖNGÜ: Host "Oyunu Başlat" butonuna basana kadar (isGameStarted true olana kadar) burada bekler!
        while (LobbyRoleManager.Instance == null || !LobbyRoleManager.Instance.isGameStarted.Value)
        {
            yield return null; // Her kare kontrol et
        }

        // 2. Oyuncular lobiden çıkıp oyuna ışınlandıktan sonra ufak bir intro gecikmesi (örn: 3 saniye)
        yield return new WaitForSeconds(3f);

        // 3. Artık gaz sızıntısını resmi olarak başlatabiliriz!
        isLeakActive.Value = true;
        currentTimer.Value = timeLimit;
    }

    void Update()
    {
        if (isLeakActive.Value != wasLeakActiveLastFrame)
        {
            if (isLeakActive.Value)
            {
                if (hissAudioSource != null && !hissAudioSource.isPlaying) hissAudioSource.Play();
                if (alarmAudioSource != null && !alarmAudioSource.isPlaying) alarmAudioSource.Play();
            }
            else
            {
                if (hissAudioSource != null && hissAudioSource.isPlaying) hissAudioSource.Stop();
                if (alarmAudioSource != null && alarmAudioSource.isPlaying) alarmAudioSource.Stop();
            }

            wasLeakActiveLastFrame = isLeakActive.Value;
        }

        if (isLeakActive.Value)
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(true);
                timerText.text = $"DİKKAT! GAZ SIZINTISI! ACELE ET: {Mathf.CeilToInt(currentTimer.Value)}sn";
            }

            if (gasVignetteImage != null)
            {
                float alphaPuls = (Mathf.Sin(Time.time * 3f) + 1f) / 2f;
                float finalAlpha = Mathf.Lerp(0.01f, 0.08f, alphaPuls);

                Color currentColor = gasVignetteImage.color;
                currentColor.a = finalAlpha;
                gasVignetteImage.color = currentColor;
            }

            if (IsServer)
            {
                currentTimer.Value -= Time.deltaTime;

                if (currentTimer.Value <= 0)
                {
                    isLeakActive.Value = false;
                    currentTimer.Value = 0;
                }
            }
        }
        else
        {
            if (timerText != null) timerText.gameObject.SetActive(false);

            if (gasVignetteImage != null)
            {
                Color currentColor = gasVignetteImage.color;
                currentColor.a = 0f;
                gasVignetteImage.color = currentColor;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StopLeakRpc()
    {
        isLeakActive.Value = false;
        Debug.Log("Server: Gaz sızıntısı başarıyla durduruldu!");
    }
}