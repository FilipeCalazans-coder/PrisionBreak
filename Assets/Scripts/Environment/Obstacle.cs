using UnityEngine;

/// <summary>
/// Gerencia a colisao entre o jogador e obstaculos/inimigos,
/// direcionando o dano ou ativando a derrota com deformacao por Stomp.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Configuracoes de Colisao")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool canBeStomped = true;
    [SerializeField] private float stompThreshold = 0.2f;

    [Header("Dano Causado")]
    [Tooltip("Dano aplicado ao jogador caso ele colida de frente com o obstaculo.")]
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

    private void HandleCollision(GameObject playerObj)
    {
        PlayerController player = playerObj.GetComponent<PlayerController>();
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (player == null) return;

        // 1. Caso: Ground Pound do Jogador
        if (player.IsGroundPounding)
        {
            player.TriggerGroundPoundImpact();
            DefeatObstacle(player, DeathType.Stomp);
            return;
        }

        // 2. Caso: Pisar na cabeca (Stomp comum)
        if (canBeStomped)
        {
            bool isFalling = playerRb != null && playerRb.linearVelocity.y < 0.1f;
            bool isAbove = playerObj.transform.position.y > (transform.position.y + stompThreshold);

            if (isFalling && isAbove)
            {
                DefeatObstacle(player, DeathType.Stomp);
                return;
            }
        }

        // 3. Caso: Dano frontal/lateral sofrido pelo jogador
        player.TakeDamage(damageToPlayer);
    }

    private void DefeatObstacle(PlayerController player, DeathType deathType)
    {
        player.Bounce();

        Health health = GetComponent<Health>();
        if (health != null)
        {
            // Aplica dano letal acionando a deformacao especifica de pisao
            health.TakeDamage(999, deathType);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}