using Mirror;
using UnityEngine;

public class ExtractionPoints : NetworkBehaviour
{
    [Header("Configurações")]
    public float checkRadius = 2f;

    void Update()
    {
        if (!isServer) return;

        // Procura por um jogador carregando o cofre dentro do raio
        Collider[] colliders = Physics.OverlapSphere(transform.position, checkRadius);
        foreach (Collider col in colliders)
        {
            
            if (col.CompareTag("Player")) 
            {
                // Verifica se ele está carregando o cofre
                Vault cofre = col.GetComponentInChildren<Vault>();
                if (cofre != null && cofre.isBeingCarried)
                {
                    // INTROSOS VENCEM!
                    GameManager.Instance?.IntrudersWin();
                    
                    return;
                }
            }
        }
    }

    // Desenha o gizmo no editor para visualizar o raio
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}