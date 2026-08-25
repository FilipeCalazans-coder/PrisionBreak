using UnityEngine;

/// <summary>
/// Gerencia a colisão entre o jogador e obstáculos/inimigos.
/// Permite esmagar inimigos ao cair sobre a cabeça deles (pulo tradicional de plataforma)
/// ou ao atingi-los com o Ground Pound.
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
            HandleCollision(collision.gameObject, collision);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            HandleCollision(other.gameObject, null);
        }
    }

    /// <summary>
    /// Avalia se o contato resulta no esmagamento do inimigo ou na derrota do jogador.
    /// </summary>
    private void HandleCollision(GameObject playerObj, Collision2D collision)
    {
        PlayerController player = playerObj.GetComponent<PlayerController>();
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (player == null) return;

        // 1. Caso: Ground Pound ativo (destrói o obstáculo/inimigo imediatamente)
        if (player.IsGroundPounding)
        {
            DefeatObstacle(player);
            return;
        }

        // 2. Caso: Pulo/Queda sobre a cabeça do inimigo (Stomp clássico de plataforma)
        if (canBeStomped)
        {
            // O jogador precisa estar caindo (velocidade vertical negativa)
            bool isFalling = playerRb != null && playerRb.linearVelocity.y < 0.1f;

            // A base do jogador precisa estar acima do centro do inimigo
            bool isAbove = playerObj.transform.position.y > (transform.position.y + stompThreshold);

            if (isFalling && isAbove)
            {
                DefeatObstacle(player);
                return;
            }
        }

        // 3. Caso contrário: Colisão frontal ou lateral, derrota o jogador
        TriggerGameOver();
    }

    /// <summary>
    /// Elimina o inimigo e concede o impulso vertical ao jogador.
    /// </summary>
    private void DefeatObstacle(PlayerController player)
    {
        // Aplica o pulo de resposta no jogador
        player.Bounce();

        // Se o objeto possuir o componente Health, aplica o dano
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(999);
        }
        else
        {
            // Caso seja um objeto sem Health, desativa para o Object Pooler
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Aciona a tela de Game Over caso o jogador seja derrotado.
    /// </summary>
    private void TriggerGameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}