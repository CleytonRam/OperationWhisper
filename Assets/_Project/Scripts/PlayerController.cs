using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minLookAngle = -90f;
    [SerializeField] private float maxLookAngle = 90f;

    // Componentes
    private CharacterController controller;
    private Transform cameraHolder;
    private float verticalRotation = 0f;
    private Vector3 velocity;

    void Start()
    {
        // Pega o CharacterController (se não tiver, adiciona)
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }

        // Encontra o CameraHolder (filho do Player)
        cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder == null)
        {
            Debug.LogError("CameraHolder não encontrado! Crie um GameObject filho chamado 'CameraHolder'.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.enabled = isLocalPlayer;
            Debug.Log(isLocalPlayer ? "Minha câmera está ativa!" : "Câmera do outro jogador desativada.");
        }

        // SÓ ATIVA O MICROFONE/ÁUDIO SE FOR O JOGADOR LOCAL
        AudioListener listener = GetComponentInChildren<AudioListener>();
        if (listener != null)
        {
            listener.enabled = isLocalPlayer;
        }
    }

    void Update()
    {
        //Se não for o jogador local, não mexe
        if (!isLocalPlayer) return;

        //Olhar com o Mouse
        LookAround();

        //Movimento com WASD (relativo à direção da câmera)
        Move();

        // Pulo (opcional)
        Jump();
    }

    void LookAround()
    {
        // Captura o movimento do mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotação horizontal (no corpo do Player)
        transform.Rotate(0, mouseX, 0);

        // Rotação vertical (na CameraHolder)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void Move()
    {
        // Captura as teclas WASD
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Direção do movimento relativa à câmera (ignorando a inclinação vertical)
        Vector3 forward = cameraHolder.forward;
        Vector3 right = cameraHolder.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Vetor de movimento
        Vector3 moveDirection = (forward * vertical) + (right * horizontal);
        moveDirection.Normalize();

        // Aplica o movimento (CharacterController já lida com colisões)
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Aplica gravidade (CharacterController já tem gravidade, mas vamos adicionar se quiser)
        controller.Move(velocity * Time.deltaTime);
    }

    // Opcional: se quiser pular depois, é só adicionar essa lógica
    void Jump()
    {
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}