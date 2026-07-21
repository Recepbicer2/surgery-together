using UnityEngine;
using TMPro;

public class HandBoard : MonoBehaviour
{
    [Header("UI & Yazı Referansları")]
    public TMP_Text tahtaMetni;         // Tahta üzerindeki World Space TMP_Text
    public GameObject inputPaneli;      // YaziInputPaneli objesi
    public TMP_InputField yaziInput;    // YaziInputPaneli içindeki TMP_InputField bileşeni

    [Header("Animasyon Pozisyonları")]
    public Transform mainCamera;

    // Pozisyonlar (Local)
    private Vector3 sakliPos = new Vector3(0.35f, -1.2f, 0.6f);   // 1 Tuşu: Ekranın altı (Görünmez)
    private Vector3 normalPos = new Vector3(0.35f, -0.35f, 0.6f); // 2 Tuşu: Elde tutma
    private Vector3 yazmaPos = new Vector3(0f, -0.1f, 0.45f);    // Tab Tuşu: Odaklanmış yazma
    private Vector3 gosterPos = new Vector3(0f, 0.1f, 0.5f);     // B Tuşu: Camdan gösterme

    private Vector3 normalRot = new Vector3(10f, -20f, 0f);
    private Vector3 yazmaRot = new Vector3(25f, 0f, 0f);
    private Vector3 gosterRot = new Vector3(0f, 180f, 0f);

    public float yumusamaHizi = 10f;

    private enum TahtaDurumu { Sakli, Normal, Yazma, Gosterme }
    private TahtaDurumu mevcutDurum = TahtaDurumu.Sakli; // Oyuna tahta gizli başlar

    void Start()
    {
        if (yaziInput != null)
        {
            yaziInput.onValueChanged.AddListener(TahtaYazisiniAnlikGuncelle);
        }
    }

    void Update()
    {
        // 1. Slottan Çıkar / İndir Tuşları (1 ve 2)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DurumDegistir(TahtaDurumu.Sakli); // Tahtayı bel seviyesinin altına indirir
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            DurumDegistir(TahtaDurumu.Normal); // Tahtayı ele alır
        }

        // 2. TAB Tuşu: Sadece tahta eldeyse (Normal) veya zaten yazma modundaysa çalışır
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (mevcutDurum == TahtaDurumu.Yazma)
                DurumDegistir(TahtaDurumu.Normal);
            else if (mevcutDurum == TahtaDurumu.Normal)
                DurumDegistir(TahtaDurumu.Yazma);
        }

        // 3. B Tuşu: Sadece tahta eldeyse veya gösterme modundaysa çalışır
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (mevcutDurum == TahtaDurumu.Gosterme)
                DurumDegistir(TahtaDurumu.Normal);
            else if (mevcutDurum == TahtaDurumu.Normal)
                DurumDegistir(TahtaDurumu.Gosterme);
        }

        // 4. Yazma Modundayken ENTER'a basınca yazıyı kaydet ve normal elde tutmaya dön
        if (mevcutDurum == TahtaDurumu.Yazma && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            TahtayaYaz();
        }

        // Pozisyon Geçişlerini Yumuşak Yap
        TahtaHareketiGuncelle();
    }

    void DurumDegistir(TahtaDurumu yeniDurum)
    {
        mevcutDurum = yeniDurum;

        bool yazmaAcik = (mevcutDurum == TahtaDurumu.Yazma);
        if (inputPaneli != null) inputPaneli.SetActive(yazmaAcik);

        // Fare kontrolü
        Cursor.lockState = yazmaAcik ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = yazmaAcik;

        if (yazmaAcik && yaziInput != null)
        {
            yaziInput.ActivateInputField();
        }
    }

    public void TahtaYazisiniAnlikGuncelle(string yeniYazi)
    {
        if (tahtaMetni != null)
        {
            tahtaMetni.text = yeniYazi;
        }
    }

    public void TahtayaYaz()
    {
        if (tahtaMetni != null && yaziInput != null)
        {
            tahtaMetni.text = yaziInput.text;
        }
        DurumDegistir(TahtaDurumu.Normal);
    }

    void TahtaHareketiGuncelle()
    {
        Vector3 hedefPos = normalPos;
        Vector3 hedefRot = normalRot;

        switch (mevcutDurum)
        {
            case TahtaDurumu.Sakli:
                hedefPos = sakliPos;
                hedefRot = normalRot;
                break;
            case TahtaDurumu.Normal:
                hedefPos = normalPos;
                hedefRot = normalRot;
                break;
            case TahtaDurumu.Yazma:
                hedefPos = yazmaPos;
                hedefRot = yazmaRot;
                break;
            case TahtaDurumu.Gosterme:
                hedefPos = gosterPos;
                hedefRot = gosterRot;
                break;
        }

        // Yumuşak geçişler
        transform.localPosition = Vector3.Lerp(transform.localPosition, hedefPos, Time.deltaTime * yumusamaHizi);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(hedefRot), Time.deltaTime * yumusamaHizi);
    }
}