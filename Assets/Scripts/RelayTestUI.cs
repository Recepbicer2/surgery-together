using System.Text.RegularExpressions;
using UnityEngine;
using TMPro; // TextMeshPro kullanacağımız için eklendi

public class RelayTestUI : MonoBehaviour
{
    [Header("Senin Lobi Tasarımın (UI Referansları)")]
    public GameObject LobiBaglantiPaneli; // Host Ol, Kod Gir kısmının olduğu panel
    public GameObject BeklemeOdasiPaneli; // Başarıyla bağlandıktan sonra açılacak panel

    [Header("Input ve Yazı Alanları")]
    public TMP_InputField inputJoinCode; // Unity'den Kod Girme kutunu buraya sürükleyeceksin
    public TextMeshProUGUI statusMessageText; // "Bağlanıyor..." veya Hata yazısı için
    public TextMeshProUGUI odaKoduGostergeText; // Host olan kişiye kodunu göstermek için

    // UI kullanacağımız için isConnected vb. değişkenleri sildik.
    private static readonly Regex RelayCodeRegex = new Regex("^[6789BCDFGHJKLMNPQRTWbcdfghjklmnpqrtw]{6,12}$");

    private string CleanJoinCode(string rawCode)
    {
        if (string.IsNullOrEmpty(rawCode)) return "";
        return Regex.Replace(rawCode, @"\s+", "").ToUpper().Trim();
    }

    // "HOST OL" butonuna (On Click) bu fonksiyonu bağlayacağız
    public async void StartHostProcess()
    {
        statusMessageText.text = "Oda Kuruluyor, Bekleyin...";

        string joinCode = await RelayManager.Instance.CreateRelay(4); // 4 kişilik oda

        if (!string.IsNullOrEmpty(joinCode))
        {
            // Bağlantı başarılı!
            odaKoduGostergeText.text = "Oda Kodun: " + joinCode;
            OnSuccessfulConnection();
        }
        else
        {
            statusMessageText.text = "HATA: Oda oluşturulamadı!";
            statusMessageText.color = Color.red;
        }
    }

    // "ODAYA KATIL" butonuna (On Click) bu fonksiyonu bağlayacağız
    public async void StartJoinProcess()
    {
        // Kutudaki yazıyı al ve temizle
        string cleanedCode = CleanJoinCode(inputJoinCode.text);

        if (RelayCodeRegex.IsMatch(cleanedCode))
        {
            statusMessageText.text = "Odaya Bağlanılıyor...";
            statusMessageText.color = Color.white;

            bool success = await RelayManager.Instance.JoinRelay(cleanedCode);

            if (success)
            {
                odaKoduGostergeText.text = "Odaya Başarıyla Katıldın!";
                OnSuccessfulConnection();
            }
            else
            {
                statusMessageText.text = "HATA: Odaya katılım başarısız! Kodu kontrol et.";
                statusMessageText.color = Color.red;
            }
        }
        else
        {
            statusMessageText.text = "HATA: Geçersiz kod formatı!";
            statusMessageText.color = Color.red;
        }
    }

    // Bağlantı başarılı olunca panelleri değiştirir
    private void OnSuccessfulConnection()
    {
        // Fareyi lobi işlemleri için görünür tutuyoruz
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Bağlantı panelini (Kod girme vs.) kapat, Bekleme odasını aç!
        if (LobiBaglantiPaneli != null && BeklemeOdasiPaneli != null)
        {
            LobiBaglantiPaneli.SetActive(false);
            BeklemeOdasiPaneli.SetActive(true);
        }
    }

    // Sonradan ekleyeceğimiz "Oyunu Başlat" butonu için şimdiden hazır dursun
    public void StartGameFromLobby()
    {
        // Şimdilik sadece menüyü kapatıp oyuna salacak
        BeklemeOdasiPaneli.SetActive(false);

        // Oyun başladığı için fareyi kilitliyoruz (FPS/Karakter kontrolü için)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}