using UnityEngine;

/// <summary>
/// Gerencia os pontos de vida e o recebimento de dano de objetos e inimigos.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [Tooltip("Quantidade máxima e inicial de vida do inimigo.")]
    [SerializeField] private int maxHealth = 1;

    // Vida atual em tempo de execução
    private int currentHealth;

    private void OnEnable()
    {
        // Reseta a vida para o valor máximo sempre que o inimigo for ativado pelo ObjectPooler
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Aplica dano ao inimigo e verifica se ele deve ser destruído.
    /// </summary>
    /// <param name="damageAmount">Quantidade de dano a subtrair.</param>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{gameObject.name} recebeu {damageAmount} de dano! Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Executa a morte do inimigo (desativa o objeto para o Object Pooler).
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} foi derrotado!");
        gameObject.SetActive(false);
    }
}