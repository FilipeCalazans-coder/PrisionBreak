using UnityEngine;
using UnityEngine.SceneManagement; // Namespace para gerenciar o carregamento de cenas

/// <summary>
/// Gerencia o estado do jogo, alternando entre a partida ativa e a tela de Game Over.
/// </summary>
public class GameManager : MonoBehaviour
{
    // Instância estática para permitir acesso fácil de outros scripts (Padrão Singleton)
    public static GameManager Instance;

    [Header("Interface do Usuário (UI)")]
    [Tooltip("Referência ao GameObject do Painel de Game Over na Canvas.")]
    [SerializeField] private GameObject gameOverPanel;

    // Variável interna para controlar se a partida já terminou
    private bool isGameOver = false;

    private void Awake()
    {
        // Garante a existência de apenas uma instância do GameManager na cena
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
        // Garante que o tempo do jogo comece na velocidade normal (1.0)
        Time.timeScale = 1f;

        // Garante que o painel de Game Over comece desativado
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Pausa o jogo e exibe a tela de derrota quando o jogador colide com um obstáculo.
    /// </summary>
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // Congela o tempo da física e animações do jogo
        Time.timeScale = 0f;

        // Exibe a tela de Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Descongela o tempo e recarrega a cena para reiniciar a partida.
    /// Método vinculado ao botão 'Tentar Novamente' na UI.
    /// </summary>
    public void RestartGame()
    {
        // Restaura a velocidade do tempo antes de recarregar a cena
        Time.timeScale = 1f;

        // Recarrega a cena ativa no momento
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}