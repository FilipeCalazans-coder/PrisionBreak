using UnityEngine;

/// <summary>
/// Gerencia os pontos de vida e o recebimento de dano de objetos e inimigos.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Configuracoes de Vida")]
    [Tooltip("Quantidade maxima e inicial de vida do inimigo.")]
    [SerializeField] private int maxHealth = 1;

    private int currentHealth;

    private void OnEnable()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Aplica dano ao inimigo e verifica se ele deve ser destruido.
    /// </summary>
    /// <param name="damageAmount">Quantidade de dano a subtrair.</param>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Executa a morte do inimigo, notifica o ScoreManager e desativa o GameObject para o Pooler.
    /// </summary>
    private void Die()
    {
        // Notifica o ScoreManager que um inimigo foi derrotado
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddDefeatedEnemy(1);
        }

        gameObject.SetActive(false);
    }
}