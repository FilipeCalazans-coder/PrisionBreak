using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a criação contínua de blocos conectando a ponta esquerda do novo bloco
/// perfeitamente com a ponta direita do bloco anterior, independente de buracos.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    [Header("Referências Principais")]
    [Tooltip("Transform do jogador para monitorar a posição de avanço.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuração do Bloco Inicial")]
    [Tooltip("Tag no ObjectPooler correspondente ao Chunk inicial seguro.")]
    [SerializeField] private string startingChunkTag = "StartingChunk";

    [Header("Configurações de Geração")]
    [Tooltip("Altura padrão (Eixo Y) onde o chão será alinhado.")]
    [SerializeField] private float fixedGroundY = 0f;

    [Tooltip("Largura de segurança caso o Chunk não tenha o script Chunk.")]
    [SerializeField] private float fallbackChunkWidth = 20f;

    [Tooltip("Quantidade de blocos mantidos ativos na cena.")]
    [SerializeField] private int initialChunksCount = 5;

    [Tooltip("Distância à frente do jogador para acionar a criação do próximo bloco.")]
    [SerializeField] private float spawnDistanceThreshold = 30f;

    // Coordenada X onde o chão do último bloco gerado terminou
    private float currentEndOfGroundX = 0f;

    // Fila para reciclagem de blocos na memória
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    private void Start()
    {
        currentEndOfGroundX = 0f;

        // 1. Instancia o bloco inicial
        SpawnSpecificChunk(startingChunkTag);

        // 2. Preenche o restante do caminho
        for (int i = 1; i < initialChunksCount; i++)
        {
            SpawnRandomChunk();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Se o jogador estiver próximo do final do chão gerado, gera o próximo
        if (currentEndOfGroundX - playerTransform.position.x < spawnDistanceThreshold)
        {
            SpawnRandomChunk();
            RecycleOldestChunk();
        }
    }

    /// <summary>
    /// Posiciona o Chunk de forma que a sua borda esquerda encoste perfeitamente na borda direita anterior.
    /// </summary>
    private void SpawnSpecificChunk(string chunkTag)
    {
        // 1. Instancia temporariamente no ponto neutro para ler seus limites locais
        GameObject newChunk = ObjectPooler.Instance.SpawnFromPool(chunkTag, Vector3.zero, Quaternion.identity);
        if (newChunk == null) return;

        activeChunks.Enqueue(newChunk);

        Chunk chunkComponent = newChunk.GetComponent<Chunk>();
        float minX = -fallbackChunkWidth / 2f;
        float maxX = fallbackChunkWidth / 2f;

        if (chunkComponent != null)
        {
            chunkComponent.GetLocalHorizontalBounds(out minX, out maxX);
        }

        // 2. Calcula a posição onde o centro do Chunk deve ficar para que seu início (minX) toque o fim anterior
        float spawnCenterX = currentEndOfGroundX - minX;

        // 3. Aplica a posição final corrigida
        newChunk.transform.position = new Vector3(spawnCenterX, fixedGroundY, 0f);

        // 4. Atualiza o ponto final do chão com a ponta direita (maxX) deste bloco
        currentEndOfGroundX = spawnCenterX + maxX;
    }

    /// <summary>
    /// Consulta o BiomeManager e sorteia um bloco correspondente.
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
    /// Desativa o bloco mais antigo que ficou para trás para reaproveitamento no pool.
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