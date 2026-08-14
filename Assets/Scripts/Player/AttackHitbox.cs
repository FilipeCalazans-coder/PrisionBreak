using UnityEngine;

/// <summary>
/// Detecta e aplica dano EXCLUSIVAMENTE a inimigos que entram no alcance do golpe.
/// Ignora obstáculos inanimados como espinhos e armadilhas.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("Configurações do Golpe")]
    [Tooltip("Quantidade de dano aplicada a cada acerto.")]
    [SerializeField] private int attackDamage = 1;

    [Tooltip("Tag obrigatória do alvo para receber o dano.")]
    [SerializeField] private string enemyTag = "Enemy";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Filtra rigorosamente: se NÃO for um inimigo, ignora completamente
        if (!other.CompareTag(enemyTag)) return;

        // 2. Busca o componente de vida do inimigo e aplica o dano
        Health enemyHealth = other.GetComponent<Health>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(attackDamage);
            Debug.Log($"Ataque acertou o inimigo: {other.gameObject.name}");
        }
    }
}