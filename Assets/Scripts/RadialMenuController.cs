using UnityEngine;
using Unity.Netcode;

public class RadialMenuController : NetworkBehaviour
{
    [Header("UI Ayarları")]
    public GameObject radialMenuUI;

    [Header("Referanslar")]
    // HandBoard scriptine ulaşmak için bir referans ekliyoruz
    private HandBoard myHandBoard;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            this.enabled = false;
            return;
        }

        // 'true' parametresi sayesinde HandBoard inaktif (kapalı) olsa bile bulur
        if (myHandBoard == null)
        {
            myHandBoard = GetComponentInChildren<HandBoard>(true);
        }

        if (myHandBoard == null)
        {
            Debug.LogError("HATA: Karakterde veya çocuk objelerinde HandBoard bulunamadı!");
        }

        // RadialMenu kapalı (inaktif) olsa bile sahnede bulmamızı sağlayan kod
        if (radialMenuUI == null)
        {
            Transform[] tumObjeler = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in tumObjeler)
            {
                // Sadece adı eşleşen ve sahnede var olan objeyi alıyoruz (Prefab'ları ayıklamak için scene.isLoaded kullanıyoruz)
                if (t.name == "RadialMenu_Background" && t.gameObject.scene.isLoaded)
                {
                    radialMenuUI = t.gameObject;
                    break;
                }
            }
        }

        if (radialMenuUI != null)
        {
            radialMenuUI.SetActive(false);
            Debug.Log("Radial menü başarıyla bulundu ve bağlandı!");
        }
        else
        {
            Debug.LogError("KRİTİK HATA: Sahnede 'RadialMenu_Background' adında bir obje bulunamadı. Adını kontrol et!");
        }
    }

    void Update()
    {
        if (!IsOwner) return; // Karakter benim değilse okuma

        // Eğer UI obje oyun içinde bir şekilde silinirse bizi uyaracak
        if (radialMenuUI == null)
        {
            Debug.LogWarning("DİKKAT: Radial menü başta bulundu ama sonradan kayboldu!");
            return;
        }

        // TAB tuşuna BASILI tutulduğunda
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log("-> TAB TUŞUNA BASILDI! Menü açılıyor...");
            radialMenuUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // TAB tuşundan elini ÇEKTİĞİNDE
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            Debug.Log("-> TAB TUŞU BIRAKILDI! Menü kapanıyor...");
            radialMenuUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ==========================================
    // BUTON FONKSİYONLARI
    // ==========================================

    public void Buton_TahtaAl()
    {
        if (myHandBoard != null)
        {
            myHandBoard.DurumDegistir(HandBoard.TahtaDurumu.Normal);
            Debug.Log("Tahta başarıyla ele alındı!");
        }
        else
        {
            // Ne olur ne olmaz son bir kez daha 'true' ile aramayı dene
            myHandBoard = GetComponentInChildren<HandBoard>(true);
            if (myHandBoard != null)
            {
                myHandBoard.DurumDegistir(HandBoard.TahtaDurumu.Normal);
            }
            else
            {
                Debug.LogError("HATA: Butona basıldı ama hala HandBoard bulunamıyor!");
            }
        }
    }

    public void Buton_BosEl()
    {
        if (myHandBoard != null)
        {
            // Tahtayı gizleme (elleri boşaltma) durumuna geçir
            myHandBoard.DurumDegistir(HandBoard.TahtaDurumu.Sakli);
        }
    }

    public void Buton_Dans()
    {
        Debug.Log("Butona tıklandı: Dans Emote!");
        // Dans animasyonlarını buraya bağlayacağız
    }

    public void Buton_Sola()
    {
        Debug.Log("Butona tıklandı: Sol Emote!");
        // Diğer hareketleri buraya bağlayacağız
    }
}