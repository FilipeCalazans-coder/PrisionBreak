using UnityEngine;
using TMPro; // Namespace necessário para manipular componentes TextMeshProUGUI

/// <summary>
/// Gerencia a pontuação global da partida, combinando a contagem de moedas coletadas
/// e a distância percorrida em metros pelo jogador.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Instância estática para fácil acesso global por outros scripts (Padrão Singleton)
    public static ScoreManager Instance;

    [Header("Interface do Usuário (UI)")]
    [Tooltip("Texto da UI (TextMeshPro) que exibirá a quantidade de moedas coletadas.")]
    [SerializeField] private TextMeshProUGUI coinsText;

    [Tooltip("Texto da UI (TextMeshPro) que exibirá a distância percorrida em metros.")]
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Referências do Jogador")]
    [Tooltip("Transform do jogador para calcular o avanço horizontal.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configurações de Distância")]
    [Tooltip("Multiplicador de escala da distância em metros.")]
    [SerializeField] private float distanceMultiplier = 1f;

    // Variáveis internas de estado
    private int currentCoins = 0;
    private int currentDistance = 0;
    private float startPositionX = 0f;
    private bool isCounting = true;

    // Propriedades públicas para leitura externa
    public int CurrentCoins => currentCoins;
    public int CurrentDistance => currentDistance;

    private void Awake()
    {
        // Garante que exista apenas uma instância ativa do ScoreManager na cena
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
        if (playerTransform != null)
        {
            // Salva a posição horizontal X onde o jogador inicia a corrida
            startPositionX = playerTransform.position.x;
        }

        // Inicializa as duas interfaces no início da partida
        UpdateCoinsUI();
        UpdateDistanceUI();
    }

    private void Update()
    {
        if (!isCounting || playerTransform == null) return;

        // 1. Calcula a distância percorrida no eixo X
        float distanceTraveled = (playerTransform.position.x - startPositionX) * distanceMultiplier;

        // 2. Converte para inteiro garantindo que a pontuação não diminua
        int calculatedScore = Mathf.Max(0, Mathf.FloorToInt(distanceTraveled));

        if (calculatedScore > currentDistance)
        {
            currentDistance = calculatedScore;
            UpdateDistanceUI();
        }
    }

    #region MÉTODOS DE MOEDAS

    /// <summary>
    /// Adiciona moedas ao contador global e atualiza o texto na interface.
    /// </summary>
    /// <param name="amount">Quantidade de moedas a adicionar (padrão é 1).</param>
    public void AddCoins(int amount = 1)
    {
        currentCoins += amount;
        UpdateCoinsUI();
    }

    /// <summary>
    /// Atualiza o componente de texto da UI com o valor atual de moedas.
    /// </summary>
    private void UpdateCoinsUI()
    {
        if (coinsText != null)
        {
            coinsText.text = "Moedas: " + currentCoins.ToString();
        }
    }

    #endregion

    #region MÉTODOS DE DISTÂNCIA

    /// <summary>
    /// Atualiza o componente de texto da UI com a distância atual percorrida.
    /// </summary>
    private void UpdateDistanceUI()
    {
        if (distanceText != null)
        {
            distanceText.text = $"{currentDistance} m";
        }
    }

    /// <summary>
    /// Pausa a contagem de metros (chamado no Game Over).
    /// </summary>
    public void StopCounting()
    {
        isCounting = false;
    }

    /// <summary>
    /// Reinicia a pontuação e os metros para uma nova partida.
    /// </summary>
    public void ResetScore()
    {
        if (playerTransform != null)
        {
            startPositionX = playerTransform.position.x;
        }

        currentCoins = 0;
        currentDistance = 0;
        isCounting = true;

        UpdateCoinsUI();
        UpdateDistanceUI();
    }

    #endregion
}