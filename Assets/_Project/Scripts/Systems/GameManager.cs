using Mirror;
using UnityEngine;
using System.Collections;

public class GameManager : NetworkSingleton<GameManager>
{
    [Header("Referências")]
    public GameObject cofrePrefab;
    public Transform cofreSpawnPoint;
    public Transform extracaoPoint;
    public float timeLimit = 300f;

    [Header("Estado")]
    [SyncVar] public float timeRemaining = 300f;
    [SyncVar] public bool gameOver = false;

    [Header("UI (Temporário)")]
    public GameObject victoryPanel;
    public TMPro.TextMeshProUGUI victoryText;

    void Awake()
    {
        Debug.Log("GameManager Awake - isServer: " + isServer);
    }

    void Start()
    {
        if (isServer)
        {
            timeRemaining = timeLimit;
            SpawnCofre();
        }
    }

    void Update()
    {
        if (!isServer || gameOver) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            GuardasWin();
        }
    }

    void SpawnCofre()
    {
        Debug.Log("SpawnCofre chamado");
        if (cofrePrefab != null && cofreSpawnPoint != null)
        {
            GameObject cofre = Instantiate(cofrePrefab, cofreSpawnPoint.position, Quaternion.identity);
            NetworkServer.Spawn(cofre);
            Debug.Log("Cofre spawnado!");
        }
        else
        {
            Debug.LogError("CofrePrefab ou SpawnPoint não configurado!");
        }
    }

    [Server]
    public void IntrudersWin()
    {
        if (gameOver) return;
        gameOver = true;
        RpcShowVictory("🟥 Intrusos Venceram!");
        Debug.Log("Intrusos venceram!");
        StartCoroutine(RestartRound(5f));
    }

    [Server]
    public void GuardasWin()
    {
        if (gameOver) return;
        gameOver = true;
        RpcShowVictory("🟦 Guardas Venceram!");
        Debug.Log("Guardas venceram!");
        StartCoroutine(RestartRound(5f));
    }

    // 🔥 MÉTODO QUE ESTAVA FALTANDO
    [Server]
    public void IntruderDied()
    {
        // Conta quantos Intrusos ainda estão vivos
        int intrudersAlive = 0;
        foreach (var player in FindObjectsOfType<TeamManager>())
        {
            if (player.IsIntruder() && player.GetComponent<Health>().IsAlive())
                intrudersAlive++;
        }
        if (intrudersAlive == 0)
        {
            GuardasWin();
        }
    }

    [ClientRpc]
    void RpcShowVictory(string message)
    {
        Debug.Log(message);
        if (victoryText != null)
            victoryText.text = message;
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    [ClientRpc]
    void RpcHideVictory()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    [Server]
    IEnumerator RestartRound(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 1. Resspawna todos os jogadores
        foreach (var health in FindObjectsOfType<Health>())
        {
            health.gameObject.SetActive(true);
            health.currentHealth = health.maxHealth;
        }

        // 2. Remove o cofre antigo
        foreach (var vault in FindObjectsOfType<Vault>())
        {
            NetworkServer.Destroy(vault.gameObject);
        }

        // 3. Reseta estado
        gameOver = false;
        timeRemaining = timeLimit;

        // 4. Spawna novo cofre
        SpawnCofre();

        // 5. Esconde UI
        RpcHideVictory();

        Debug.Log("Rodada reiniciada!");
    }
}