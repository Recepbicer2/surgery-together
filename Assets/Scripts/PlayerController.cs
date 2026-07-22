using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float mouseSensitivity = 2f;

    [Header("Fizik & Zıplama")]
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f; // Gerçekçi düşüş için standart gravity (-9.81) x 2

    [Header("Eğilme (Crouch) Ayarları")]
    public float normalHeight = 2.0f;
    public float crouchHeight = 1.0f;
    public float cameraNormalY = 0.8f;
    public float cameraCrouchY = 0.3f;

    [Header("Bileşenler")]
    public Transform cameraTransform;
    public Camera playerCamera;

    private CharacterController controller;
    private float verticalRotation = 0f;
    private Vector3 velocity; // Yerçekimi ve zıplama hızını tutar
    private bool isGrounded;
    private float currentSpeed;

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

        // SADECE KENDİ KARAKTERİMİZ İÇİN
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam != playerCamera)
        {
            AudioListener mainListener = mainCam.GetComponent<AudioListener>();
            if (mainListener != null) mainListener.enabled = false;

            mainCam.gameObject.SetActive(false);
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.enabled = true;

            AudioListener listener = playerCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        GameObject lobbyCanvas = GameObject.Find("HUD_Canvas") ?? GameObject.Find("HUD_Canvas 1");
        if (lobbyCanvas != null)
        {
            lobbyCanvas.SetActive(false);
        }

        // Doğum Pozisyonunu Ayarla
        if (PlayerSpawnManager.Instance != null)
        {
            if (controller != null) controller.enabled = false;
            transform.position = PlayerSpawnManager.Instance.GetNextSpawnPosition();
            if (controller != null) controller.enabled = true;
        }

       // Cursor.lockState = CursorLockMode.Locked;
       // Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        // 1. Yere Basıyor Mu Kontrolü
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            // Yere tam oturması için küçük bir eksi değer veriyoruz
            velocity.y = -2f;
        }

        // 2. Etrafa Bakma (Mouse)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        // 3. Eğilme (Crouch) Kontrolü - Left Control
        HandleCrouch();

        // 4. Hız Belirleme (Koşma / Yürüme / Eğilme)
        if (Input.GetKey(KeyCode.LeftControl))
        {
            currentSpeed = crouchSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        // 5. Yürüme (WASD)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller != null && controller.enabled)
        {
            controller.Move(move * currentSpeed * Time.deltaTime);

            // 6. Zıplama (Space)
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // 7. Yerçekimi Uygulama (V = g * t)
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    void HandleCrouch()
    {
        if (controller == null) return;

        bool isCrouching = Input.GetKey(KeyCode.LeftControl);
        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        float targetCamY = isCrouching ? cameraCrouchY : cameraNormalY;

        // Karakter boyunu yumuşakça ayarla
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);

        // Kamera pozisyonunu yumuşakça indir/kaldır
        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * 10f);
            cameraTransform.localPosition = camPos;
        }
    }
}