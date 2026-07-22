using UnityEngine;

public class TestBox : MonoBehaviour, IInteractable
{
    public string promptText = "[E] Kutuyu Döndür";

    public string GetInteractPrompt()
    {
        return promptText;
    }

    public void Interact()
    {
        // E'ye basınca kutu 45 derece dönsün
        transform.Rotate(Vector3.up * 45f);
        Debug.Log("Kutu ile etkileşime geçildi!");
    }
}