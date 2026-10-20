using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Representa um bloco procedural.
/// Calcula limites lendo diretamente os dados da grade do Tilemap,
/// garantindo uma leitura instantanea e contornando o atraso da fisica.
/// </summary>
public class Chunk : MonoBehaviour
{
    [Header("Configuracoes Manuais (Opcional)")]
    [Tooltip("Se desmarcado, ignora o calculo automatico e usa o manualWidth.")]
    [SerializeField] private bool autoCalculateBounds = true;
    [Tooltip("Largura padrao caso o calculo automatico falhe ou esteja desativado.")]
    [SerializeField] private float manualWidth = 20f;

    /// <summary>
    /// Retorna as bordas horizontais locais em relacao ao centro (Pivot) do Chunk.
    /// </summary>
    public void GetLocalHorizontalBounds(out float minX, out float maxX)
    {
        if (!autoCalculateBounds)
        {
            minX = 0f;
            maxX = manualWidth;
            return;
        }

        float worldMinX = float.MaxValue;
        float worldMaxX = float.MinValue;
        bool foundBounds = false;

        // 1. Tenta ler a malha visual diretamente dos Tilemaps (leitura imediata na arquitetura da grade)
        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
        if (tilemaps.Length > 0)
        {
            foreach (Tilemap tm in tilemaps)
            {
                // Forca o Tilemap a descartar espacos vazios e focar apenas onde existem tiles desenhados
                tm.CompressBounds();
                
                // Converte os limites da grade local para coordenadas absolutas do mundo
                Vector3 minWorld = tm.transform.TransformPoint(tm.localBounds.min);
                Vector3 maxWorld = tm.transform.TransformPoint(tm.localBounds.max);

                if (minWorld.x < worldMinX) worldMinX = minWorld.x;
                if (maxWorld.x > worldMaxX) worldMaxX = maxWorld.x;
                foundBounds = true;
            }
        }
        else
        {
            // 2. Fallback para fisica: procura colisores convencionais (caso o chunk seja feito de Sprites isolados)
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                if (!col.isTrigger && col.bounds.size.x > 0.01f)
                {
                    foundBounds = true;
                    if (col.bounds.min.x < worldMinX) worldMinX = col.bounds.min.x;
                    if (col.bounds.max.x > worldMaxX) worldMaxX = col.bounds.max.x;
                }
            }
        }

        // 3. Valida se encontrou medidas reais. Se a largura final for 0, usa o fallback de seguranca
        if (foundBounds && (worldMaxX - worldMinX) > 0.1f)
        {
            minX = worldMinX - transform.position.x;
            maxX = worldMaxX - transform.position.x;
        }
        else
        {
            minX = 0f;
            maxX = manualWidth;
        }
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