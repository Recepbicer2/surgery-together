using Unity.Netcode;
using UnityEngine;

public class LeverController : NetworkBehaviour
{
    [Header("Animasyon Ayarları")]
    public Animator leverAnimator;
    public string animParametreAdi = "Pull";

    private NetworkVariable<bool> isLeverPulled = new NetworkVariable<bool>(false);

    private void Update()
    {
        if (isLeverPulled.Value) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

            Vector3 playerPos = NetworkManager.Singleton.LocalClient.PlayerObject.transform.position;
            Vector3 salterPos = transform.position;
            float mesafe = Vector3.Distance(salterPos, playerPos);

            if (mesafe <= 0.7f)
            {
                Debug.Log("✅ Şaltere basıldı! İşlemler başlıyor...");
                ToggleLeverRpc();
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleLeverRpc()
    {
        if (isLeverPulled.Value) return;

        isLeverPulled.Value = true;

        // 1. GAZ SIZINTISINI DURDUR
        GasLeakManager gasManager = FindFirstObjectByType<GasLeakManager>();
        if (gasManager != null)
        {
            gasManager.StopLeakRpc();
        }
        else
        {
            Debug.LogError("Sahnedeki GasLeakManager bulunamadı!");
        }

        // 2. ANİMASYONU OYNAT
        PlayAnimationRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayAnimationRpc()
    {
        if (leverAnimator != null)
        {
            leverAnimator.SetTrigger(animParametreAdi);
        }
        else
        {
            Debug.LogWarning("LeverController'da Animator atanmamış!");
        }
    }
}