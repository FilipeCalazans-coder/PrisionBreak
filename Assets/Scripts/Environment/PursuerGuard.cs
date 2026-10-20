using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o guarda perseguidor.
/// Reproduz o rastro historico do jogador e avanca para capturar caso o jogador
/// fique travado em uma parede (sem se mover no eixo X).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PursuerGuard : MonoBehaviour
{
    public static PursuerGuard Instance;

    [Header("Referencias")]
    [Tooltip("Transform do jogador a ser perseguido.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Animator do guarda para trocar as animacoes.")]
    [SerializeField] private Animator animator;
    [Tooltip("SpriteRenderer do guarda para visualizacao.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Configuracoes de Distancia")]
    [Tooltip("Distancia horizontal padrao atras do jogador em corrida normal.")]
    [SerializeField] private float followDistanceX = 2.5f;
    [Tooltip("Distancia horizontal quando o guarda esta escondido fora da tela.")]
    [SerializeField] private float hiddenDistanceX = 14f;
    [Tooltip("Velocidade normal de transicao (aproximar/recuar).")]
    [SerializeField] private float transitionSpeed = 5f;
    [Tooltip("Velocidade com que o guarda avanca para cima do jogador preso na parede.")]
    [SerializeField] private float rushSpeedWhenStuck = 5.5f;

    [Header("Deteccao de Travamento no Eixo X")]
    [Tooltip("Velocidade minima de avanco horizontal para considerar que o jogador esta correndo.")]
    [SerializeField] private float minRunSpeedThreshold = 0.5f;
    [Tooltip("Tempo em segundos que o jogador pode ficar preso antes do guarda avancar.")]
    [SerializeField] private float stuckTimeThreshold = 0.2f;

    [Header("Captura")]
    [Tooltip("Distancia minima para o guarda pegar o jogador.")]
    [SerializeField] private float captureDistanceThreshold = 0.6f;

    [Header("Temporizador de Fuga")]
    [Tooltip("Tempo sem erros para o guarda desistir e sair de cena.")]
    [SerializeField] private float resetPursuitDelay = 8f;

    // Componentes internos
    private Rigidbody2D rb;

    // Historico do caminho percorrido pelo jogador
    private readonly List<Vector3> playerTrailHistory = new List<Vector3>();

    // Variaveis de controle e estado
    private bool isPursuing = false;
    private float pursuitTimer = 0f;
    private float currentDistanceX;
    private float lastPlayerPositionX;
    private float stuckTimer = 0f;
    private bool isPlayerStuckOnWall = false;

    public bool IsPursuing => isPursuing;

    private void Awake()
    {
        // Garante que exista apenas uma instancia deste componente
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        currentDistanceX = hiddenDistanceX;

        // Desativa fisica e visual enquanto o guarda nao estiver ativo
        rb.simulated = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    private void Start()
    {
        // Busca o jogador caso nao tenha sido associado no Inspector
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (playerTransform != null)
        {
            lastPlayerPositionX = playerTransform.position.x;
            transform.position = new Vector3(playerTransform.position.x - hiddenDistanceX, playerTransform.position.y, transform.position.z);
            playerTrailHistory.Add(playerTransform.position);
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Grava a movimentacao do jogador
        RecordPlayerPosition();

        // 2. Verifica se o jogador parou de se mover no eixo X (bateu na parede)
        CheckIfPlayerIsStuck();

        // 3. Temporizador de retirada do guarda
        if (isPursuing && !isPlayerStuckOnWall)
        {
            pursuitTimer -= Time.deltaTime;
            if (pursuitTimer <= 0f)
            {
                StopPursuit();
            }
        }

        // 4. Atualiza a distancia horizontal
        UpdateDistanceAndApproach();

        // 5. Move o guarda pelo rastro e checa captura
        if (rb.simulated)
        {
            FollowRecordedTrail();
            CheckCatchCondition();
        }
    }

    /// <summary>
    /// Mede o deslocamento horizontal do jogador para detectar paradas e colisoes em paredes.
    /// </summary>
    private void CheckIfPlayerIsStuck()
    {
        // Calcula a velocidade horizontal real do jogador em relacao ao ultimo frame
        float deltaX = playerTransform.position.x - lastPlayerPositionX;
        float horizontalSpeed = deltaX / Mathf.Max(0.0001f, Time.deltaTime);
        lastPlayerPositionX = playerTransform.position.x;

        // Se o jogador esta se movendo abaixo da velocidade minima de corrida esperada
        if (horizontalSpeed < minRunSpeedThreshold)
        {
            stuckTimer += Time.deltaTime;

            // Se o tempo travado ultrapassar o limite, consideramos que ele bateu na parede
            if (stuckTimer >= stuckTimeThreshold)
            {
                isPlayerStuckOnWall = true;

                // Se o guarda ainda nao estava em perseguicao, ele entra em acao imediatamente
                if (!isPursuing)
                {
                    TriggerPursuit();
                }
            }
        }
        else
        {
            // O jogador voltou a correr para a frente
            stuckTimer = 0f;
            isPlayerStuckOnWall = false;
        }
    }

    /// <summary>
    /// Ajusta a aproximacao do guarda com base na movimentacao do jogador.
    /// </summary>
    private void UpdateDistanceAndApproach()
    {
        if (isPursuing)
        {
            if (isPlayerStuckOnWall)
            {
                // Jogador travado na parede: o guarda corre rapidamente para alcancar distancia 0
                currentDistanceX = Mathf.MoveTowards(currentDistanceX, 0f, rushSpeedWhenStuck * Time.deltaTime);
            }
            else
            {
                // Jogador correndo normalmente: mantem a distancia padrao de perseguicao atras dele
                currentDistanceX = Mathf.MoveTowards(currentDistanceX, followDistanceX, transitionSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Guarda em retirada para fora da tela
            currentDistanceX = Mathf.MoveTowards(currentDistanceX, hiddenDistanceX, transitionSpeed * Time.deltaTime);

            if (currentDistanceX >= hiddenDistanceX - 0.5f)
            {
                if (rb.simulated) rb.simulated = false;
                if (spriteRenderer != null && spriteRenderer.enabled) spriteRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// Grava a posicao do jogador no rastro e descarta pontos antigos.
    /// </summary>
    private void RecordPlayerPosition()
    {
        Vector3 currentPos = playerTransform.position;

        if (playerTrailHistory.Count == 0 || Vector3.Distance(playerTrailHistory[playerTrailHistory.Count - 1], currentPos) > 0.05f)
        {
            playerTrailHistory.Add(currentPos);
        }

        float purgeBoundaryX = transform.position.x - 5f;
        while (playerTrailHistory.Count > 0 && playerTrailHistory[0].x < purgeBoundaryX)
        {
            playerTrailHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Move o guarda interpolando pelos pontos historicos gravados.
    /// </summary>
    private void FollowRecordedTrail()
    {
        if (playerTrailHistory.Count == 0) return;

        float desiredX = playerTransform.position.x - currentDistanceX;

        Vector3 targetPosition = Vector3.zero;
        bool foundSegment = false;

        for (int i = 0; i < playerTrailHistory.Count - 1; i++)
        {
            Vector3 pointA = playerTrailHistory[i];
            Vector3 pointB = playerTrailHistory[i + 1];

            if (pointA.x <= desiredX && pointB.x >= desiredX)
            {
                float segmentLength = pointB.x - pointA.x;
                float t = segmentLength > 0.001f ? (desiredX - pointA.x) / segmentLength : 0f;

                targetPosition = Vector3.Lerp(pointA, pointB, t);
                foundSegment = true;
                break;
            }
        }

        if (!foundSegment)
        {
            if (desiredX < playerTrailHistory[0].x)
            {
                targetPosition = new Vector3(desiredX, playerTrailHistory[0].y, transform.position.z);
            }
            else
            {
                targetPosition = playerTrailHistory[playerTrailHistory.Count - 1];
            }
        }

        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
    }

    /// <summary>
    /// Checa se o guarda alcancou o jogador para disparar a captura.
    /// </summary>
    private void CheckCatchCondition()
    {
        if (!isPursuing) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= captureDistanceThreshold)
        {
            CatchPlayer();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }

    /// <summary>
    /// Ativa o guarda na tela e inicia a perseguicao.
    /// </summary>
    public void TriggerPursuit()
    {
        isPursuing = true;
        pursuitTimer = resetPursuitDelay;

        // Se o guarda estava completamente fora da tela, aproxima para a borda visivel
        if (currentDistanceX > followDistanceX + 3.5f)
        {
            currentDistanceX = followDistanceX + 3.5f;
            if (playerTransform != null)
            {
                transform.position = new Vector3(playerTransform.position.x - currentDistanceX, playerTransform.position.y, transform.position.z);
            }
        }

        rb.simulated = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }

    /// <summary>
    /// Faz o guarda desistir e recuar para fora da tela.
    /// </summary>
    public void StopPursuit()
    {
        isPursuing = false;
        pursuitTimer = 0f;
    }

    /// <summary>
    /// Aciona a captura final.
    /// </summary>
    public void CatchPlayer()
    {
        isPursuing = false;
        currentDistanceX = 0f;

        if (animator != null)
        {
            animator.SetTrigger("Catch");
        }
    }
}