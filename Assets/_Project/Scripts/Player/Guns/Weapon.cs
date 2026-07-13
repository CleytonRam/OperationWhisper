using Mirror;
using UnityEngine;

public class Weapon : NetworkBehaviour
{
    [Header("Configurações Gerais")]
    public KeyCode fireKey = KeyCode.Mouse0;
    public float range = 50f;
    public LayerMask shootableLayers = ~0;

    [Header("Dano (definido automaticamente pelo time)")]
    public int damage = 10; // Será sobrescrito no Start

    [Header("Referências")]
    private TeamManager teamManager;
    private Transform camTransform;

    void Start()
    {
        teamManager = GetComponent<TeamManager>();
        camTransform = GetComponentInChildren<Camera>()?.transform;
        if (camTransform == null)
            Debug.LogError("Nenhuma câmera encontrada no prefab!");

        // Define o dano baseado no time
        if (teamManager != null)
        {
            if (teamManager.IsIntruder())
                damage = 10;   // Intruso dá 10 de dano
            else
                damage = 20;   // Guarda dá 20 de dano
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

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
                // Não atira em si mesmo
                if (targetHealth.gameObject == gameObject) return;

                // Aplica o dano (servidor)
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