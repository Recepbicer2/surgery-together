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

        // DOĞUM POZİSYONUNU AYARLAMA (Fizik kilitlenmesini önler)
        if (PlayerSpawnManager.Instance != null)
        {
            if (controller != null) controller.enabled = false; // Pozisyon değişirken fiziği kapat

            transform.position = PlayerSpawnManager.Instance.GetNextSpawnPosition();

            if (controller != null) controller.enabled = true; // Pozisyon değiştikten sonra aç
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }
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