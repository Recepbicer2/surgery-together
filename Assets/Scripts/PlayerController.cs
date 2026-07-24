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
    public float gravity = -19.62f;

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
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (cameraTransform == null && playerCamera != null) cameraTransform = playerCamera.transform;

        controller = GetComponent<CharacterController>();

        // 1. ÖNCE SERVER YETKİSİYLE LOBİYE IŞINLA (Tüm oyuncular için server bunu yapar)
        if (IsServer && PlayerSpawnManager.Instance != null)
        {
            if (controller != null) controller.enabled = false;

            Vector3 lobbyPos = PlayerSpawnManager.Instance.GetLobbySpawnPosition();

            // NetworkTransform'u geçici olarak devre dışı bırakıp pozisyonu doğrudan set ediyoruz
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null) netTransform.enabled = false;

            transform.position = lobbyPos;

            if (netTransform != null) netTransform.enabled = true;

            if (controller != null) controller.enabled = true;
        }

        // 2. EĞER BU KARAKTER BİZE (LOCAL CLIENT'A) AİT DEĞİLSE KAMERALARI KAPAT
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

        // 3. SADECE KENDİ KARAKTERİMİZ İÇİN GÖRSEL VE UI AYARLARI
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
    }
    void Update()
    {
        if (!IsOwner) return;

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        HandleCrouch();

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

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (controller != null && controller.enabled)
        {
            controller.Move(move * currentSpeed * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

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

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);

        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * 10f);
            cameraTransform.localPosition = camPos;
        }
    }
}