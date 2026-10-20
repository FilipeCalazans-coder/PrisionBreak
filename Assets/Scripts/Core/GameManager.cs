using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla o fluxo de telas, exibe dados salvos, gerencia upgrades, o painel de configuracoes e o estado de pausa.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Paineis de Interface (UI)")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject inGameHUD;
    [Tooltip("Painel que aparece quando o jogo é pausado.")]
    [SerializeField] private GameObject pauseMenuPanel;
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
    private bool isPaused = false;

    public bool IsGameStarted => isGameStarted;
    public bool IsGameOver => isGameOver;
    public bool IsPaused => isPaused;

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
        isPaused = false;
        Time.timeScale = 0f;

        UpdateStartMenuUI();

        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (inGameHUD != null) inGameHUD.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
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

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        
        // Se estiver no menu principal, oculta o menu. Se estiver pausado, oculta o menu de pausa.
        if (!isGameStarted && startMenuPanel != null) startMenuPanel.SetActive(false);
        else if (isPaused && pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // Retorna para a tela correta dependendo do estado do jogo
        if (!isGameStarted && startMenuPanel != null) startMenuPanel.SetActive(true);
        else if (isPaused && pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
    }

    #endregion

    #region SISTEMA DE PAUSA

    /// <summary>
    /// Pausa o jogo congelando o TimeScale e exibindo o painel de pausa.
    /// </summary>
    public void PauseGame()
    {
        if (!isGameStarted || isGameOver || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Retoma o jogo descongelando o TimeScale e ocultando o painel de pausa.
    /// </summary>
    public void ResumeGame()
    {
        if (!isGameStarted || isGameOver || !isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Alterna entre pausado e despausado. Útil para atalhos de teclado (ex: tecla ESC).
    /// </summary>
    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    #endregion

    #region UPGRADES E FLUXO DE JOGO

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
        isPaused = false;
        Time.timeScale = 1f;

        if (playerController != null)
        {
            playerController.ApplyUpgrades();
        }
        else
        {
            PlayerController foundPlayer = FindFirstObjectByType<PlayerController>();
            if (foundPlayer != null) foundPlayer.ApplyUpgrades();
        }

        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (inGameHUD != null) inGameHUD.SetActive(true);
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isPaused = false;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopCounting();
            ScoreManager.Instance.SaveSessionData();
        }

        UpdateGameOverUI();
        Time.timeScale = 0f;

        if (inGameHUD != null) inGameHUD.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private void UpdateGameOverUI()
    {
        if (ScoreManager.Instance == null) return;

        if (finalCoinsText != null) finalCoinsText.text = $"Moedas: {ScoreManager.Instance.CurrentCoins}";
        if (finalDistanceText != null) finalDistanceText.text = $"Distancia: {ScoreManager.Instance.CurrentDistance} m";
        if (finalHighScoreText != null) finalHighScoreText.text = $"Recorde: {ScoreManager.Instance.HighScoreDistance} m";
        if (finalEnemiesText != null) finalEnemiesText.text = $"Inimigos Derrotados: {ScoreManager.Instance.DefeatedEnemies}";
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    #endregion
}