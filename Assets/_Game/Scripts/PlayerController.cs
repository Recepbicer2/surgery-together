using Unity.Netcode;
using UnityEngine;

// 1. MonoBehaviour YERİNE NetworkBehaviour KULLANIYORUZ
public class PlayerController : NetworkBehaviour
{
    [Header("Hareket Ayarlari")]
    public float moveSpeed = 4f;
    public float mouseSensitivity = 2f;

    [Header("Bilesenler")]
    public Transform cameraTransform;
    public Camera playerCamera; // Kamera bileşenini kapatıp açabilmek için eklendi

    private CharacterController controller;
    private float verticalRotation = 0f;

    // Start yerine Network'ün kendi doğma fonksiyonunu kullanıyoruz
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        controller = GetComponent<CharacterController>();

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

        if (controller != null) controller.enabled = true;

        // DIKKAT: Cursor.lockState kısmını buradan kaldırdık!
        // İmleç kilitlenmesini bağlantı başarılı olunca RelayTestUI veya oyun akışı yönetecek.
    }

    void Update()
    {
        // 2. KRİTİK KONTROL: Bu karakter bizim değilse hiç kod çalıştırma!
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
        if (controller != null)
        {
            controller.Move(move * moveSpeed * Time.deltaTime);
        }
    }
}