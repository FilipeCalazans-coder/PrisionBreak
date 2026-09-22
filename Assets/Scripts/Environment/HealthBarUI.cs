using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a exibição visual da barra de vida do jogador na HUD.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    public static HealthBarUI Instance;

    [Header("Referência da Barra")]
    [Tooltip("Imagem da barra com o Image Type configurado como Filled ou um Slider.")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Slider healthSlider;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Atualiza a proporção da barra de vida com base na vida atual e máxima.
    /// </summary>
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        float fillRatio = (float)currentHealth / Mathf.Max(1, maxHealth);

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = fillRatio;
        }

        if (healthSlider != null)
        {
            healthSlider.value = fillRatio;
        }
    }
}