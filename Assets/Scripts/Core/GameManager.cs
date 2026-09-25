using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Necessario para interagir com Sliders
using TMPro;

/// <summary>
/// Controla o fluxo de telas, exibe dados salvos, gerencia upgrades e o painel de configuracoes.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Paineis de Interface (UI)")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject inGameHUD;
    [Tooltip("Painel de Configuracoes de Som")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Configuracoes de Som (UI)")]
    [Tooltip("Slider responsavel pelo volume da Musica")]
    [SerializeField] private Slider musicVolumeSlider;
    [Tooltip("Slider responsavel pelo volume dos Efeitos Sonoros")]
    [SerializeField] private Slider sfxVolumeSlider;

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

    [Header("Referência do Jogador")]
    [SerializeField] private PlayerController playerController;

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
        SetupAudioSliders();
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
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Inicializa a posicao dos Sliders com base nos volumes salvos no AudioManager.
    /// </summary>
    private void SetupAudioSliders()
    {
        if (AudioManager.Instance != null)
        {
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXSliderChanged);
            }
        }
    }

    #region CONTROLE DE TELAS & CONFIGURACOES

    /// <summary>
    /// Abre o painel de configuracoes e oculta o menu principal.
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Fecha o painel de configuracoes e retorna ao menu principal.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Callback disparado quando o jogador move o Slider de Musica.
    /// </summary>
    public void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    /// <summary>
    /// Callback disparado quando o jogador move o Slider de SFX.
    /// </summary>
    public void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    #endregion

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

    public void BuyHealthUpgrade()
    {
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.TryUpgradeHealth())
        {
            UpdateStartMenuUI();
        }
    }

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

        // Garante que o jogador inicia com os upgrades mais recentes comprados no menu
        if (playerController != null)
        {
            playerController.ApplyUpgrades();
        }
        else
        {
            // Tenta localizar automaticamente caso não tenha sido arrastado no Inspector
            PlayerController foundPlayer = FindFirstObjectByType<PlayerController>();
            if (foundPlayer != null)
            {
                foundPlayer.ApplyUpgrades();
            }
        }

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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