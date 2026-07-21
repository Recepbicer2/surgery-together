using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Hareket Ayarlari")]
    public float moveSpeed = 4f;
    public float mouseSensitivity = 2f;

    [Header("Bilesenler")]
    public Transform cameraTransform;
    public Camera playerCamera;

    private CharacterController controller;
    private float verticalRotation = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Kamera ve Controller bileşenlerini güvenli şekilde al
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (cameraTransform == null && playerCamera != null) cameraTransform = playerCamera.transform;
        controller = GetComponent<CharacterController>();

        // --- EĞER BU KARAKTER BAŞKASINA AİTSE (REMOTE CLIENT) ---
        if (!IsOwner)
        {
            // Başkasının kamerasını ve ses dinleyicisini KESİNLİKLE KAPAT
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            if (controller != null) controller.enabled = false;
            return; // İşlemi bitir, alt taraftaki bizim kendi kameramız için çalışacak!
        }

        // --- SADECE BİZİM KARAKTERİMİZ İÇİN (LOCAL OWNER) ---

        // 1. Sahnede duran Lobi / Sahne kamerasını TAMAMEN KAPAT
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != playerCamera)
        {
            mainCam.gameObject.SetActive(false);
        }

        // 2. Kendi Oyuncu Kameramızı ve Ses Dinleyicimizi AÇ
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener == null) listener = playerCamera.gameObject.AddComponent<AudioListener>();
            listener.enabled = true;
        }

        // 3. Teleport ve Doğum Pozisyonu
        if (PlayerSpawnManager.Instance != null)
        {
            if (controller != null) controller.enabled = false;
            transform.position = PlayerSpawnManager.Instance.GetNextSpawnPosition();
            if (controller != null) controller.enabled = true;
        }

        // 4. Mouse'u Ekran Ortasına Kitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Yetki bizde değilse kesinlikle hiçbir Input çalıştırma
        if (!IsOwner) return;

        // 1. Etrafa Bakma (Mouse)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        // 2. Yürüme (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller != null && controller.enabled)
        {
            controller.Move(move * moveSpeed * Time.deltaTime);
        }
    }
}