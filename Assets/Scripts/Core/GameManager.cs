using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerencia os estados globais do jogo: Menu Inicial, Partida Ativa e Game Over.
/// Controla o fluxo de tempo e a exibição dos painéis da interface.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Instância estática para acesso global fácil (Padrão Singleton)
    public static GameManager Instance;

    [Header("Painéis de Interface (UI)")]
    [Tooltip("Referência ao GameObject do Painel de Menu Inicial no Canvas.")]
    [SerializeField] private GameObject startMenuPanel;

    [Tooltip("Referência ao GameObject do Painel de Game Over no Canvas.")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Interface Durante a Partida (HUD)")]
    [Tooltip("Objeto que contém a pontuação e moedas durante o jogo (opcional).")]
    [SerializeField] private GameObject inGameHUD;

    // Variáveis internas de controle
    private bool isGameStarted = false;
    private bool isGameOver = false;

    public bool IsGameStarted => isGameStarted;
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        // Garante que só existe um GameManager na cena
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
        // 1. Inicia o jogo no estado de Menu Inicial
        SetupStartMenu();
    }

    /// <summary>
    /// Prepara a tela inicial pausando o jogo até o jogador clicar em Jogar.
    /// </summary>
    private void SetupStartMenu()
    {
        isGameStarted = false;
        isGameOver = false;

        // Congela o tempo para o jogador não correr antes da hora
        Time.timeScale = 0f;

        // Exibe o menu inicial e oculta as outras telas
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (inGameHUD != null) inGameHUD.SetActive(false);
    }

    /// <summary>
    /// Inicia a corrida ao clicar no botão 'Jogar'.
    /// Método vinculado ao botão da UI.
    /// </summary>
    public void StartGame()
    {
        isGameStarted = true;

        // Descongela a física e o movimento do jogo
        Time.timeScale = 1f;

        // Oculta o menu e exibe a HUD de pontos/moedas
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (inGameHUD != null) inGameHUD.SetActive(true);
    }

    /// <summary>
    /// Pausa o jogo e exibe a tela de derrota quando o jogador colide.
    /// </summary>
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Congela o tempo da física
        Time.timeScale = 0f;

        // Oculta o HUD e exibe o Game Over
        if (inGameHUD != null) inGameHUD.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    /// <summary>
    /// Reinicia a partida recarregando a cena ativa.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}