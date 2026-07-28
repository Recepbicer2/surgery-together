using UnityEngine;
using Unity.Netcode;

public class PatientInteraction : NetworkBehaviour
{
    public float interactionRange = 3f;
    private Transform patientTransform;

    [Header("Player'ın Kendi Puzzle Canvası")]
    public GameObject localPuzzleCanvas; // Player prefab'inin içindeki Canvas'ı buraya sürükle!

    void Start()
    {
        // Sahnedeki geçici hastayı bulalım
        GameObject patient = GameObject.Find("PlaceholderPatient");
        if (patient != null) patientTransform = patient.transform;

        // Oyun başladığında puzzle kapalı olsun (Sadece bu karakteri kontrol eden oyuncu için)
        if (localPuzzleCanvas != null && IsOwner)
        {
            localPuzzleCanvas.SetActive(false);
        }
    }

    void Update()
    {
        // Sadece bu karakteri kontrol eden oyuncu çalıştırsın VE OYUN BAŞLAMIŞ OLMALI!
        if (!IsOwner) return;
        if (LobbyRoleManager.Instance == null || !LobbyRoleManager.Instance.isGameStarted.Value) return;

        if (patientTransform == null)
        {
            GameObject patient = GameObject.Find("PlaceholderPatient");
            if (patient != null) patientTransform = patient.transform;
            return;
        }

        float distance = Vector3.Distance(transform.position, patientTransform.position);

        // Hastaya yaklaşıp E tuşuna basıldığında
        if (distance <= interactionRange && Input.GetKeyDown(KeyCode.E))
        {
            if (localPuzzleCanvas != null)
            {
                bool isOpen = !localPuzzleCanvas.activeSelf;
                TogglePuzzle(isOpen);
            }
        }
    }

    void TogglePuzzle(bool isOpen)
    {
        localPuzzleCanvas.SetActive(isOpen);

        // Mouse imlecini puzzle'ı çözebilmesi için serbest bırak/kilitle
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}