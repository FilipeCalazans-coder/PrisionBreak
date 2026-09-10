using UnityEngine;

/// <summary>
/// Gatilho acionado ao mudar de andar/área.
/// Move a câmara, comunica o BiomeManager e faz o ChunkSpawner gerar
/// blocos imediatamente a partir da posição do gatilho.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class AreaTransitionTrigger : MonoBehaviour
{
    [Header("Configuração da Rota do Bioma")]
    [Tooltip("Qual rota vertical este gatilho ativa (Default ou UpperRoof).")]
    [SerializeField] private RouteLayer targetRoute = RouteLayer.UpperRoof;

    [Header("Configuração da Câmara")]
    [Tooltip("Altura Y para onde a câmara deve transitar suavemente.")]
    [SerializeField] private float targetCameraHeight = 6f;

    [Tooltip("Tag de identificação do jogador.")]
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            // 1. Atualiza a câmara
            if (CameraFollowTarget.Instance != null)
            {
                CameraFollowTarget.Instance.SetTargetHeight(targetCameraHeight);
            }

            // 2. Atualiza a rota ativa no BiomeManager
            if (BiomeManager.Instance != null)
            {
                BiomeManager.Instance.SetRoute(targetRoute);
            }

            // 3. Força o spawner a gerar blocos da nova rota a partir deste ponto horizontal exato
            if (ChunkSpawner.Instance != null)
            {
                ChunkSpawner.Instance.SwitchRouteAndSpawnImmediate(targetRoute, transform.position.x);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(transform.position.x - 3f, targetCameraHeight, 0f),
                        new Vector3(transform.position.x + 3f, targetCameraHeight, 0f));
    }
}