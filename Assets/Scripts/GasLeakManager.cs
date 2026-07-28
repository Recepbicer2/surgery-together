using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class GasLeakManager : NetworkBehaviour
{
    [Header("Arayüz (UI) Ayarları")]
    public GameObject warningPanel;
    public TextMeshProUGUI timerText;

    [Header("Sızıntı Ayarları")]
    public float timeLimit = 30f;

    private NetworkVariable<bool> isLeakActive = new NetworkVariable<bool>(false);
    private NetworkVariable<float> currentTimer = new NetworkVariable<float>(0f);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(StartGasLeakRoutine());
        }
    }

    private IEnumerator StartGasLeakRoutine()
    {
        yield return new WaitForSeconds(5f);

        isLeakActive.Value = true;
        currentTimer.Value = timeLimit;
    }

    void Update()
    {
        if (isLeakActive.Value)
        {
            warningPanel.SetActive(true);
            timerText.text = $"DİKKAT! GAZ SIZINTISI! ACELE ET: {Mathf.CeilToInt(currentTimer.Value)}sn";

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
            warningPanel.SetActive(false);
        }
    }

    // YENİ GÜNCEL RPC KODU
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StopLeakRpc()
    {
        isLeakActive.Value = false;
        Debug.Log("Server: Gaz sızıntısı başarıyla durduruldu!");
    }
}