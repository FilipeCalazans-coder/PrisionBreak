using UnityEngine;

/// <summary>
/// Aplica dano ao inimigo e aciona a animacao de soco caso o golpe seja fatal.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    [Header("Configuracoes do Golpe")]
    [Tooltip("Dano padrao caso o UpgradeManager nao esteja ativo.")]
    [SerializeField] private int fallbackDamage = 1;

    [Tooltip("Tag obrigatoria do alvo para receber o dano.")]
    [SerializeField] private string enemyTag = "Enemy";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag)) return;

        Health enemyHealth = other.GetComponent<Health>();
        if (enemyHealth != null)
        {
            int finalDamage = (UpgradeManager.Instance != null) ? UpgradeManager.Instance.CurrentDamage : fallbackDamage;

            // Passa o dano informando que a origem e um Soco (DeathType.Punch)
            enemyHealth.TakeDamage(finalDamage, DeathType.Punch);

            // Dispara tremor na tela
            if (ScreenDamageFX.Instance != null)
            {
                ScreenDamageFX.Instance.TriggerShake(0.12f, 0.15f);
            }

            Debug.Log($"Ataque acertou {other.gameObject.name} causando {finalDamage} de dano!");
        }
    }
}