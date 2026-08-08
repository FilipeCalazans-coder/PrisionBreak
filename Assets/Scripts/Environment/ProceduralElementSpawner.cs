using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a criação procedural e infinita de obstáculos e moedas sem a necessidade de Chunks pré-fabricados.
/// </summary>
public class ProceduralElementSpawner : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Transform do jogador para monitorar a posição de spawn à frente.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configurações de Posição")]
    [Tooltip("Distância à frente do jogador onde os elementos vão reaparecer na tela.")]
    [SerializeField] private float spawnDistanceAhead = 25f;

    [Tooltip("Altura do chão para alinhar os obstáculos terrestres (Eixo Y).")]
    [SerializeField] private float groundYPosition = -3f;

    [Header("Geração de Obstáculos")]
    [Tooltip("Tags cadastradas no ObjectPooler correspondentes aos obstáculos.")]
    [SerializeField] private List<string> obstacleTags;

    [Tooltip("Intervalo mínimo de distância entre um obstáculo e outro.")]
    [SerializeField] private float minObstacleInterval = 8f;

    [Tooltip("Intervalo máximo de distância entre um obstáculo e outro.")]
    [SerializeField] private float maxObstacleInterval = 15f;

    [Header("Geração de Moedas")]
    [Tooltip("Tag da moeda cadastrada no ObjectPooler.")]
    [SerializeField] private string coinTag = "Coin";

    [Tooltip("Altura mínima e máxima que as moedas podem aparecer no ar.")]
    [SerializeField] private float minCoinY = -2f;
    [SerializeField] private float maxCoinY = 2f;

    // Controle interno de posições do próximo spawn
    private float nextObstacleX = 0f;
    private float nextCoinX = 0f;

    private void Start()
    {
        if (playerTransform != null)
        {
            // Define o ponto inicial de geração à frente do jogador
            nextObstacleX = playerTransform.position.x + spawnDistanceAhead;
            nextCoinX = playerTransform.position.x + spawnDistanceAhead / 2f;
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Verifica se está na hora de gerar um novo obstáculo
        if (playerTransform.position.x + spawnDistanceAhead >= nextObstacleX)
        {
            SpawnDynamicObstacle();
        }

        // 2. Verifica se está na hora de gerar uma nova fileira de moedas
        if (playerTransform.position.x + spawnDistanceAhead >= nextCoinX)
        {
            SpawnCoinPattern();
        }
    }

    /// <summary>
    /// Sorteia um obstáculo da lista e o posiciona na tela via ObjectPooler.
    /// </summary>
    private void SpawnDynamicObstacle()
    {
        if (obstacleTags == null || obstacleTags.Count == 0) return;

        // Sorteia um obstáculo aleatório da lista
        string selectedTag = obstacleTags[Random.Range(0, obstacleTags.Count)];

        // Posição de spawn no eixo X acumulado e Y alinhado ao chão
        Vector3 spawnPos = new Vector3(nextObstacleX, groundYPosition, 0f);

        // Solicita o objeto à piscina
        ObjectPooler.Instance.SpawnFromPool(selectedTag, spawnPos, Quaternion.identity);

        // Define a distância até o próximo obstáculo de forma aleatória
        float randomInterval = Random.Range(minObstacleInterval, maxObstacleInterval);
        nextObstacleX += randomInterval;
    }

    /// <summary>
    /// Cria uma fileira horizontal de moedas flutuantes no ar.
    /// </summary>
    private void SpawnCoinPattern()
    {
        // Sorteia a quantidade de moedas na fileira (ex: entre 3 e 6 moedas)
        int coinCount = Random.Range(3, 7);
        
        // Sorteia a altura da fileira de moedas
        float randomY = Random.Range(minCoinY, maxCoinY);

        // Distância entre cada moeda da fileira
        float coinSpacing = 1.2f;

        for (int i = 0; i < coinCount; i++)
        {
            Vector3 spawnPos = new Vector3(nextCoinX + (i * coinSpacing), randomY, 0f);
            ObjectPooler.Instance.SpawnFromPool(coinTag, spawnPos, Quaternion.identity);
        }

        // Avança a posição para o próximo grupo de moedas
        nextCoinX += (coinCount * coinSpacing) + Random.Range(10f, 20f);
    }
}