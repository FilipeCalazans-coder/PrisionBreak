using UnityEngine;

/// <summary>
/// Gerencia a colisão entre o jogador e obstáculos/inimigos.
/// Dispara a derrota do inimigo por Stomp ou Ground Pound,
/// acionando a animação de impacto e o quique (bounce).
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Configurações de Colisão")]
    [Tooltip("Tag atribuída ao GameObject do jogador.")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Define se este obstáculo é um inimigo que pode ser derrotado ao pular em cima dele.")]
    [SerializeField] private bool canBeStomped = true;
    [Tooltip("Tolerância de altura para considerar que o jogador pisou por cima (Offset Y).")]
    [SerializeField] private float stompThreshold = 0.2f;

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
    /// Avalia o contato entre o jogador e o inimigo/obstáculo.
    /// </summary>
    private void HandleCollision(GameObject playerObj)
    {
        PlayerController player = playerObj.GetComponent<PlayerController>();
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (player == null) return;

        // 1. Caso: Ground Pound ativo
        if (player.IsGroundPounding)
        {
            // Dispara a animação de impacto ao acertar o inimigo
            player.TriggerGroundPoundImpact();
            DefeatObstacle(player);
            return;
        }

        // 2. Caso: Stomp normal (caindo em cima da cabeça do inimigo)
        if (canBeStomped)
        {
            bool isFalling = playerRb != null && playerRb.linearVelocity.y < 0.1f;
            bool isAbove = playerObj.transform.position.y > (transform.position.y + stompThreshold);

            if (isFalling && isAbove)
            {
                DefeatObstacle(player);
                return;
            }
        }

        // 3. Caso contrário: Dano frontal/lateral -> Derrota do jogador
        TriggerGameOver();
    }

    /// <summary>
    /// Elimina o inimigo e aplica o impulso vertical (Bounce) ao jogador.
    /// </summary>
    private void DefeatObstacle(PlayerController player)
    {
        player.Bounce();

        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(999);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void TriggerGameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}