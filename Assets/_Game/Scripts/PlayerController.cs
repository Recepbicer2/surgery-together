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

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (cameraTransform == null && playerCamera != null) cameraTransform = playerCamera.transform;

        controller = GetComponent<CharacterController>();

        // EĞER BU KARAKTER BİZE AİT DEĞİLSE
        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            if (controller != null) controller.enabled = false;
            return;
        }

        // --- SADECE BİZİM KARAKTERİMİZ İÇİN ÇALIŞIR ---

        // 1. Sahnede Varsa Lobi/Ana Kamerayı Kapat (No Cameras Rendering Hatasını Önler)
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != playerCamera)
        {
            mainCam.gameObject.SetActive(false);
        }

        // 2. Bağlanan Oyuncu İçin Lobi/Giriş Ekranı Panellerini Kapat
        // (Ekranda takılı kalan "Host Ol / Odaya Katıl" panelini gizler)
        GameObject lobbyUI = GameObject.Find("LobbyCanvas") ?? GameObject.Find("StartCanvas") ?? GameObject.Find("Canvas");
        if (lobbyUI != null)
        {
            // Giriş paneli / oda kodu paneli Canvas içindeyse gizliyoruz
            Transform panelTransform = lobbyUI.transform.Find("LobbyPanel") ?? lobbyUI.transform.Find("StartPanel") ?? lobbyUI.transform.Find("MainPanel");
            if (panelTransform != null)
            {
                panelTransform.gameObject.SetActive(false);
            }
        }

        // 3. DOĞUM POZİSYONUNU AYARLAMA (Fizik kilitlenmesini önler)
        if (PlayerSpawnManager.Instance != null)
        {
            if (controller != null) controller.enabled = false; // Pozisyon değişirken fiziği kapat

            transform.position = PlayerSpawnManager.Instance.GetNextSpawnPosition();

            if (controller != null) controller.enabled = true; // Pozisyon değiştikten sonra aç
        }

        // 4. Kendi Kameramızı ve Ses Dinleyicimizi Aç
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        // 5. Oyuna Girildiği İçin Fareyi Ekranın Ortasına Kitle
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