using UnityEngine;

/// <summary>
/// Gerencia a colisao entre o jogador e obstaculos/inimigos.
/// Agora os inimigos so sao derrotados de cima quando o jogador executa o Ground Pound.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Configuracoes de Colisao")]
    [Tooltip("Tag para identificar o jogador.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Dano Causado ao Jogador")]
    [Tooltip("Dano aplicado ao jogador caso colida sem usar Ground Pound.")]
    [SerializeField] private int damageToPlayer = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            HandleCollision(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            HandleCollision(other.gameObject);
        }
    }

    /// <summary>
    /// Processa a interacao entre o jogador e o inimigo/obstaculo.
    /// </summary>
    /// <param name="playerObj">GameObject do jogador envolvido no contato.</param>
    private void HandleCollision(GameObject playerObj)
    {
        PlayerController player = playerObj.GetComponent<PlayerController>();
        if (player == null) return;

        // 1. Caso exclusivo de derrota por cima: Ground Pound ativo
        if (player.IsGroundPounding)
        {
            // Conclui o impacto no solo/alvo e gera a resposta visual do jogador
            player.TriggerGroundPoundImpact();
            
            // Elimina o obstaculo aplicando a deformacao de esmagamento (Stomp)
            DefeatObstacle(player, DeathType.Stomp);
            return;
        }

        // 2. Qualquer outro contato (incluindo cair em cima com pulo normal) causa dano ao jogador
        player.TakeDamage(damageToPlayer);
    }

    /// <summary>
    /// Faz o jogador quicar e causa dano letal ao inimigo com feedback de gamefeel.
    /// </summary>
    private void DefeatObstacle(PlayerController player, DeathType deathType)
    {
        // Impulsiona o jogador para cima apos o acerto
        player.Bounce();

        Health health = GetComponent<Health>();
        if (health != null)
        {
            // Aplica dano total acionando a deformacao visual configurada no Health
            health.TakeDamage(999, deathType);
        }
        else
        {
            // Se nao tiver o script Health, apenas desativa o objeto no pool
            gameObject.SetActive(false);
        }
    }
}