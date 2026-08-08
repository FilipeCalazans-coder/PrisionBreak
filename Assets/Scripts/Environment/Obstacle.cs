using UnityEngine;

/// <summary>
/// Controla a detecção de colisão dos obstáculos mortais com o jogador.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Configurações do Obstáculo")]
    [Tooltip("Tag atribuída ao GameObject do jogador para confirmar a colisão.")]
    [SerializeField] private string playerTag = "Player";

    /// <summary>
    /// Método disparado pela Unity quando outro objeto colide fisicamente com este obstáculo.
    /// </summary>
    /// <param name="collision">Dados referentes à colisão 2D.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Verifica se o objeto que colidiu tem a tag de jogador
        if (collision.gameObject.CompareTag(playerTag))
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Método alternativo caso o colisor do obstáculo esteja marcado como 'Is Trigger'.
    /// </summary>
    /// <param name="other">O colisor 2D que entrou na área do trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// Aciona o evento de Game Over no gerenciador principal do jogo.
    /// </summary>
    private void TriggerGameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}