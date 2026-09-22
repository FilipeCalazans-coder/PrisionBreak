using UnityEngine;

/// <summary>
/// Aplica dano a inimigos considerando o nivel de dano do UpgradeManager
/// e dispara um tremor leve na câmara ao conectar o golpe.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("Configurações do Golpe")]
    [Tooltip("Dano padrão caso o UpgradeManager não esteja ativo.")]
    [SerializeField] private int fallbackDamage = 1;
    [Tooltip("Tag obrigatória do alvo para receber o dano.")]
    [SerializeField] private string enemyTag = "Enemy";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag)) return;

        Health enemyHealth = other.GetComponent<Health>();
        if (enemyHealth != null)
        {
            int finalDamage = (UpgradeManager.Instance != null) ? UpgradeManager.Instance.CurrentDamage : fallbackDamage;
            enemyHealth.TakeDamage(finalDamage);

            // Dispara um tremor curto e leve para dar peso ao soco!
            if (ScreenDamageFX.Instance != null)
            {
                ScreenDamageFX.Instance.TriggerShake(0.12f, 0.15f);
            }

            Debug.Log($"Ataque acertou {other.gameObject.name} causando {finalDamage} de dano!");
        }
    }
}