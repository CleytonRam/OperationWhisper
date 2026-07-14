using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [Header("Atributos")]
    [SyncVar] public int currentHealth = 100;
    [SyncVar] public int maxHealth = 100;

    private TeamManager teamManager;

    void Start()
    {
        teamManager = GetComponent<TeamManager>();
        maxHealth = 100;
        currentHealth = 100;
    }

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
        currentHealth = 0;
        gameObject.SetActive(false);

        // Se for Intruso, notifica o GameManager (ele mesmo conta os vivos)
        TeamManager tm = GetComponent<TeamManager>();
        if (tm != null && tm.IsIntruder())
        {
            GameManager.Instance?.IntruderDied();
        }

        RpcOnDeath();
    }

    [ClientRpc]
    void RpcOnDeath()
    {
        Debug.Log("Morte visual ativada!");
    }

    public bool IsAlive() => currentHealth > 0;

    [Server]
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}