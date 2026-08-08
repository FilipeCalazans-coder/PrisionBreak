using UnityEngine;
using TMPro; // Namespace necessário caso utilize o TextMeshPro para a interface

/// <summary>
/// Gerencia a pontuação global da partida, como moedas coletadas e distância.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Instância estática para fácil acesso por outros scripts (Padrão Singleton simples)
    public static ScoreManager Instance;

    [Header("Interface do Usuário (UI)")]
    [Tooltip("Texto da UI (TextMeshPro) que exibirá a quantidade de moedas.")]
    [SerializeField] private TextMeshProUGUI coinsText;

    // Variável interna para guardar a quantidade atual de moedas
    private int currentCoins = 0;

    private void Awake()
    {
        // Garante que exista apenas um ScoreManager na cena
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
        // Inicializa a interface com o valor zero
        UpdateCoinsUI();
    }

    /// <summary>
    /// Adiciona moedas ao contador global e atualiza o texto na tela.
    /// </summary>
    /// <param name="amount">Quantidade de moedas a adicionar (padrão é 1).</param>
    public void AddCoins(int amount = 1)
    {
        currentCoins += amount;
        UpdateCoinsUI();
    }

    /// <summary>
    /// Atualiza o componente de texto da interface com o valor atual de moedas.
    /// </summary>
    private void UpdateCoinsUI()
    {
        if (coinsText != null)
        {
            coinsText.text = "Moedas: " + currentCoins.ToString();
        }
    }
}