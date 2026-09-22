using UnityEngine;

/// <summary>
/// Gerencia os niveis de atributos (Vida e Dano), custos de evolucao
/// e persistencia dos upgrades usando PlayerPrefs.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    // Chaves para salvar no PlayerPrefs
    private const string HEALTH_LEVEL_KEY = "PlayerHealthLevel";
    private const string DAMAGE_LEVEL_KEY = "PlayerDamageLevel";

    [Header("Configuracao de Vida")]
    [Tooltip("Vida base no nivel 1.")]
    [SerializeField] private int baseHealth = 1;
    [Tooltip("Custo base em moedas para subir o primeiro nivel de vida.")]
    [SerializeField] private int baseHealthCost = 10;
    [Tooltip("Aumento de custo a cada nivel comprado.")]
    [SerializeField] private float healthCostMultiplier = 1.5f;

    [Header("Configuracao de Dano")]
    [Tooltip("Dano base no nivel 1.")]
    [SerializeField] private int baseDamage = 1;
    [Tooltip("Custo base em moedas para subir o primeiro nivel de dano.")]
    [SerializeField] private int baseDamageCost = 15;
    [Tooltip("Aumento de custo a cada nivel comprado.")]
    [SerializeField] private float damageCostMultiplier = 1.5f;

    // Niveis atuais
    private int currentHealthLevel = 1;
    private int currentDamageLevel = 1;

    public int CurrentHealth => baseHealth + (currentHealthLevel - 1);
    public int CurrentDamage => baseDamage + (currentDamageLevel - 1);
    public int CurrentHealthLevel => currentHealthLevel;
    public int CurrentDamageLevel => currentDamageLevel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadUpgrades();
    }

    private void LoadUpgrades()
    {
        currentHealthLevel = PlayerPrefs.GetInt(HEALTH_LEVEL_KEY, 1);
        currentDamageLevel = PlayerPrefs.GetInt(DAMAGE_LEVEL_KEY, 1);
    }

    /// <summary>
    /// Calcula o preco do proximo nivel de vida.
    /// </summary>
    public int GetHealthUpgradeCost()
    {
        return Mathf.RoundToInt(baseHealthCost * Mathf.Pow(healthCostMultiplier, currentHealthLevel - 1));
    }

    /// <summary>
    /// Calcula o preco do proximo nivel de dano.
    /// </summary>
    public int GetDamageUpgradeCost()
    {
        return Mathf.RoundToInt(baseDamageCost * Mathf.Pow(damageCostMultiplier, currentDamageLevel - 1));
    }

    /// <summary>
    /// Tenta comprar o upgrade de vida se o jogador tiver moedas suficientes.
    /// </summary>
    public bool TryUpgradeHealth()
    {
        int cost = GetHealthUpgradeCost();
        if (ScoreManager.Instance != null && ScoreManager.Instance.TotalSavedCoins >= cost)
        {
            ScoreManager.Instance.SpendSavedCoins(cost);
            currentHealthLevel++;
            PlayerPrefs.SetInt(HEALTH_LEVEL_KEY, currentHealthLevel);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tenta comprar o upgrade de dano se o jogador tiver moedas suficientes.
    /// </summary>
    public bool TryUpgradeDamage()
    {
        int cost = GetDamageUpgradeCost();
        if (ScoreManager.Instance != null && ScoreManager.Instance.TotalSavedCoins >= cost)
        {
            ScoreManager.Instance.SpendSavedCoins(cost);
            currentDamageLevel++;
            PlayerPrefs.SetInt(DAMAGE_LEVEL_KEY, currentDamageLevel);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }
}