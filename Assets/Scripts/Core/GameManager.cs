using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controla o fluxo de telas, exibe dados salvos e gerencia os botoes de upgrade no Menu Inicial.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Paineis de Interface (UI)")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject inGameHUD;

    [Header("Textos do Menu Inicial")]
    [SerializeField] private TextMeshProUGUI startMenuTotalCoinsText;
    [Tooltip("Texto do botao/painel de upgrade de vida.")]
    [SerializeField] private TextMeshProUGUI healthUpgradeButtonText;
    [Tooltip("Texto do botao/painel de upgrade de dano.")]
    [SerializeField] private TextMeshProUGUI damageUpgradeButtonText;

    [Header("Textos do Painel de Game Over")]
    [SerializeField] private TextMeshProUGUI finalCoinsText;
    [SerializeField] private TextMeshProUGUI finalDistanceText;
    [SerializeField] private TextMeshProUGUI finalHighScoreText;
    [SerializeField] private TextMeshProUGUI finalEnemiesText;

    private bool isGameStarted = false;
    private bool isGameOver = false;

    public bool IsGameStarted => isGameStarted;
    public bool IsGameOver => isGameOver;

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
    }

    private void Start()
    {
        SetupStartMenu();
    }

    private void SetupStartMenu()
    {
        isGameStarted = false;
        isGameOver = false;
        Time.timeScale = 0f;

        UpdateStartMenuUI();

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (inGameHUD != null) inGameHUD.SetActive(false);
    }

    /// <summary>
    /// Atualiza todos os textos do menu inicial (moedas, vida e dano).
    /// </summary>
    public void UpdateStartMenuUI()
    {
        if (ScoreManager.Instance != null && startMenuTotalCoinsText != null)
        {
            startMenuTotalCoinsText.text = $"Moedas: {ScoreManager.Instance.TotalSavedCoins}";
        }

        if (UpgradeManager.Instance != null)
        {
            if (healthUpgradeButtonText != null)
            {
                healthUpgradeButtonText.text = $"Vida: {UpgradeManager.Instance.CurrentHealth} (Nv.{UpgradeManager.Instance.CurrentHealthLevel})\nCusto: {UpgradeManager.Instance.GetHealthUpgradeCost()}";
            }

            if (damageUpgradeButtonText != null)
            {
                damageUpgradeButtonText.text = $"Dano: {UpgradeManager.Instance.CurrentDamage} (Nv.{UpgradeManager.Instance.CurrentDamageLevel})\nCusto: {UpgradeManager.Instance.GetDamageUpgradeCost()}";
            }
        }
    }

    /// <summary>
    /// Metodo chamado pelo botao de upgrade de Vida na UI.
    /// </summary>
    public void BuyHealthUpgrade()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeHealth())
        {
            UpdateStartMenuUI();
        }
    }

    /// <summary>
    /// Metodo chamado pelo botao de upgrade de Dano na UI.
    /// </summary>
    public void BuyDamageUpgrade()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeDamage())
        {
            UpdateStartMenuUI();
        }
    }

    public void StartGame()
    {
        isGameStarted = true;
        Time.timeScale = 1f;

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (inGameHUD != null) inGameHUD.SetActive(true);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopCounting();
            ScoreManager.Instance.SaveSessionData();
        }

        UpdateGameOverUI();

        Time.timeScale = 0f;

        if (inGameHUD != null) inGameHUD.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private void UpdateGameOverUI()
    {
        if (ScoreManager.Instance == null) return;

        if (finalCoinsText != null)
            finalCoinsText.text = $"Moedas: {ScoreManager.Instance.CurrentCoins}";

        if (finalDistanceText != null)
            finalDistanceText.text = $"Distancia: {ScoreManager.Instance.CurrentDistance} m";

        if (finalHighScoreText != null)
            finalHighScoreText.text = $"Recorde: {ScoreManager.Instance.HighScoreDistance} m";

        if (finalEnemiesText != null)
            finalEnemiesText.text = $"Inimigos Derrotados: {ScoreManager.Instance.DefeatedEnemies}";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}