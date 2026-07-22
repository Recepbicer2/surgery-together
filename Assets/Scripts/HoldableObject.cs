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
        // Eğer bir oyuncu bu nesneyi tutuyorsa, nesneyi HoldPoint pozisyonuna yumuşakça taşı
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

    public void Interact()
    {
        if (isHeld) return;

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        if (player != null && player.holdPoint != null)
        {
            PickUp(player.holdPoint);
        }
    }

    public void PickUp(Transform holdPoint)
    {
        isHeld = true;
        currentHolder = holdPoint;

        // Fiziği kapatıyoruz ki elimizdeyken titremesin ve karaktere çarpmasın
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Drop()
    {
        isHeld = false;
        currentHolder = null;

        // Fiziği tekrar açıyoruz
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void Throw(Vector3 throwDirection)
    {
        Drop();

        // Karakterin kendi Collider'ına çarpıp uzaya fırlamaması için nesneyi hafifçe ileri kaydırıyoruz
        transform.position += throwDirection * 0.5f;

        // Anlık patlama kuvveti uygula
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
    }

    public bool IsHeld()
    {
        return isHeld;
    }
}