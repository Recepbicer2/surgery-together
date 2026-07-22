public interface IInteractable
{
    // Objenin ekranda göstereceği yazı (Örn: "Tahtayı Al", "Kapıyı Aç")
    string GetInteractPrompt();

    // E tuşuna basıldığında tetiklenecek asıl fonksiyon
    void Interact();
}