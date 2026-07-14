using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float carrySpeedMultiplier = 0.7f; // 70% da velocidade normal
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minLookAngle = -90f;
    [SerializeField] private float maxLookAngle = 90f;

    [Header("Agachar")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    // Componentes
    private CharacterController controller;
    private Transform cameraHolder;
    private float verticalRotation = 0f;
    private Vector3 velocity;
    private bool isCrouching = false;
    private bool isCarrying = false;

    // Referências
    private Vault carriedVault;
    private float originalHeight;

    [SyncVar] public Vector3 currentMoveDirection = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        originalHeight = standingHeight;
        controller.height = standingHeight;

        cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder == null)
            Debug.LogError("CameraHolder não encontrado!");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Configuração da câmera e áudio (já existente)
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
            cam.enabled = isLocalPlayer;

        AudioListener listener = GetComponentInChildren<AudioListener>();
        if (listener != null)
            listener.enabled = isLocalPlayer;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Agachar (tecla Ctrl ou C)
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }

        LookAround();
        Move();
        Jump();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Verifica se W está pressionado
            bool isHoldingW = Input.GetKey(KeyCode.W);
            Vector3 throwDirection = Vector3.zero;

            if (isHoldingW)
            {
                // Direção da câmera (forward) - onde você está olhando
                throwDirection = cameraHolder.forward;
                // Normaliza (já é normalizado)
                Debug.Log($"Arremessando na direção da câmera: {throwDirection}");
            }
            else
            {
                Debug.Log("Largando sem arremesso (W não pressionado)");
            }

            CmdDropVault(throwDirection);
        }

        // Interação com o cofre (E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            CmdInteract();
        }
        CmdUpdateMoveDirection(GetMoveDirection());
    }

    void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void Move()
    {
        // Ajusta altura do CharacterController para agachar
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        if (Mathf.Abs(controller.height - targetHeight) > 0.01f)
        {
            controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            // Ajusta a posição da câmera para acompanhar a altura
            Vector3 camPos = cameraHolder.localPosition;
            camPos.y = controller.height * 0.9f;
            cameraHolder.localPosition = camPos;
        }

        // Calcula a velocidade base
        float currentSpeed = moveSpeed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        if (isCarrying)
            currentSpeed *= carrySpeedMultiplier;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = cameraHolder.forward;
        Vector3 right = cameraHolder.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * vertical) + (right * horizontal);
        moveDirection.Normalize();

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);
    }

    void Jump()
    {
        // Não pode pular se estiver carregando cofre (opcional) ou agachado
        if (isCrouching) return;

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            float jump = jumpForce;
            if (isCarrying) jump *= 0.7f; // Pulo reduzido com cofre
            velocity.y = Mathf.Sqrt(jump * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // --- Comandos de Rede ---

    [Command]
    void CmdInteract()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (Collider col in colliders)
        {
            Vault vault = col.GetComponent<Vault>();
            if (vault != null && !vault.isBeingCarried)
            {
                vault.PickUp(gameObject);
                // Atualiza o estado local (será sincronizado via SyncVar)
                isCarrying = true;
                carriedVault = vault;
                return;
            }
        }
    }

    [Command]
    void CmdDropVault(Vector3 throwDirection)
    {
        if (carriedVault != null && carriedVault.isBeingCarried)
        {
            carriedVault.Drop(gameObject, throwDirection);
            isCarrying = false;
            carriedVault = null;
        }
    }

    Vector3 GetMoveDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontal) < 0.1f && Mathf.Abs(vertical) < 0.1f)
            return Vector3.zero;

        // Direção relativa à câmera
        Vector3 forward = cameraHolder.forward;
        Vector3 right = cameraHolder.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        return (forward * vertical + right * horizontal).normalized;
    }

    [Command]
    void CmdUpdateMoveDirection(Vector3 direction)
    {
        currentMoveDirection = direction;
    }

    // Este método é chamado pelo Vault quando o cofre é largado ou pego por outro
    public void SetCarrying(bool carrying, Vault vault = null)
    {
        isCarrying = carrying;
        carriedVault = vault;
    }


    public bool IsMovingForward()
    {
        // A direção atual tem componente Z positivo (frente do personagem)?
        // Transformamos a direção para o espaço local do personagem
        Vector3 localDir = transform.InverseTransformDirection(currentMoveDirection);
        return localDir.z > 0.1f; // Movendo pra frente
    }
    // Verifica se está carregando algo
    public bool IsCarrying() => isCarrying;

    // Verifica se está agachado (para dutos)
    public bool IsCrouching() => isCrouching;
}