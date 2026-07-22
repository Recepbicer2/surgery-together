using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class HoldableObject : NetworkBehaviour, IInteractable
{
    [Header("Eşya Ayarları")]
    public string promptText = "[E] Nesneyi Al";
    public float throwForce = 12f;

    private Rigidbody rb;
    private Transform currentHolder;
    private bool isHeld = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Eğer bir oyuncu bu nesneyi tutuyorsa, herkesin ekranında bu obje o oyuncunun elini takip edecek
        if (isHeld && currentHolder != null)
        {
            transform.position = currentHolder.position;
            transform.rotation = currentHolder.rotation;
        }
    }

    public string GetInteractPrompt()
    {
        return isHeld ? "" : promptText;
    }

    // 1. AŞAMA: Oyuncu Etkileşime Girer
    public void Interact()
    {
        if (isHeld) return;

        // FindFirstObjectByType SİLDİK! Sadece tuşa basan YEREL oyuncumuzun ID'sini alıyoruz.
        if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            ulong myClientId = NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId;

            // İşlemi lokalde yapma, sunucuya istek gönder!
            RequestPickUpRpc(myClientId);
        }
    }

    // 2. AŞAMA: Sunucu İstek Alır
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPickUpRpc(ulong playerNetworkObjId)
    {
        if (isHeld) return; // İki kişi aynı anda E'ye basarsa ilk basan alır.

        // Sunucu onayladı, şimdi ağdaki herkesin ekranında objeyi o oyuncunun eline ver!
        ExecutePickUpRpc(playerNetworkObjId);
    }

    // 3. AŞAMA: Herkesin Ekranında Eşya Ele Geçer
    [Rpc(SendTo.Everyone)]
    private void ExecutePickUpRpc(ulong playerNetworkObjId)
    {
        // Ağa bağlı olan tüm oyuncular arasından o ID'ye sahip karakteri buluyoruz
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjId, out NetworkObject playerObj))
        {
            PlayerInteraction player = playerObj.GetComponent<PlayerInteraction>();
            if (player != null && player.holdPoint != null)
            {
                isHeld = true;
                currentHolder = player.holdPoint;

                // Fiziği herkesin bilgisayarında kapatıyoruz ki titreme yapmasın
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    // === BIRAKMA (DROP) İŞLEMİ ===
    public void Drop()
    {
        if (!isHeld) return;
        RequestDropRpc(); // Bırakma isteğini sunucuya yolla
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDropRpc()
    {
        ExecuteDropRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ExecuteDropRpc()
    {
        isHeld = false;
        currentHolder = null;

        // Fiziği herkesin ekranında tekrar açıyoruz
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    // === FIRLATMA (THROW) İŞLEMİ ===
    public void Throw(Vector3 throwDirection)
    {
        if (!isHeld) return;
        RequestThrowRpc(throwDirection); // Fırlatma isteğini ve yönünü sunucuya yolla
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestThrowRpc(Vector3 throwDirection)
    {
        ExecuteThrowRpc(throwDirection);
    }

    [Rpc(SendTo.Everyone)]
    private void ExecuteThrowRpc(Vector3 throwDirection)
    {
        isHeld = false;
        currentHolder = null;

        rb.isKinematic = false;
        rb.useGravity = true;

        transform.position += throwDirection * 0.5f;
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
    }
    // === YERLEŞTİRME (PLACE) İŞLEMİ ===
    public void Place(Vector3 position, Quaternion rotation)
    {
        if (!isHeld) return;
        RequestPlaceRpc(position, rotation);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlaceRpc(Vector3 position, Quaternion rotation)
    {
        ExecutePlaceRpc(position, rotation);
    }

    [Rpc(SendTo.Everyone)]
    private void ExecutePlaceRpc(Vector3 position, Quaternion rotation)
    {
        isHeld = false;
        currentHolder = null;

        // Fiziği tekrar aç ve objeyi kutunun belirlediği noktaya oturt
        rb.isKinematic = false;
        rb.useGravity = true;

        transform.position = position;
        transform.rotation = rotation;
    }
    public bool IsHeld()
    {
        return isHeld;
    }
}