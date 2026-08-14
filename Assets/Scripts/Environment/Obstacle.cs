using UnityEngine;

/// <summary>
/// Controla a colisão de obstáculos e inimigos com o jogador.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Configurações de Colisão")]
    [Tooltip("Tag do jogador.")]
    [SerializeField] private string playerTag = "Player";

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            HandlePlayerCollision(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            HandlePlayerCollision(other.gameObject);
        }
    }

    private void HandlePlayerCollision(GameObject playerObj)
    {
        PlayerController player = playerObj.GetComponent<PlayerController>();

        // Se o jogador estiver em Ground Pound, causa dano/destrói o inimigo
        if (player != null && player.IsGroundPounding)
        {
            player.Bounce();

            // Se o inimigo tiver o script Health, aplica dano fatal
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
        else
        {
            // Caso contrário, o jogador morre
            TriggerGameOver();
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