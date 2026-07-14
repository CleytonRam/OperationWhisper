using Mirror;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    [Header("Configurações Gerais")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public float range = 50f;
    public LayerMask shootableLayers = ~0;

    [Header("Dano")]
    public int damage = 10;

    [Header("Referências")]
    private TeamManager teamManager;
    private Transform camTransform;
    private PlayerController playerController;

    void Start()
    {
        teamManager = GetComponent<TeamManager>();
        playerController = GetComponent<PlayerController>();
        camTransform = GetComponentInChildren<Camera>()?.transform;
        if (camTransform == null)
            Debug.LogError("Nenhuma câmera encontrada no prefab!");

        if (teamManager != null)
        {
            if (teamManager.IsIntruder())
                damage = 10;
            else
                damage = 20;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // ⛔ IMPEDE ATIRAR SE ESTIVER CARRREGANDO COFRE
        if (playerController != null && playerController.IsCarrying())
        {
            Debug.Log("Não pode atirar enquanto carrega o cofre!");
            return;
        }

        if (Input.GetKeyDown(fireKey))
        {
            CmdFire();
        }
    }

    [Command]
    void CmdFire()
    {
        Health health = GetComponent<Health>();
        if (health != null && !health.IsAlive()) return;

        Fire();
    }

    void Fire()
    {
        Vector3 origin = camTransform.position;
        Vector3 direction = camTransform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, range, shootableLayers))
        {
            Debug.Log($"Tiro acertou: {hit.collider.gameObject.name}");

            Health targetHealth = hit.collider.GetComponent<Health>();
            if (targetHealth != null)
            {
                if (targetHealth.gameObject == gameObject) return;
                targetHealth.TakeDamage(damage, gameObject);
                Debug.Log($"{gameObject.name} causou {damage} de dano em {targetHealth.gameObject.name}");
            }
            else
            {
                Debug.Log("Tiro na parede/objeto");
            }
        }
        else
        {
            Debug.Log("Tiro no ar");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (camTransform == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(camTransform.position, camTransform.forward * range);
    }
}