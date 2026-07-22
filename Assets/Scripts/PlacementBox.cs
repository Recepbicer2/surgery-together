using UnityEngine;
using Unity.Netcode;

public class PlacementBox : NetworkBehaviour, IInteractable
{
    [Header("Yerleştirme Ayarları")]
    public Transform placePoint; // Eşyanın tam oturacağı nokta
    public string promptText = "[E] Buraya Koy";

    public string GetInteractPrompt()
    {
        return promptText;
    }

    public void Interact()
    {
        // Tetikleme işini PlayerInteraction kodundan yapacağız, burası boş kalabilir.
    }
}