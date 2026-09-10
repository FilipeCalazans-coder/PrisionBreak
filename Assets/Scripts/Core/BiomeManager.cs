using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define os andares ou rotas verticais disponíveis no jogo.
/// </summary>
public enum RouteLayer
{
    Default,    // Chão padrão / Térreo
    UpperRoof   // Teto / Andar de cima / Rota suspensa
}

/// <summary>
/// Estrutura que guarda os blocos e as alturas correspondentes a cada rota do bioma.
/// </summary>
[System.Serializable]
public class BiomeData
{
    [Tooltip("Nome do bioma (ex: Bloco de Celas, Refeitório, Pátio).")]
    public string biomeName;

    [Tooltip("Distância em metros necessária para entrar neste bioma.")]
    public float targetDistance;

    [Header("Rota Padrão (Térreo)")]
    [Tooltip("Altura vertical (Eixo Y) onde os blocos do térreo serão posicionados.")]
    public float groundHeightY = 0f;

    [Tooltip("Tags dos Chunks da rota térrea/padrão deste bioma.")]
    public List<string> groundChunkTags;

    [Header("Rota Superior (Teto / 2º Andar)")]
    [Tooltip("Altura vertical (Eixo Y) onde os blocos do teto serão posicionados.")]
    public float upperHeightY = 5f;

    [Tooltip("Tags dos Chunks exclusivos da rota superior deste bioma.")]
    public List<string> upperChunkTags;
}

/// <summary>
/// Controla a progressão horizontal de biomas, as rotas verticais e as alturas dos Chunks.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance;

    [Header("Referências")]
    [Tooltip("Transform do jogador para ler a distância X.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configuração de Biomas")]
    [Tooltip("Lista de biomas configurados por ordem de distância.")]
    [SerializeField] private List<BiomeData> biomes;

    // Estado ativo
    private int currentBiomeIndex = 0;
    private RouteLayer currentRoute = RouteLayer.Default;

    public RouteLayer CurrentRoute => currentRoute;

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

    private void Update()
    {
        if (playerTransform == null || biomes == null || biomes.Count == 0) return;

        // Verifica se atingiu a distância para o próximo bioma principal
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
    /// Altera o bioma principal com base na distância percorrida.
    /// </summary>
    private void SetBiome(int newIndex)
    {
        currentBiomeIndex = newIndex;
        Debug.Log($"Transição de Bioma: {biomes[currentBiomeIndex].biomeName} | Rota Ativa: {currentRoute}");
    }

    /// <summary>
    /// Altera a rota vertical ativa (chamado pelos gatilhos de subida/descida).
    /// </summary>
    public void SetRoute(RouteLayer newRoute)
    {
        currentRoute = newRoute;
        Debug.Log($"Rota alterada para: {currentRoute} no bioma {biomes[currentBiomeIndex].biomeName}");
    }

    /// <summary>
    /// Retorna as tags dos Chunks correspondentes ao bioma e à rota vertical ativa.
    /// </summary>
    public List<string> GetCurrentGroundChunkTags()
    {
        if (biomes != null && biomes.Count > currentBiomeIndex)
        {
            BiomeData activeBiome = biomes[currentBiomeIndex];

            // Se estiver no teto e houver chunks configurados, retorna os do teto
            if (currentRoute == RouteLayer.UpperRoof && activeBiome.upperChunkTags != null && activeBiome.upperChunkTags.Count > 0)
            {
                return activeBiome.upperChunkTags;
            }

            // Caso contrário, retorna os chunks da rota térrea padrão
            return activeBiome.groundChunkTags;
        }

        return new List<string>();
    }

    /// <summary>
    /// Retorna a altura vertical Y exata onde os blocos da rota ativa devem ser posicionados.
    /// </summary>
    public float GetCurrentRouteHeight()
    {
        if (biomes != null && biomes.Count > currentBiomeIndex)
        {
            BiomeData activeBiome = biomes[currentBiomeIndex];

            if (currentRoute == RouteLayer.UpperRoof)
            {
                return activeBiome.upperHeightY;
            }

            return activeBiome.groundHeightY;
        }

        return 0f;
    }
}