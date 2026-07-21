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

        // EĞER BU KARAKTER BİZE AİT DEĞİLSE (Diğer Oyuncular)
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

        // --- SADECE BİZİM KENDİ KARAKTERİMİZ İÇİN ÇALIŞIR ---

        // 1. Sahnede duran Lobi Kamerasını ve AudioListener'ını TAMAMEN KAPAT
        // (2 Audio Listener uyarısını çözen yer burasıdır)
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != playerCamera)
        {
            AudioListener mainListener = mainCam.GetComponent<AudioListener>();
            if (mainListener != null) mainListener.enabled = false;

            mainCam.gameObject.SetActive(false);
        }

        // 2. Kendi Oyuncu Kameramızı ve Ses Dinleyicimizi AÇ
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        // 3. Katıl Butonunun Olduğu Lobi Canvas'ını Kapat
        GameObject lobbyCanvas = GameObject.Find("HUD_Canvas") ?? GameObject.Find("HUD_Canvas 1");
        if (lobbyCanvas != null)
        {
            lobbyCanvas.SetActive(false);
        }

        // 4. Doğum Pozisyonunu Ayarla
        if (PlayerSpawnManager.Instance != null)
        {
            if (controller != null) controller.enabled = false;
            transform.position = PlayerSpawnManager.Instance.GetNextSpawnPosition();
            if (controller != null) controller.enabled = true;
        }

        // 5. Mouse'u Ortaya Kitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
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