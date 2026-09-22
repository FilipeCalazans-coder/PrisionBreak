using UnityEngine;
using TMPro;

/// <summary>
/// Gerencia a pontuacao da corrida atual e salva os dados permanentes
/// (saldo total de moedas e recorde de distancia) usando PlayerPrefs.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private const string TOTAL_COINS_KEY = "TotalSavedCoins";
    private const string HIGH_SCORE_KEY = "HighScoreDistance";

    [Header("Interface da Partida (HUD)")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Referencias do Jogador")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuracoes de Distancia")]
    [SerializeField] private float distanceMultiplier = 1f;

    private int currentCoins = 0;
    private int currentDistance = 0;
    private int defeatedEnemies = 0;
    private float startPositionX = 0f;
    private bool isCounting = true;

    private int totalSavedCoins = 0;
    private int highScoreDistance = 0;

    public int CurrentCoins => currentCoins;
    public int CurrentDistance => currentDistance;
    public int DefeatedEnemies => defeatedEnemies;
    public int TotalSavedCoins => totalSavedCoins;
    public int HighScoreDistance => highScoreDistance;

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

        LoadSavedData();
    }

    private void Start()
    {
        if (playerTransform != null)
        {
            startPositionX = playerTransform.position.x;
        }

        UpdateCoinsUI();
        UpdateDistanceUI();
    }

    private void Update()
    {
        if (!isCounting || playerTransform == null) return;

        float distanceTraveled = (playerTransform.position.x - startPositionX) * distanceMultiplier;
        int calculatedScore = Mathf.Max(0, Mathf.FloorToInt(distanceTraveled));

        if (calculatedScore > currentDistance)
        {
            currentDistance = calculatedScore;
            UpdateDistanceUI();
        }
    }

    private void LoadSavedData()
    {
        totalSavedCoins = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
        highScoreDistance = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }

    public void SaveSessionData()
    {
        totalSavedCoins += currentCoins;
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, totalSavedCoins);

        if (currentDistance > highScoreDistance)
        {
            highScoreDistance = currentDistance;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScoreDistance);
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Desconta moedas do saldo permanente ao comprar upgrades.
    /// </summary>
    public void SpendSavedCoins(int amount)
    {
        totalSavedCoins = Mathf.Max(0, totalSavedCoins - amount);
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, totalSavedCoins);
        PlayerPrefs.Save();
    }

    #region METODOS DE MOEDAS
    public void AddCoins(int amount = 1)
    {
        currentCoins += amount;
        UpdateCoinsUI();
    }

    private void UpdateCoinsUI()
    {
        if (coinsText != null)
        {
            coinsText.text = "Moedas: " + currentCoins.ToString();
        }
    }
    #endregion

    #region METODOS DE DISTANCIA
    private void UpdateDistanceUI()
    {
        if (distanceText != null)
        {
            distanceText.text = $"{currentDistance} m";
        }
    }

    public void StopCounting()
    {
        isCounting = false;
    }
    #endregion

    #region METODOS DE INIMIGOS
    public void AddDefeatedEnemy(int amount = 1)
    {
        defeatedEnemies += amount;
    }
    #endregion
}