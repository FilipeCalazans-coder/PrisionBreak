using System.Collections;
using UnityEngine;

/// <summary>
/// Define a forma como o inimigo foi derrotado para aplicar a deformação correta.
/// </summary>
public enum DeathType
{
    Punch, // Soco (estica verticalmente)
    Stomp  // Pisão ou Ground Pound (estica horizontalmente)
}

/// <summary>
/// Gerencia a vida do inimigo, instancia partículas e executa o efeito de gamefeel
/// antes de devolver o objeto ao ObjectPooler.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Configurações de Vida")]
    [Tooltip("Quantidade máxima e inicial de vida.")]
    [SerializeField] private int maxHealth = 1;

    [Header("Efeitos Visuais de Morte")]
    [Tooltip("Prefab do sistema de partículas a ser emitido ao morrer.")]
    [SerializeField] private GameObject deathParticlesPrefab;

    [Header("Configurações de Gamefeel na Morte")]
    [Tooltip("Duração da animação de deformação antes de desativar o objeto.")]
    [SerializeField] private float deathDuration = 0.12f;

    [Tooltip("Deformação ao levar um soco (X = encolhe, Y = estica verticalmente).")]
    [SerializeField] private Vector3 punchSquashScale = new Vector3(0.5f, 1.8f, 1f);

    [Tooltip("Deformação ao levar pisão/ground pound (X = estica horizontalmente, Y = esmaga).")]
    [SerializeField] private Vector3 stompSquashScale = new Vector3(1.8f, 0.3f, 1f);

    private int currentHealth;
    private bool isDying = false;

    // Cache de componentes originais
    private SpriteRenderer spriteRenderer;
    private Collider2D col2D;
    private Vector3 originalScale;
    private Color originalColor;
    private Material originalMaterial;

    private static Material whiteFlashMaterial;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col2D = GetComponent<Collider2D>();
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalMaterial = spriteRenderer.material;
        }

        if (whiteFlashMaterial == null)
        {
            Shader flashShader = Shader.Find("GUI/Text Shader");
            if (flashShader != null)
            {
                whiteFlashMaterial = new Material(flashShader);
            }
        }
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDying = false;
        transform.localScale = originalScale;

        if (col2D != null)
        {
            col2D.enabled = true;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.material = originalMaterial;
        }
    }

    public void TakeDamage(int damageAmount, DeathType deathType = DeathType.Punch)
    {
        if (isDying) return;

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die(deathType);
        }
    }

    public void Die(DeathType deathType = DeathType.Punch)
    {
        if (isDying) return;
        isDying = true;

        // 1. Desativa a colisão imediatamente
        if (col2D != null)
        {
            col2D.enabled = false;
        }

        // 2. Dispara a explosão de partículas brancas
        SpawnDeathParticles();

        // 3. Notifica o sistema de pontuação
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddDefeatedEnemy(1);
        }

        // 4. Executa o efeito de Squash & Stretch
        StartCoroutine(DeathFeedbackRoutine(deathType));
    }

    private void SpawnDeathParticles()
    {
        if (deathParticlesPrefab != null)
        {
            Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
        }
    }

    private IEnumerator DeathFeedbackRoutine(DeathType deathType)
    {
        if (spriteRenderer != null)
        {
            if (whiteFlashMaterial != null)
            {
                spriteRenderer.material = whiteFlashMaterial;
            }
            spriteRenderer.color = Color.white;
        }

        Vector3 targetScale;
        if (deathType == DeathType.Punch)
        {
            targetScale = new Vector3(
                originalScale.x * punchSquashScale.x,
                originalScale.y * punchSquashScale.y,
                originalScale.z
            );
        }
        else
        {
            targetScale = new Vector3(
                originalScale.x * stompSquashScale.x,
                originalScale.y * stompSquashScale.y,
                originalScale.z
            );
        }

        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathDuration;

            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.material = originalMaterial;
            spriteRenderer.color = originalColor;
        }

        gameObject.SetActive(false);
    }
}