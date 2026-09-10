using UnityEngine;

/// <summary>
/// Serve como alvo (Target) para a CinemachineCamera.
/// Acompanha o jogador no eixo X e mantém o eixo Y travado por andares/patamares,
/// transitando suavemente entre diferentes alturas.
/// </summary>
public class CameraFollowTarget : MonoBehaviour
{
    // Instância estática para acesso global direto pelos gatilhos
    public static CameraFollowTarget Instance;

    [Header("Alvo a Seguir")]
    [Tooltip("Transform do jogador.")]
    [SerializeField] private Transform playerTransform;

    [Header("Configurações de Altura (Eixo Y)")]
    [Tooltip("Altura vertical padrão do chão/primeiro andar.")]
    [SerializeField] private float defaultHeightY = 0f;
    [Tooltip("Velocidade de transição vertical entre os andares (quanto menor, mais rápido).")]
    [SerializeField] private float verticalSmoothTime = 0.3f;

    // Controle de altura e amortecimento
    private float targetHeightY;
    private float verticalVelocity;

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

        targetHeightY = defaultHeightY;
    }

    private void Start()
    {
        // Se o player não for referenciado no Inspector, procura-o pela Tag
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        // Posiciona o objeto de imediato na coordenada inicial
        if (playerTransform != null)
        {
            transform.position = new Vector3(playerTransform.position.x, defaultHeightY, playerTransform.position.z);
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        // 1. Eixo X: Acompanha o avanço do jogador diretamente
        float newX = playerTransform.position.x;

        // 2. Eixo Y: Transita suavemente até a altura do andar ativo
        float newY = Mathf.SmoothDamp(transform.position.y, targetHeightY, ref verticalVelocity, verticalSmoothTime);

        // 3. Atualiza a posição da âncora
        transform.position = new Vector3(newX, newY, playerTransform.position.z);
    }

    /// <summary>
    /// Altera a altura alvo para posicionar a câmara no novo andar.
    /// </summary>
    /// <param name="newHeightY">Coordenada Y do novo andar.</param>
    public void SetTargetHeight(float newHeightY)
    {
        targetHeightY = newHeightY;
    }

    /// <summary>
    /// Retorna a altura para o valor base.
    /// </summary>
    public void ResetHeight()
    {
        targetHeightY = defaultHeightY;
    }
}