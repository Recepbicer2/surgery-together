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

        // Inspector'da atanmadıysa alt objelerdeki kamerayı otomatik bulur
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (cameraTransform == null && playerCamera != null) cameraTransform = playerCamera.transform;

        controller = GetComponent<CharacterController>();

        // EĞER BU KARAKTER BİZE AİT DEĞİLSE (Diğer oyuncunun karakteriyse)
        if (!IsOwner)
        {
            // Başkasının kamerasını ve sesini kapatıyor
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }

            // Başkasının karakter kontrolcüsünü kapatıyor
            if (controller != null)
            {
                controller.enabled = false;
            }
            return;
        }

        // --- BURASI SADECE BİZİM (OWNER) KARAKTERİMİZ İÇİN ÇALIŞIR ---

        // Eğer CharacterController kapalı kaldıysa kesinlikle aktif et
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Kameranın ve AudioListener'ın açık olduğundan emin ol
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