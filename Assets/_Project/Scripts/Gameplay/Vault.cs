using Mirror;
using UnityEngine;

public class Vault : NetworkBehaviour
{
    [Header("Estados")]
    [SyncVar(hook = nameof(OnStateChanged))]
    public bool isBeingCarried = false;

    [Header("Configurações")]
    public float throwForce = 8f; // Força aplicada no arremesso (ajuste no Inspetor)

    [Header("Referências")]
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogWarning("Vault não tem Rigidbody!");
    }

    void Start()
    {
        // CarryPoint não é mais necessário (vamos definir a posição diretamente)
    }

    [Server]
    public void PickUp(GameObject player)
    {
        if (isBeingCarried) return;

        TeamManager tm = player.GetComponent<TeamManager>();
        if (tm == null || !tm.IsIntruder()) return;

        isBeingCarried = true;
        transform.SetParent(player.transform);
        transform.localPosition = new Vector3(0, 1.5f, 1f);
        transform.localRotation = Quaternion.identity;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.SetCarrying(true, this);

        Debug.Log($"{player.name} pegou o cofre!");
    }

    [Server]
    public void Drop(GameObject player, Vector3 throwDirection)
    {
        if (!isBeingCarried) return;

        // Remove o parent e avisa o PlayerController
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
                pc.SetCarrying(false, null);
        }

        isBeingCarried = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // Verifica se há uma direção de arremesso (W pressionado)
            if (throwDirection != Vector3.zero)
            {
                // Aplica a força na direção da câmera (com um pequeno componente para cima, opcional)
                // Se quiser um pouco mais de altura, descomente a linha abaixo:
                // throwDirection.y += 0.3f; 
                throwDirection.Normalize();

                rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
                Debug.Log($"Cofre arremessado na direção {throwDirection} com força {throwForce}!");
            }
            else
            {
                Debug.Log("Cofre largado sem arremesso (cai no chão)");
            }
        }

        Debug.Log("Cofre foi largado!");
    }

    void OnStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"Estado do cofre: {(newValue ? "Carregado" : "No chão")}");
    }

    public bool IsCarriedBy(GameObject player)
    {
        return isBeingCarried && transform.parent != null && transform.parent.gameObject == player;
    }
}