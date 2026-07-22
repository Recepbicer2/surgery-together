using System.Text.RegularExpressions;
using UnityEngine;

public class RelayTestUI : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("Sahnede kapatılmasını istediğin Canvas objesini buraya sürükle")]
    public GameObject hudCanvas;

    private string inputJoinCode = "";
    private string currentJoinCode = "";
    private string statusMessage = "";
    private bool isConnected = false;
    private bool isConnecting = false;

    private static readonly Regex RelayCodeRegex = new Regex("^[6789BCDFGHJKLMNPQRTWbcdfghjklmnpqrtw]{6,12}$");

    private void Update()
    {
        // Bağlantı kurulduktan sonra ESC'ye basarak imleci serbest bırakabilir veya tekrar kilitleyebilirsin
        if (isConnected)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // Lobi aşamasındayken sadece ESC ile aç kapa yapsın. 
                // Sol tık (GetMouseButtonDown) ile kilitleme kodunu SİLDİK ki butonlara tıklayabilelim!
                bool isLocked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = isLocked;
            }
        }
    }

    private void OnGUI()
    {
        // Bağlantı sağlandıysa ve imleç kilitliyse UI çizme (Ekrandaki kutucuklar kaybolsun)
        if (isConnected && Cursor.lockState == CursorLockMode.Locked)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(20, 20, 380, 350));

        if (!isConnected)
        {
            if (isConnecting)
            {
                GUILayout.Label("Bağlanıyor, lütfen bekleyin...");
            }
            else
            {
                // ODA OLUŞTURMA (HOST)
                if (GUILayout.Button("Host Ol (Oda Kur)", GUILayout.Height(40)))
                {
                    StartHostProcess();
                }

                GUILayout.Space(20);

                // ODAYA KATILMA (CLIENT)
                GUILayout.Label("Oda Kodu Girin:");

                // GUI Textfield ismini belirliyoruz ki odaklanma sorunları yaşanmasın
                GUI.SetNextControlName("JoinCodeInput");
                inputJoinCode = GUILayout.TextField(inputJoinCode, GUILayout.Height(30));

                if (GUILayout.Button("Odaya Katıl (Client)", GUILayout.Height(40)))
                {
                    string cleanedCode = CleanJoinCode(inputJoinCode);

                    if (RelayCodeRegex.IsMatch(cleanedCode))
                    {
                        statusMessage = "";
                        StartJoinProcess(cleanedCode);
                    }
                    else
                    {
                        statusMessage = "HATA: Geçersiz kod formatı!";
                    }
                }

                if (!string.IsNullOrEmpty(statusMessage))
                {
                    GUILayout.Space(10);
                    GUI.color = Color.red;
                    GUILayout.Label(statusMessage);
                    GUI.color = Color.white;
                }
            }
        }
        else
        {
            GUILayout.Label("--- BAĞLANTI BAŞARILI ---");
            if (!string.IsNullOrEmpty(currentJoinCode))
            {
                GUILayout.Label($"Arkadaşına Göndereceğin Kod: {currentJoinCode}", GUI.skin.box);
            }
            GUILayout.Label("Oyna tıklayarak imleci kilitleyebilir, ESC ile açabilirsin.");
        }

        GUILayout.EndArea();
    }

    private string CleanJoinCode(string rawCode)
    {
        if (string.IsNullOrEmpty(rawCode)) return "";
        return Regex.Replace(rawCode, @"\s+", "").ToUpper().Trim();
    }

    private async void StartHostProcess()
    {
        isConnecting = true;
        currentJoinCode = await RelayManager.Instance.CreateRelay(4);

        if (!string.IsNullOrEmpty(currentJoinCode))
        {
            OnSuccessfulConnection();
        }
        else
        {
            statusMessage = "Oda oluşturulurken hata oluştu!";
        }
        isConnecting = false;
    }

    private async void StartJoinProcess(string code)
    {
        isConnecting = true;
        bool success = await RelayManager.Instance.JoinRelay(code);

        if (success)
        {
            OnSuccessfulConnection();
        }
        else
        {
            statusMessage = "Odaya katılım başarısız!";
        }
        isConnecting = false;
    }

    private void OnSuccessfulConnection()
    {
        isConnected = true;

        // DİKKAT: Bağlantı kurulduğu an lobide olacağımız için İMLECİ SERBEST BIRAKIYORUZ!
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Sahnedeki HUD / UI Canvas'ını kapat
        if (hudCanvas != null)
        {
            hudCanvas.SetActive(false);
        }
    }
}