using UnityEngine;
using Unity.Netcode;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance;

    [Header("Bölüm Durumları")]
    public NetworkVariable<bool> isPuzzleActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isPatientSaved = new NetworkVariable<bool>(false);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CompletePuzzleRpc()
    {
        if (IsServer)
        {
            isPatientSaved.Value = true;
            Debug.Log("Tebrikler! Hasta başarıyla tedavi edildi, bölüm geçildi!");
        }
    }
}