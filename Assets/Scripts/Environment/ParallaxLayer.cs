using UnityEngine;

/// <summary>
/// Controla o efeito Parallax de uma camada individual de fundo,
/// fazendo-a mover-se proporcionalmente à velocidade da câmera/jogador.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Transform do jogador ou da câmera principal para acompanhar o movimento.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Configurações do Efeito")]
    [Tooltip("Fator de velocidade do Parallax. Valores entre 0 (preso na câmera) e 1 (preso no chão).")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffect = 0.5f;

    [Header("Repetição Infinita (Opcional)")]
    [Tooltip("Marque esta opção se o fundo deve se repetir infinitamente conforme a câmera avança.")]
    [SerializeField] private bool infiniteLoop = true;

    [Tooltip("Largura horizontal da imagem de fundo para calcular o reposicionamento.")]
    [SerializeField] private float textureUnitSizeX = 20f;

    // Posições internas para controle de movimento
    private Vector3 lastCameraPosition;

    private void Start()
    {
        // Se a câmera não for atribuída no Inspector, pega a Câmera Principal automaticamente
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calculates quanta distância a câmera se moveu desde o último frame
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Move a camada do fundo a uma fração do movimento da câmera
        transform.position += new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffect, 0f);

        // Atualiza a última posição salva da câmera
        lastCameraPosition = cameraTransform.position;

        // Lógica de repetição infinita do fundo no eixo X
        if (infiniteLoop && textureUnitSizeX > 0f)
        {
            if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
            {
                float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
                transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, transform.position.y, transform.position.z);
            }
        }
    }
}