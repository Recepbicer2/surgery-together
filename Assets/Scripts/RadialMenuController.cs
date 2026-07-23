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

        // Önce Inspector'dan atanmış mı diye bakıyoruz, atanmadıysa alt objeler de dahil arıyoruz
        if (myHandBoard == null)
        {
            myHandBoard = GetComponentInChildren<HandBoard>();
        }

        if (myHandBoard == null)
        {
            Debug.LogError("HATA: Karakterde veya çocuk objelerinde HandBoard bulunamadı!");
        }

        if (radialMenuUI == null)
        {
            radialMenuUI = GameObject.Find("RadialMenu_Background");
        }

        if (radialMenuUI != null)
        {
            radialMenuUI.SetActive(false);
        }
    }
    void Update()
    {
        if (!IsOwner || radialMenuUI == null) return;

        // TAB tuşuna BASILI tutulduğunda menüyü aç
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            radialMenuUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // TAB tuşundan elini ÇEKTİĞİNDE menüyü kapat
        if (Input.GetKeyUp(KeyCode.Tab))
        {
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
            // Ne olur ne olmaz son bir kez daha aramayı dene
            myHandBoard = GetComponentInChildren<HandBoard>();
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