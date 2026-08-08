using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Garante o encaixe milimétrico dos blocos sem lacunas.
/// </summary>
public class ChunkSpawner : MonoBehaviour
{
    [Header("Referências Principais")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string startingChunkTag = "StartingChunk";
    [SerializeField] private List<string> chunkTags;

    [Header("Configurações de Encaixe")]
    [Tooltip("Largura exata do bloco no eixo X.")]
    [SerializeField] private float chunkWidth = 20f;
    [SerializeField] private int initialChunksCount = 5;
    [SerializeField] private float spawnDistanceThreshold = 30f;

    // Guarda a posição exata sem acúmulo de erros de arredondamento
    private Vector3 nextSpawnPosition = Vector3.zero;
    private Queue<GameObject> activeChunks = new Queue<GameObject>();

    private void Start()
    {
        // Define a posição inicial zerada para o primeiro bloco
        nextSpawnPosition = Vector3.zero;

        SpawnChunk(startingChunkTag);

        for (int i = 1; i < initialChunksCount; i++)
        {
            SpawnRandomChunk();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Compara a distância horizontal contínua
        if (nextSpawnPosition.x - playerTransform.position.x < spawnDistanceThreshold)
        {
            SpawnRandomChunk();
            RecycleOldestChunk();
        }
    }

    private void SpawnRandomChunk()
    {
        if (chunkTags == null || chunkTags.Count == 0) return;
        int randomIndex = Random.Range(0, chunkTags.Count);
        SpawnChunk(chunkTags[randomIndex]);
    }

    private void SpawnChunk(string chunkTag)
    {
        // Instancia na posição calculada exata
        GameObject newChunk = ObjectPooler.Instance.SpawnFromPool(chunkTag, nextSpawnPosition, Quaternion.identity);

        if (newChunk == null) return;

        activeChunks.Enqueue(newChunk);

        // Soma exatamente a largura do bloco para o próximo spawn
        nextSpawnPosition.x += chunkWidth;
    }

    private void RecycleOldestChunk()
    {
        if (activeChunks.Count > initialChunksCount)
        {
            GameObject chunkToRecycle = activeChunks.Dequeue();
            chunkToRecycle.SetActive(false);
        }
    }
}