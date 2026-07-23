using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

public class HandBoard : NetworkBehaviour
{
    [Header("UI & Yazı Referansları")]
    public TMP_Text tahtaMetni;
    public GameObject inputPaneli;
    public TMP_InputField yaziInput;
    public GameObject yaziInputPanel;

    [Header("Animasyon Pozisyonları")]
    public Transform mainCamera;

    // AĞ SENKRONİZASYONU: Tahtadaki metni ağ üzerinde senkron tutar
    private NetworkVariable<FixedString128Bytes> senkronizeYazi = new NetworkVariable<FixedString128Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Enum'u public yaptık ki NetworkVariable rahatça tanısın
    public enum TahtaDurumu { Sakli, Normal, Yazma, Gosterme }

    // AĞ SENKRONİZASYONU: Tahtanın Hangi Pozisyonda Olduğunu (Durumunu) Senkronize Tutar
    private NetworkVariable<TahtaDurumu> senkronizeDurum = new NetworkVariable<TahtaDurumu>(
        TahtaDurumu.Sakli,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // Pozisyonlar (Local)
    private Vector3 sakliPos = new Vector3(0.35f, -1.2f, 0.6f);
    private Vector3 normalPos = new Vector3(0.35f, -0.35f, 0.6f);
    private Vector3 yazmaPos = new Vector3(0f, -0.1f, 0.45f);
    private Vector3 gosterPos = new Vector3(0f, 0.1f, 0.5f);

    private Vector3 normalRot = new Vector3(10f, -20f, 0f);
    private Vector3 yazmaRot = new Vector3(25f, 0f, 0f);
    private Vector3 gosterRot = new Vector3(0f, 180f, 0f);

    public float yumusamaHizi = 10f;

    public override void OnNetworkSpawn()
    {
        // Ağ üzerindeki metin değiştiğinde tahta üzerindeki metni güncelle
        senkronizeYazi.OnValueChanged += (eskiYazi, yeniYazi) =>
        {
            if (tahtaMetni != null) tahtaMetni.text = yeniYazi.ToString();
        };

        if (tahtaMetni != null)
        {
            tahtaMetni.text = senkronizeYazi.Value.ToString();
        }
    }

    void Start()
    {
        // GÜVENLİK: Eğer Inspector'dan atanmamışsa bulmaya çalış (Ama Inspector'dan atamanı tavsiye ederim!)
        if (yaziInput == null)
            yaziInput = FindFirstObjectByType<TMP_InputField>(FindObjectsInactive.Include);

        if (inputPaneli == null && yaziInput != null)
            inputPaneli = yaziInput.transform.parent.gameObject;

        // DİKKAT: Ağı yormamak için onValueChanged olayını BURADAN KALDIRDIK.
        // Artık sadece oyuncu Enter'a basıp işi bitirdiğinde ağa gidecek.
    }

    void Update()
    {
        // 1. ÖNEMLİ DEĞİŞİKLİK: Pozisyon güncellemelerini IsOwner kontrolünden ÖNCEYE aldık.
        // Böylece ağdaki herkes senin tahtanın senkronizeDurum'una bakıp tahtanı hareket ettirebilecek!
        TahtaHareketiGuncelle();

        // 2. SADECE KARAKTERİN SAHİBİ TUŞLARA BASABİLİR (Tuş kontrolleri buradan sonra)
        if (!IsOwner) return;

        //if (Input.GetKeyDown(KeyCode.Alpha1))
           // DurumDegistir(TahtaDurumu.Sakli);

        //if (Input.GetKeyDown(KeyCode.Alpha2))
            //DurumDegistir(TahtaDurumu.Normal);

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (senkronizeDurum.Value == TahtaDurumu.Yazma)
                DurumDegistir(TahtaDurumu.Normal);
            else if (senkronizeDurum.Value == TahtaDurumu.Normal)
                DurumDegistir(TahtaDurumu.Yazma);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            if (senkronizeDurum.Value == TahtaDurumu.Gosterme)
                DurumDegistir(TahtaDurumu.Normal);
            else if (senkronizeDurum.Value == TahtaDurumu.Normal)
                DurumDegistir(TahtaDurumu.Gosterme);
        }
        if (yaziInputPanel != null)
        {
            bool yaziyorMu = (senkronizeDurum.Value == TahtaDurumu.Yazma);
            if (yaziInputPanel.activeSelf != yaziyorMu)
            {
                yaziInputPanel.SetActive(yaziyorMu);

                // Eğer panel açıldıysa direkt klavyeden yazabilsin diye focus (odaklanma) atalım
                if (yaziyorMu)
                {
                    // Eğer içinde InputField bileşeni varsa direkt odaklanır
                    var inputField = yaziInputPanel.GetComponentInChildren<UnityEngine.UI.InputField>();
                    if (inputField != null) inputField.ActivateInputField();

                    // Eğer TextMeshPro kullanıyorsan üstteki yerine alttakini aktif et:
                    // var tmpInput = yaziInputPanel.GetComponentInChildren<TMPro.TMP_InputField>();
                    // if (tmpInput != null) tmpInput.ActivateInputField();
                }
            }
        }
        if (senkronizeDurum.Value == TahtaDurumu.Yazma && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            TahtayaYaz();
        }
    }

    public void DurumDegistir(TahtaDurumu yeniDurum)
    {
        if (!IsOwner) return;

        // Ağdaki durumu güncelliyoruz ki herkes tahtanın yeni pozisyonunu bilsin
        senkronizeDurum.Value = yeniDurum;

        // UI, Fare ve Yazı Paneli SADECE BİZİM EKRANIMIZDA açılıp kapansın
        bool yazmaAcik = (yeniDurum == TahtaDurumu.Yazma);
        if (inputPaneli != null) inputPaneli.SetActive(yazmaAcik);

        Cursor.lockState = yazmaAcik ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = yazmaAcik;

        if (yazmaAcik && yaziInput != null)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(yaziInput.gameObject);

            yaziInput.Select();
            yaziInput.ActivateInputField();
        }
        else
        {
            if (yaziInput != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == yaziInput.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    public void TahtaYazisiniAnlikGuncelle(string yeniYazi)
    {
        if (!IsOwner) return;

        if (tahtaMetni != null) tahtaMetni.text = yeniYazi;
        senkronizeYazi.Value = yeniYazi;
    }

    public void TahtayaYaz()
    {
        if (tahtaMetni != null && yaziInput != null)
        {
            // Yazıyı sadece işlem bittiğinde ağdaki diğer oyunculara gönderiyoruz
            tahtaMetni.text = yaziInput.text;
            if (IsOwner)
            {
                senkronizeYazi.Value = yaziInput.text;
            }
        }

        // İşlem bitince input'un içini temizlemek istersen (isteğe bağlı):
        if (yaziInput != null) yaziInput.text = "";

        DurumDegistir(TahtaDurumu.Normal);
    }

    void TahtaHareketiGuncelle()
    {
        Vector3 hedefPos = normalPos;
        Vector3 hedefRot = normalRot;

        // Tahtanın durumunu artık kendi yerel değişkenimizden değil, AĞDAKİ SENKRONİZE DEĞİŞKENDEN okuyoruz
        switch (senkronizeDurum.Value)
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

        transform.localPosition = Vector3.Lerp(transform.localPosition, hedefPos, Time.deltaTime * yumusamaHizi);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(hedefRot), Time.deltaTime * yumusamaHizi);
    }
    // Çark menüden doğrudan çağrılacak public fonksiyonlar
    public void RadialMenu_TahtaAl()
    {
        if (!IsOwner) return;
        DurumDegistir(TahtaDurumu.Normal);
        Debug.Log("Radial Menu ile Tahta Normal konuma getirildi.");
    }

    public void RadialMenu_TahtayiSakla()
    {
        if (!IsOwner) return;
        DurumDegistir(TahtaDurumu.Sakli);
        Debug.Log("Radial Menu ile Tahta Saklandı.");
    }
}