using UnityEngine;

/// <summary>
/// Representa um bloco procedural e calcula os limites horizontais reais
/// para permitir encaixe contínuo e perfeito entre os blocos.
/// </summary>
public class Chunk : MonoBehaviour
{
    [Header("Configurações Manuais (Opcional)")]
    [Tooltip("Se desmarcado, ignora o cálculo automático e usa o manualWidth.")]
    [SerializeField] private bool autoCalculateBounds = true;
    [Tooltip("Largura padrão caso o cálculo automático esteja desativado.")]
    [SerializeField] private float manualWidth = 20f;

    /// <summary>
    /// Retorna as bordas horizontais locais em relação ao centro (Pivot) do Chunk.
    /// </summary>
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
            // Mede apenas plataformas sólidas, ignorando moedas, inimigos e gatilhos de câmara
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

        minX = worldMinX - transform.position.x;
        maxX = worldMaxX - transform.position.x;
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