using Mirror;
using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTeamChanged))]
    public int teamID = 0; // 0 = Intruso, 1 = Guarda

    private PlayerController controller;
    private Renderer rend;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        // Se for o jogador local, ele escolhe o time via teclado (protótipo)
        if (isLocalPlayer)
        {
            // (opcional) poderia abrir um menu aqui
        }
    }

    // ESSE MÉTODO É CHAMADO QUANDO O OBJETO É SPAWNADO NO CLIENTE
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Aplica as configurações (cor, velocidade) com o valor atual do SyncVar
        ApplyTeamStats();
    }

    void Update()
    {
        // Só o jogador local pode escolher o time (teclas 1 e 2)
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CmdSetTeam(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CmdSetTeam(1);
        }
    }

    [Command]
    void CmdSetTeam(int newTeam)
    {
        teamID = newTeam;
        // Aplica no servidor (e o hook vai sincronizar para os clientes)
        ApplyTeamStats();
    }

    void OnTeamChanged(int oldTeam, int newTeam)
    {
        // Isso roda em todos os clientes quando o SyncVar muda
        ApplyTeamStats();
    }

    void ApplyTeamStats()
    {
        // Se o controller ainda não foi encontrado, tenta de novo
        if (controller == null)
            controller = GetComponent<PlayerController>();

        // Aplica a velocidade conforme o time
        if (controller != null)
        {
            if (teamID == 0) // Intruso
            {
                controller.moveSpeed = 6f;
                Debug.Log("Sou Intruso!");
            }
            else // Guarda
            {
                controller.moveSpeed = 3f;
                Debug.Log("Sou Guarda!");
            }
        }

        // Aplica a cor
        if (rend == null)
            rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            if (teamID == 0)
                rend.material.color = Color.red;
            else
                rend.material.color = Color.blue;
        }

        // Vida
        Health health = GetComponent<Health>();
        if (health != null)
        {
            if (teamID == 0) // Intruso
            {
                health.maxHealth = 100;
                health.currentHealth = 100;
            }
            else // Guarda
            {
                health.maxHealth = 100;
                health.currentHealth = 100;
            }
        }
    }

    // Funções auxiliares
    public bool IsIntruder() => teamID == 0;
    public bool IsGuard() => teamID == 1;
}