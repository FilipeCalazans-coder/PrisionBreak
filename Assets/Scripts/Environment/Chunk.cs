using UnityEngine;

/// <summary>
/// Representa um bloco procedural e calcula os limites horizontais reais
/// (início, fim e largura) considerando múltiplos blocos de chão e buracos.
/// </summary>
public class Chunk : MonoBehaviour
{
    [Header("Configurações Manuais (Opcional)")]
    [Tooltip("Se desmarcado, ignora o cálculo automático e usa os valores manuais.")]
    [SerializeField] private bool autoCalculateBounds = true;

    [Tooltip("Largura padrão caso o cálculo automático esteja desativado.")]
    [SerializeField] private float manualWidth = 20f;

    /// <summary>
    /// Retorna os limites horizontais locais do Chunk em relação ao seu próprio centro (Pivot).
    /// </summary>
    /// <param name="minX">Ponto mais à esquerda em coordenadas locais.</param>
    /// <param name="maxX">Ponto mais à direita em coordenadas locais.</param>
    public void GetLocalHorizontalBounds(out float minX, out float maxX)
    {
        if (!autoCalculateBounds)
        {
            minX = -manualWidth / 2f;
            maxX = manualWidth / 2f;
            return;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        
        bool foundValidCollider = false;
        float worldMinX = float.MaxValue;
        float worldMaxX = float.MinValue;

        foreach (Collider2D col in colliders)
        {
            // Ignora gatilhos (moedas, inimigos, áreas de dano) para medir apenas superfícies sólidas
            if (!col.isTrigger)
            {
                foundValidCollider = true;
                if (col.bounds.min.x < worldMinX) worldMinX = col.bounds.min.x;
                if (col.bounds.max.x > worldMaxX) worldMaxX = col.bounds.max.x;
            }
        }

        if (!foundValidCollider)
        {
            minX = -manualWidth / 2f;
            maxX = manualWidth / 2f;
            return;
        }

        // Converte as coordenadas globais para relativas à posição do Chunk
        minX = worldMinX - transform.position.x;
        maxX = worldMaxX - transform.position.x;
    }

    /// <summary>
    /// Retorna a largura total calculada.
    /// </summary>
    public float GetChunkWidth()
    {
        GetLocalHorizontalBounds(out float minX, out float maxX);
        return maxX - minX;
    }

    private void OnDrawGizmosSelected()
    {
        GetLocalHorizontalBounds(out float minX, out float maxX);
        float width = maxX - minX;
        float centerX = transform.position.x + minX + (width / 2f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(centerX, transform.position.y, 0f), new Vector3(width, 2f, 0.1f));
    }
}