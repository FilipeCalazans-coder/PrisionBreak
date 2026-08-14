using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a criação procedural de Chunks, garantindo um bloco inicial seguro
/// e a integração contínua com o BiomeManager.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    [Header("Referências Principais")]
    [Tooltip("Transform do jogador para monitorar a posição de avanço.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuração do Bloco Inicial")]
    [Tooltip("Tag no ObjectPooler correspondente ao Chunk inicial seguro (sem obstáculos/buracos).")]
    [SerializeField] private string startingChunkTag = "StartingChunk";

    [Header("Configurações de Encaixe e Tamanho")]
    [Tooltip("Largura exata de cada bloco (Chunk) no eixo X.")]
    [SerializeField] private float chunkWidth = 20f;

    [Tooltip("Quantidade de blocos visíveis na tela simultaneamente.")]
    [SerializeField] private int initialChunksCount = 5;

    [Tooltip("Distância à frente do jogador para acionar a criação do próximo bloco.")]
    [SerializeField] private float spawnDistanceThreshold = 30f;

    // Posição matemática exata onde o próximo bloco deve ser colocado
    private Vector3 nextSpawnPosition = Vector3.zero;

    // Fila para gerenciar os blocos ativos na cena e permitir reciclagem
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    private void Start()
    {
        nextSpawnPosition = Vector3.zero;

        // 1. Gera obrigatoriamente o bloco inicial seguro no ponto zero
        SpawnSpecificChunk(startingChunkTag);

        // 2. Preenche o restante da tela com blocos aleatórios do bioma ativo
        for (int i = 1; i < initialChunksCount; i++)
        {
            SpawnRandomChunk();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Se o jogador estiver próximo da ponta do caminho gerado, instancia o próximo bloco
        if (nextSpawnPosition.x - playerTransform.position.x < spawnDistanceThreshold)
        {
            SpawnRandomChunk();
            RecycleOldestChunk();
        }
    }

    /// <summary>
    /// Instancia um Chunk específico informando a sua tag cadastrada no ObjectPooler.
    /// Usado para o Chunk inicial seguro.
    /// </summary>
    /// <param name="chunkTag">Tag do bloco no ObjectPooler.</param>
    private void SpawnSpecificChunk(string chunkTag)
    {
        GameObject newChunk = ObjectPooler.Instance.SpawnFromPool(chunkTag, nextSpawnPosition, Quaternion.identity);

        if (newChunk == null) return;

        activeChunks.Enqueue(newChunk);
        nextSpawnPosition.x += chunkWidth;
    }

    /// <summary>
    /// Consulta o BiomeManager, sorteia um Chunk do cenário atual e posiciona na cena.
    /// </summary>
    private void SpawnRandomChunk()
    {
        List<string> currentGroundTags = null;

        if (BiomeManager.Instance != null)
        {
            currentGroundTags = BiomeManager.Instance.GetCurrentGroundChunkTags();
        }

        if (currentGroundTags == null || currentGroundTags.Count == 0) return;

        int randomIndex = Random.Range(0, currentGroundTags.Count);
        string selectedTag = currentGroundTags[randomIndex];

        SpawnSpecificChunk(selectedTag);
    }

    /// <summary>
    /// Desativa o bloco mais antigo que ficou para trás para reuso no ObjectPooler.
    /// </summary>
    private void RecycleOldestChunk()
    {
        if (activeChunks.Count > initialChunksCount)
        {
            GameObject chunkToRecycle = activeChunks.Dequeue();
            chunkToRecycle.SetActive(false);
        }
    }
}