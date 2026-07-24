using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Etkileşim Ayarları")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public Transform holdPoint;

    [Header("UI Referansları")]
    public TMP_Text interactUI; // DİKKAT: Bunu artık kodla aramayacağız, Inspector'dan atayacaksın!

    private Camera playerCam;
    private HoldableObject currentHeldObject;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1. ÖNCE HERKESİN YAZISINI KAPAT (Senin ekranında başkasının "E'ye bas" yazısı havada kalmasın)
        if (interactUI != null) interactUI.gameObject.SetActive(false);

        // 2. EĞER BU KARAKTER BANA AİT DEĞİLSE (KLONSA) KODUN GERİSİNİ ÇALIŞTIRMA
        if (!IsOwner) return;

        playerCam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner || playerCam == null) return;

        // ELİMİZDE EŞYA VARKEN G TUŞUNA BASILIRSA (FIRLAT)
        if (currentHeldObject != null && Input.GetKeyDown(KeyCode.G))
        {
            currentHeldObject.Throw(playerCam.transform.forward);
            currentHeldObject = null;
        }

        // Raycast ile bakılan objeyi kontrol et
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit hit;

        // Işın atıyoruz (Sadece interactLayer katmanındaki objelere çarpacak)
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // DURUM 1: ELİMİZDE EŞYA VAR VE BAKTIĞIMIZ ŞEY BİR KOYMA KUTUSU
                if (currentHeldObject != null)
                {
                    PlacementBox box = hit.collider.GetComponent<PlacementBox>();
                    if (box != null)
                    {
                        ShowUI(box.GetInteractPrompt());

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            currentHeldObject.Place(box.placePoint.position, box.placePoint.rotation);
                            currentHeldObject = null;
                        }
                    }
                    else
                    {
                        HideUI(); // Kutudan başka bir şeye bakıyorsak yazıyı gizle
                    }
                }
                // DURUM 2: ELİMİZ BOŞ VE ALINABİLİR BİR EŞYAYA BAKIYORUZ
                else
                {
                    HoldableObject holdable = hit.collider.GetComponent<HoldableObject>();
                    if (holdable != null)
                    {
                        ShowUI("ALMAK IÇIN E'YE BAS");

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            interactable.Interact();
                            currentHeldObject = holdable;
                        }
                    }
                }
                return; // Işın bir etkileşim objesine çarptıysa işlemi bitir ve UI'ı kapatma
            }
        }

        HideUI(); // Işın boşa gidiyorsa yazıyı kapat
    }

    private void ShowUI(string text)
    {
        Debug.Log("Raycast objeyi gördü! Gelen yazı: " + text);

        if (interactUI != null)
        {
            interactUI.text = text;
            if (!interactUI.gameObject.activeSelf) interactUI.gameObject.SetActive(true);
            Debug.Log("UI objesi bulundu ve ekrana basıldı.");
        }
        else
        {
            Debug.LogError("KANKA DİKKAT: interactUI şuan BOŞ! Inspector'dan PlayerInteraction scriptine InteractText'i atamamışsın!");
        }
    }

    private void HideUI()
    {
        if (interactUI != null && interactUI.gameObject.activeSelf)
        {
            interactUI.gameObject.SetActive(false);
        }
    }
}