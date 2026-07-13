using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [Header("Atributos")]
    [SyncVar] public int currentHealth = 100;
    [SyncVar] public int maxHealth = 100;

    [Header("Referências")]
    private TeamManager teamManager;

    void Start()
    {
        teamManager = GetComponent<TeamManager>();
        // Todos começam com 100 de vida (definido no Inspetor ou aqui)
        maxHealth = 100;
        currentHealth = 100;
    }

    // Método para tomar dano (chamado pelo servidor)
    [Server]
    public void TakeDamage(int damage, GameObject attacker)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} tomou {damage} de dano. Vida: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die(attacker);
        }
    }

    [Server]
    void Die(GameObject killer)
    {
        Debug.Log($"{gameObject.name} morreu! Matador: {killer.name}");
        // Desativa o personagem (pode ser substituído por animação de morte)
        gameObject.SetActive(false);

        // Notifica todos os clientes (efeitos visuais, etc.)
        RpcOnDeath();
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        // Efeito visual/sonoro de morte (pode colocar partículas)
        Debug.Log("Morte visual ativada!");
    }

    public bool IsAlive() => currentHealth > 0;

    // Método para curar (opcional, para testes)
    [Server]
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}