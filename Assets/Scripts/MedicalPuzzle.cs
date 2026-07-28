using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MedicalPuzzle : MonoBehaviour
{
    [Header("Puzzle UI Elemanları")]
    public Button[] puzzleButtons; // 3 adet buton buraya sürüklenecek
    public TextMeshProUGUI infoText; // Bilgilendirme yazısı

    private int[] correctSequence = { 0, 1, 2 }; // Doğru sıra: Önce 0, sonra 1, en son 2 numaralı buton
    private int currentIndex = 0;

    void OnEnable()
    {
        // Panel her açıldığında puzzle'ı sıfırla
        ResetPuzzle();
    }

    void Start()
    {
        // Butonlara tıklandığında çalışacak olayları (Listener) koda bağlıyoruz
        for (int i = 0; i < puzzleButtons.Length; i++)
        {
            int index = i; // C# closure kuralı için yerel kopya
            puzzleButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    void OnButtonClicked(int buttonIndex)
    {
        // Doğru sıradaki tuşa mı bastı?
        if (buttonIndex == correctSequence[currentIndex])
        {
            currentIndex++;
            if (infoText != null) infoText.text = $"Doğru! Adım {currentIndex} / 3";

            // Sıra tamamen bittiyse!
            if (currentIndex >= correctSequence.Length)
            {
                if (infoText != null) infoText.text = "TEDAVİ BAŞARILI!";

                // Server'a hastanın kurtarıldığını bildir
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.CompletePuzzleRpc();
                }

                // İmleci tekrar kilitle ve paneli kapat
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                gameObject.SetActive(false);
            }
        }
        else
        {
            // Yanlış tuşa bastıysa başa sar
            if (infoText != null) infoText.text = "YANLIŞ KARIŞIM! Başa dönüldü.";
            currentIndex = 0;
        }
    }

    void ResetPuzzle()
    {
        currentIndex = 0;
        // Hata veren kısım düzeltildi: Sadece null kontrolü yapıyoruz
        if (infoText != null)
        {
            infoText.text = "Doğru ilaç sırasını takip et: 1 -> 2 -> 3";
        }
    }
}