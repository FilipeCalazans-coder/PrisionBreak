using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a criacao procedural de blocos e garante que o jogador
/// seja posicionado exatamente no centro horizontal do primeiro chunk.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    public static ChunkSpawner Instance;

    [Header("Referencias Principais")]
    [Tooltip("Transform do jogador para monitorar o avanco horizontal.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuracao do Bloco Inicial")]
    [Tooltip("Tag no ObjectPooler correspondente ao Chunk inicial.")]
    [SerializeField] private string startingChunkTag = "StartingChunk";

    [Header("Configuracoes de Geracao")]
    [Tooltip("Largura de seguranca caso o Chunk nao tenha o script Chunk anexado.")]
    [SerializeField] private float fallbackChunkWidth = 20f;
    [Tooltip("Quantidade de blocos mantidos ativos simultaneamente.")]
    [SerializeField] private int initialChunksCount = 5;
    [Tooltip("Distancia a frente do jogador para acionar a criacao do proximo bloco.")]
    [SerializeField] private float spawnDistanceThreshold = 30f;

    // Coordenada horizontal onde o ultimo bloco gerado terminou (por rota)
    private float currentGroundEndX = 0f;
    private float currentRoofEndX = 0f;

    // Fila para reaproveitamento de memoria (pooling)
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
        // Garante a busca do jogador caso a referencia nao tenha sido colocada no Inspector
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        currentGroundEndX = 0f;
        currentRoofEndX = 0f;

        // 1. Gera o primeiro bloco de seguranca
        GameObject firstChunk = SpawnSpecificChunk(startingChunkTag, GetHeightForRoute(RouteLayer.Default), ref currentGroundEndX);

        // 2. Posiciona o jogador com precisao no meio horizontal desse bloco inicial
        PositionPlayerAtChunkCenter(firstChunk);

        // 3. Preenche os blocos seguintes da esteira
        for (int i = 1; i < initialChunksCount; i++)
        {
            SpawnNextChunkInActiveRoute();
        }
    }

    /// <summary>
    /// Calcula o ponto central exato do primeiro bloco e reposiciona o jogador e sua fisica.
    /// </summary>
    /// <param name="chunkObj">GameObject do primeiro chunk instanciado.</param>
    private void PositionPlayerAtChunkCenter(GameObject chunkObj)
    {
        if (chunkObj == null || playerTransform == null) return;

        Chunk chunkComponent = chunkObj.GetComponent<Chunk>();
        float minX = -fallbackChunkWidth / 2f;
        float maxX = fallbackChunkWidth / 2f;

        // Obtem as bordas reais do colisor do chunk se o script existir
        if (chunkComponent != null)
        {
            chunkComponent.GetLocalHorizontalBounds(out minX, out maxX);
        }

        // Formula correta do centro horizontal: posicao X do objeto somada a media dos limites locais
        float middleX = chunkObj.transform.position.x + ((minX + maxX) / 2f);

        // Reposiciona o jogador no eixo X mantendo a sua altura Y original da cena
        Vector3 newPlayerPosition = new Vector3(middleX, playerTransform.position.y, playerTransform.position.z);
        playerTransform.position = newPlayerPosition;

        // Atualiza tambem o Rigidbody2D para evitar conflito de teletransporte na fisica da Unity
        Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.position = new Vector2(middleX, playerTransform.position.y);
            playerRb.linearVelocity = Vector2.zero;
        }

        Debug.Log($"[ChunkSpawner] Jogador reposicionado com sucesso no meio da primeira chunk: X = {middleX}");
    }

    private void Update()
    {
        if (playerTransform == null) return;

        RouteLayer currentRoute = GetCurrentActiveRoute();
        float currentEndX = (currentRoute == RouteLayer.UpperRoof) ? currentRoofEndX : currentGroundEndX;

        // Gera novos blocos na rota ativa conforme o jogador se aproxima do final do chao
        if (currentEndX - playerTransform.position.x < spawnDistanceThreshold)
        {
            SpawnNextChunkInActiveRoute();
            RecycleOldestChunk();
        }
    }

    public void SwitchRouteAndSpawnImmediate(RouteLayer newRoute, float transitionX)
    {
        float targetY = GetHeightForRoute(newRoute);

        if (newRoute == RouteLayer.UpperRoof)
        {
            if (currentRoofEndX < transitionX)
            {
                currentRoofEndX = transitionX;
            }

            for (int i = 0; i < 3; i++)
            {
                SpawnChunkForRoute(newRoute, targetY, ref currentRoofEndX);
            }
        }
        else
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
    /// Encaixa o Chunk na posicao horizontal continua da rota indicada e devolve a instancia gerada.
    /// </summary>
    private GameObject SpawnSpecificChunk(string chunkTag, float targetY, ref float endXReference)
    {
        if (ObjectPooler.Instance == null) return null;

        GameObject newChunk = ObjectPooler.Instance.SpawnFromPool(chunkTag, Vector3.zero, Quaternion.identity);
        if (newChunk == null) return null;

        activeChunks.Enqueue(newChunk);

        Chunk chunkComponent = newChunk.GetComponent<Chunk>();
        float minX = -fallbackChunkWidth / 2f;
        float maxX = fallbackChunkWidth / 2f;

        if (chunkComponent != null)
        {
            chunkComponent.GetLocalHorizontalBounds(out minX, out maxX);
        }

        // Encaixa a borda esquerda do novo chunk no ponto onde o anterior terminou
        float spawnCenterX = endXReference - minX;
        newChunk.transform.position = new Vector3(spawnCenterX, targetY, 0f);

        // Atualiza a coordenada final do chao
        endXReference = spawnCenterX + maxX;

        return newChunk;
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

    private void RecycleOldestChunk()
    {
        if (activeChunks.Count > initialChunksCount * 2)
        {
            GameObject chunkToRecycle = activeChunks.Dequeue();
            chunkToRecycle.SetActive(false);
        }
    }
}