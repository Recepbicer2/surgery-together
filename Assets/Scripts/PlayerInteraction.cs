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
    public TMP_Text interactUI;

    private Camera playerCam;
    private HoldableObject currentHeldObject;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        playerCam = GetComponentInChildren<Camera>();

        if (interactUI == null)
        {
            GameObject uiObj = GameObject.Find("InteractText");
            if (uiObj != null) interactUI = uiObj.GetComponent<TMP_Text>();
        }

        if (interactUI != null) interactUI.gameObject.SetActive(false);
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

        // Artık elimiz doluyken de Raycast atıyoruz ki kutuyu görebilelim!
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit hit;

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
                        ShowUI(interactable.GetInteractPrompt());

                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            interactable.Interact();
                            currentHeldObject = holdable;
                        }
                    }
                }
                return; // Işın bir etkileşim objesine çarptıysa işlemi bitir
            }
        }

        HideUI(); // Işın boşa gidiyorsa yazıyı kapat
    }

    private void ShowUI(string text)
    {
        if (interactUI != null)
        {
            interactUI.text = text;
            if (!interactUI.gameObject.activeSelf) interactUI.gameObject.SetActive(true);
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