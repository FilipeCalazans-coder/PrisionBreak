using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla a geração dinâmica de moedas em formatos visuais,
/// o surgimento de obstáculos e a curva de dificuldade progressiva.
/// Implementa o padrão Singleton para acesso global.
/// </summary>
public class InfiniteProceduralManager : MonoBehaviour
{
    // Instância estática para permitir acesso global fácil (Padrão Singleton)
    public static InfiniteProceduralManager Instance;

    [Header("Referências Principais")]
    [Tooltip("Transform do jogador para calcular distâncias.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configurações de Distância e Posição")]
    [Tooltip("Distância à frente do jogador onde os elementos aparecem.")]
    [SerializeField] private float spawnDistanceAhead = 30f;

    [Tooltip("Altura do chão para alinhar obstáculos terrestres.")]
    [SerializeField] private float groundYPosition = -3f;

    [Header("Dificuldade Progressiva")]
    [Tooltip("Velocidade com que a taxa de geração de obstáculos aumenta com a distância.")]
    [SerializeField] private float difficultyScale = 0.005f;

    [Tooltip("Intervalo mínimo de distância entre obstáculos no nível mais difícil.")]
    [SerializeField] private float minPossibleInterval = 6f;

    [Tooltip("Intervalo inicial padrão entre obstáculos.")]
    [SerializeField] private float initialObstacleInterval = 14f;

    [Header("Tags do ObjectPooler")]
    [Tooltip("Lista de tags cadastradas no ObjectPooler para obstáculos.")]
    [SerializeField] private List<string> obstacleTags;

    [Tooltip("Tag da moeda no ObjectPooler.")]
    [SerializeField] private string coinTag = "Coin";

    // Variáveis internas de controle de fluxo
    private float nextObstacleX = 0f;
    private float nextCoinPatternX = 0f;
    private float currentObstacleInterval;

    // Enum para identificar os tipos de padrões de moedas
    private enum CoinPatternType { Line, JumpArc, Block3x3 }

    private void Awake()
    {
        // Configuração do padrão Singleton
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
        currentObstacleInterval = initialObstacleInterval;

        if (playerTransform != null)
        {
            nextObstacleX = playerTransform.position.x + spawnDistanceAhead;
            nextCoinPatternX = playerTransform.position.x + (spawnDistanceAhead * 0.5f);
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Atualiza a dificuldade progressiva com base na distância percorrida
        UpdateDifficulty();

        // 2. Geração procedural de obstáculos
        if (playerTransform.position.x + spawnDistanceAhead >= nextObstacleX)
        {
            SpawnObstacle();
        }

        // 3. Geração de padrões visuais de moedas
        if (playerTransform.position.x + spawnDistanceAhead >= nextCoinPatternX)
        {
            SpawnRandomCoinPattern();
        }
    }

    /// <summary>
    /// Atualiza dinamicamente as tags de obstáculos quando o cenário/bioma muda.
    /// Chamado pelo BiomeManager.
    /// </summary>
    /// <param name="newObstacleTags">Nova lista de tags de obstáculos do bioma.</param>
    public void UpdateObstacleTags(List<string> newObstacleTags)
    {
        if (newObstacleTags != null && newObstacleTags.Count > 0)
        {
            obstacleTags = newObstacleTags;
        }
    }

    /// <summary>
    /// Ajusta a frequência de obstáculos progressivamente conforme o jogador avança.
    /// </summary>
    private void UpdateDifficulty()
    {
        float distanceTravelled = playerTransform.position.x;
        currentObstacleInterval = Mathf.Max(minPossibleInterval, initialObstacleInterval - (distanceTravelled * difficultyScale));
    }

    /// <summary>
    /// Sorteia e posiciona um obstáculo da piscina de objetos.
    /// </summary>
    private void SpawnObstacle()
    {
        if (obstacleTags == null || obstacleTags.Count == 0) return;

        string selectedTag = obstacleTags[Random.Range(0, obstacleTags.Count)];
        Vector3 spawnPosition = new Vector3(nextObstacleX, groundYPosition, 0f);

        ObjectPooler.Instance.SpawnFromPool(selectedTag, spawnPosition, Quaternion.identity);

        // Define a posição do próximo obstáculo com base no intervalo atual ajustado pela dificuldade
        nextObstacleX += currentObstacleInterval + Random.Range(-1f, 2f);
    }

    /// <summary>
    /// Sorteia um formato/padrão visual de moedas para desenhar no ar ou no chão.
    /// </summary>
    private void SpawnRandomCoinPattern()
    {
        CoinPatternType randomPattern = (CoinPatternType)Random.Range(0, 3);
        float patternLength = 0f;

        switch (randomPattern)
        {
            case CoinPatternType.Line:
                patternLength = SpawnLinePattern();
                break;
            case CoinPatternType.JumpArc:
                patternLength = SpawnJumpArcPattern();
                break;
            case CoinPatternType.Block3x3:
                patternLength = SpawnBlockPattern();
                break;
        }

        // Define o espaço até o próximo grupo de moedas
        nextCoinPatternX += patternLength + Random.Range(12f, 22f);
    }

    // --- PADRÕES DE MOEDAS ---

    private float SpawnLinePattern()
    {
        int count = Random.Range(4, 8);
        float spacing = 1.2f;
        float heightY = groundYPosition + 1.5f;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(nextCoinPatternX + (i * spacing), heightY, 0f);
            ObjectPooler.Instance.SpawnFromPool(coinTag, pos, Quaternion.identity);
        }

        return count * spacing;
    }

    private float SpawnJumpArcPattern()
    {
        int count = 5;
        float spacing = 1.2f;

        // Simula uma curva de arco (parábola de pulo)
        float[] heights = new float[] { 0f, 1.2f, 2.0f, 1.2f, 0f };

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(nextCoinPatternX + (i * spacing), groundYPosition + 1.5f + heights[i], 0f);
            ObjectPooler.Instance.SpawnFromPool(coinTag, pos, Quaternion.identity);
        }

        return count * spacing;
    }

    private float SpawnBlockPattern()
    {
        int columns = 3;
        int rows = 3;
        float spacing = 1.1f;
        float startY = groundYPosition + 1.5f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 pos = new Vector3(nextCoinPatternX + (x * spacing), startY + (y * spacing), 0f);
                ObjectPooler.Instance.SpawnFromPool(coinTag, pos, Quaternion.identity);
            }
        }

        return columns * spacing;
    }
}