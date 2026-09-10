using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a criação procedural de blocos com suporte a geração imediata
/// ao alternar entre rotas (térreo e teto), garantindo que o jogador sempre
/// tenha chão sólido ao mudar de andar.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    public static ChunkSpawner Instance;

    [Header("Referências Principais")]
    [Tooltip("Transform do jogador para monitorar o avanço horizontal.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuração do Bloco Inicial")]
    [Tooltip("Tag no ObjectPooler correspondente ao Chunk inicial.")]
    [SerializeField] private string startingChunkTag = "StartingChunk";

    [Header("Configurações de Geração")]
    [Tooltip("Largura de segurança caso o Chunk não tenha o script Chunk.")]
    [SerializeField] private float fallbackChunkWidth = 20f;
    [Tooltip("Quantidade de blocos mantidos ativos simultaneamente.")]
    [SerializeField] private int initialChunksCount = 5;
    [Tooltip("Distância à frente do jogador para acionar a criação do próximo bloco.")]
    [SerializeField] private float spawnDistanceThreshold = 30f;

    // Coordenada horizontal onde o último bloco gerado terminou (por rota)
    private float currentGroundEndX = 0f;
    private float currentRoofEndX = 0f;

    // Fila para reaproveitamento de memória (pooling)
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

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
        currentGroundEndX = 0f;
        currentRoofEndX = 0f;

        // 1. Gera o bloco inicial de segurança no térreo
        SpawnSpecificChunk(startingChunkTag, GetHeightForRoute(RouteLayer.Default), ref currentGroundEndX);

        // 2. Preenche os blocos seguintes da rota inicial
        for (int i = 1; i < initialChunksCount; i++)
        {
            SpawnNextChunkInActiveRoute();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Obtém qual rota está ativa no momento (Default ou UpperRoof)
        RouteLayer currentRoute = GetCurrentActiveRoute();
        float currentEndX = (currentRoute == RouteLayer.UpperRoof) ? currentRoofEndX : currentGroundEndX;

        // Gera novos blocos na rota ativa conforme o jogador se aproxima do final do chão gerado
        if (currentEndX - playerTransform.position.x < spawnDistanceThreshold)
        {
            SpawnNextChunkInActiveRoute();
            RecycleOldestChunk();
        }
    }

    /// <summary>
    /// Força o Spawner a sincronizar e gerar blocos imediatamente à frente do jogador
    /// quando ele sobe ou desce de andar.
    /// </summary>
    /// <param name="newRoute">Nova rota ativada (Default ou UpperRoof).</param>
    /// <param name="transitionX">Posição horizontal X onde a transição ocorreu.</param>
    public void SwitchRouteAndSpawnImmediate(RouteLayer newRoute, float transitionX)
    {
        float targetY = GetHeightForRoute(newRoute);

        if (newRoute == RouteLayer.UpperRoof)
        {
            // Se a rota do teto estiver atrás do jogador, puxa o início para a posição do gatilho
            if (currentRoofEndX < transitionX)
            {
                currentRoofEndX = transitionX;
            }

            // Gera 3 blocos imediatamente para garantir piso contínuo à frente do jogador
            for (int i = 0; i < 3; i++)
            {
                SpawnChunkForRoute(newRoute, targetY, ref currentRoofEndX);
            }
        }
        else // Retorno para a rota padrão (Térreo)
        {
            if (currentGroundEndX < transitionX)
            {
                currentGroundEndX = transitionX;
            }

            for (int i = 0; i < 3; i++)
            {
                SpawnChunkForRoute(newRoute, targetY, ref currentGroundEndX);
            }
        }
    }

    /// <summary>
    /// Sorteia e posiciona o próximo bloco correspondente à rota ativa.
    /// </summary>
    private void SpawnNextChunkInActiveRoute()
    {
        RouteLayer currentRoute = GetCurrentActiveRoute();
        float targetY = GetHeightForRoute(currentRoute);

        if (currentRoute == RouteLayer.UpperRoof)
        {
            SpawnChunkForRoute(currentRoute, targetY, ref currentRoofEndX);
        }
        else
        {
            SpawnChunkForRoute(currentRoute, targetY, ref currentGroundEndX);
        }
    }

    /// <summary>
    /// Sorteia um prefab da rota e posiciona na coordenada horizontal referenciada.
    /// </summary>
    private void SpawnChunkForRoute(RouteLayer route, float targetY, ref float endXReference)
    {
        List<string> currentTags = null;

        if (BiomeManager.Instance != null)
        {
            currentTags = BiomeManager.Instance.GetCurrentGroundChunkTags();
        }

        if (currentTags == null || currentTags.Count == 0) return;

        int randomIndex = Random.Range(0, currentTags.Count);
        string selectedTag = currentTags[randomIndex];

        SpawnSpecificChunk(selectedTag, targetY, ref endXReference);
    }

    /// <summary>
    /// Encaixa o Chunk na posição horizontal contínua da rota indicada.
    /// </summary>
    private void SpawnSpecificChunk(string chunkTag, float targetY, ref float endXReference)
    {
        if (ObjectPooler.Instance == null) return;

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

        // Calcula a posição central para a borda esquerda tocar no final anterior daquela rota
        float spawnCenterX = endXReference - minX;

        newChunk.transform.position = new Vector3(spawnCenterX, targetY, 0f);

        // Atualiza a posição final da esteira dessa rota
        endXReference = spawnCenterX + maxX;
    }

    private RouteLayer GetCurrentActiveRoute()
    {
        if (BiomeManager.Instance != null)
        {
            return BiomeManager.Instance.CurrentRoute;
        }
        return RouteLayer.Default;
    }

    private float GetHeightForRoute(RouteLayer route)
    {
        if (BiomeManager.Instance != null)
        {
            return BiomeManager.Instance.GetCurrentRouteHeight();
        }
        return 0f;
    }

    /// <summary>
    /// Recicla o bloco mais antigo que ficou para trás do jogador.
    /// </summary>
    private void RecycleOldestChunk()
    {
        // Aumentamos a tolerância para manter blocos de ambas as rotas enquanto estiverem visíveis
        if (activeChunks.Count > initialChunksCount * 2)
        {
            GameObject chunkToRecycle = activeChunks.Dequeue();
            chunkToRecycle.SetActive(false);
        }
    }
}