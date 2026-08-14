using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define os dados e assets de um cenário/bioma do jogo.
/// </summary>
[System.Serializable]
public class BiomeData
{
    [Tooltip("Nome de identificação do cenário (ex: Floresta, Caverna).")]
    public string biomeName;

    [Tooltip("Distância em metros necessária para ativar este bioma.")]
    public float targetDistance;

    [Tooltip("Tags no ObjectPooler correspondentes aos Chunks de chão deste cenário.")]
    public List<string> groundChunkTags;
}

/// <summary>
/// Controla a transição de cenários/biomas com base na distância percorrida.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    // Instância estática para permitir acesso global fácil (Padrão Singleton)
    public static BiomeManager Instance;

    [Header("Referências")]
    [Tooltip("Transform do jogador para ler a distância X.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuração de Biomas")]
    [Tooltip("Lista dos cenários do jogo em ordem de progressão.")]
    [SerializeField] private List<BiomeData> biomes;

    // Bioma e índice atual
    private int currentBiomeIndex = 0;

    private void Awake()
    {
        // Garante a existência de apenas uma instância do BiomeManager na cena
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

    private void Update()
    {
        if (playerTransform == null || biomes == null || biomes.Count == 0) return;

        // Verifica se o jogador atingiu a distância para o próximo bioma
        int nextIndex = currentBiomeIndex + 1;
        if (nextIndex < biomes.Count)
        {
            if (playerTransform.position.x >= biomes[nextIndex].targetDistance)
            {
                SetBiome(nextIndex);
            }
        }
    }

    /// <summary>
    /// Altera o bioma ativo conforme o jogador avança no mapa.
    /// </summary>
    /// <param name="newIndex">Índice do novo bioma na lista.</param>
    private void SetBiome(int newIndex)
    {
        currentBiomeIndex = newIndex;
        Debug.Log($"Transição de Cenário! Novo Bioma: {biomes[currentBiomeIndex].biomeName}");
    }

    /// <summary>
    /// Retorna as tags dos Chunks de chão do bioma atual para o ChunkSpawner.
    /// </summary>
    public List<string> GetCurrentGroundChunkTags()
    {
        if (biomes != null && biomes.Count > currentBiomeIndex)
        {
            return biomes[currentBiomeIndex].groundChunkTags;
        }
        return new List<string>();
    }
}