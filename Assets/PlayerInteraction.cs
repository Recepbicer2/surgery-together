using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Etkileşim Ayarları")]
    public float interactDistance = 3f;
    public LayerMask interactLayer;
    public Transform holdPoint; // Kameranın önündeki HoldPoint noktası

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

        // 1. ELİMİZDE EŞYA VARKEN G TUŞUNA BASILIRSA (FIRLAT / BIRA)
        if (currentHeldObject != null)
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                // Kameranın baktığı yöne doğru fırlat
                currentHeldObject.Throw(playerCam.transform.forward);
                currentHeldObject = null;
            }

            // Elimizde eşya varken ekrandaki E ipucunu gizle
            if (interactUI != null && interactUI.gameObject.activeSelf)
                interactUI.gameObject.SetActive(false);

            return; // Elimizde eşya varken yeni etkileşim arama
        }

        // 2. ELİMİZ BOŞSA NORMAL RAYCAST ETKİLEŞİMİ YAP
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
                if (interactUI != null)
                {
                    interactUI.text = interactable.GetInteractPrompt();
                    interactUI.gameObject.SetActive(true);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();

                    // Eğer etkileşime geçtiğimiz obje elimize alınabilen bir objeyse
                    HoldableObject holdable = hit.collider.GetComponent<HoldableObject>();
                    if (holdable != null)
                    {
                        currentHeldObject = holdable;
                        currentHeldObject.PickUp(holdPoint);
                    }
                }

                return;
            }
        }

        if (interactUI != null && interactUI.gameObject.activeSelf)
        {
            interactUI.gameObject.SetActive(false);
        }
    }
}