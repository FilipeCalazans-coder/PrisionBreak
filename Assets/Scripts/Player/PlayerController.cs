using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Controla a movimentação do jogador (corrida, pulo, slide, ataque e ground pound)
/// com diagnóstico completo via Logs no Console da Unity.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade constante padrão do jogador para a direita.")]
    [SerializeField] private float runSpeed = 4f;

    [Tooltip("Força do pulo normal aplicada ao personagem.")]
    [SerializeField] private float jumpForce = 6f;

    [Header("Configurações de Ataque Básico")]
    [Tooltip("Objeto filho que contém o colisor (Hitbox) do ataque.")]
    [SerializeField] private GameObject attackHitboxObject;

    [Tooltip("Duração em segundos que a Hitbox do ataque permanece ativa.")]
    [SerializeField] private float attackDuration = 0.3f;

    [Header("Configurações de Impacto (Bounce)")]
    [Tooltip("Força do pulo de resposta ao esmagar um inimigo com o Ground Pound.")]
    [SerializeField] private float bounceForce = 6f;

    [Header("Configurações de Slide & Dash")]
    [Tooltip("Tempo em segundos que o personagem permanece agachado e acelerado.")]
    [SerializeField] private float slideDuration = 0.8f;

    [Tooltip("Velocidade extra adicionada ao movimento horizontal EXCLUSIVAMENTE durante o Slide.")]
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Configurações de Ground Pound")]
    [Tooltip("Força com que o jogador é lançado para baixo no Ground Pound.")]
    [SerializeField] private float groundPoundForce = 25f;

    [Tooltip("Tempo máximo em segundos entre dois toques no ar para acionar o Ground Pound.")]
    [SerializeField] private float doubleTapThreshold = 0.3f;

    [Header("Configurações de Swipe (Sensibilidade)")]
    [Tooltip("Distância mínima em pixels para considerar um gesto de deslize.")]
    [SerializeField] private float minSwipeDistance = 30f; // Reduzido ligeiramente para facilitar a detecção no simulador

    [Header("Verificação de Chão")]
    [Tooltip("Ponto de onde será feito o raio de detecção do chão.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Raio da esfera de detecção do chão.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("Camada (Layer) correspondente ao chão.")]
    [SerializeField] private LayerMask groundLayer;

    // Componentes e variáveis internas
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private bool isGrounded;
    private bool jumpRequested;
    private bool isSliding;
    private bool isGroundPounding;
    private bool isAttacking;

    public bool IsGroundPounding => isGroundPounding;

    private float lastAirTapTime = 0f;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;

    // Variáveis de controle de entrada
    private Vector2 startTouchPos;
    private Vector2 currentTouchPos;
    private bool isHoldingTouch;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        if (capsuleCollider != null)
        {
            originalColliderSize = capsuleCollider.size;
            originalColliderOffset = capsuleCollider.offset;
        }

        if (attackHitboxObject == null)
        {
            Transform foundHitbox = transform.Find("AttackHitbox");
            if (foundHitbox != null)
            {
                attackHitboxObject = foundHitbox.gameObject;
            }
        }

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[PlayerController] ALERTA: AttackHitboxObject NÃO foi encontrado no Player!");
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                isGroundPounding = false;
            }
        }

        // Atualiza a posição do ponteiro enquanto ele estiver pressionado
        if (isHoldingTouch)
        {
            Vector2 pos = GetInputPosition();
            if (pos != Vector2.zero)
            {
                currentTouchPos = pos;
            }
        }
    }

    private void FixedUpdate()
    {
        float currentSpeed = runSpeed;
        if (isSliding && !isGroundPounding)
        {
            currentSpeed += dashBonusSpeed;
        }

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            jumpRequested = false;
        }
    }

    public void Bounce()
    {
        isGroundPounding = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
    }

    /// <summary>
    /// Evento chamado diretamente pelo componente Player Input.
    /// </summary>
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Vector2 pos = GetInputPosition();
            Debug.Log($"[Input Log] Toque INICIADO na posição Y: {pos.y}");

            startTouchPos = pos;
            currentTouchPos = pos;
            isHoldingTouch = true;

            // Checagem de Ground Pound (Duplo toque no ar)
            if (!isGrounded)
            {
                float timeSinceLastTap = Time.time - lastAirTapTime;
                if (timeSinceLastTap <= doubleTapThreshold)
                {
                    Debug.Log("[Input Log] Ação reconhecida: GROUND POUND!");
                    ExecuteGroundPound();
                }
                lastAirTapTime = Time.time;
            }
        }
        else
        {
            Debug.Log("[Input Log] Toque LIBERADO. Avaliando gesto...");
            isHoldingTouch = false;

            EvaluateGesture();
        }
    }

    /// <summary>
    /// Decide a ação (Pulo, Slide ou Ataque) ao soltar a tela.
    /// </summary>
    private void EvaluateGesture()
    {
        float deltaY = currentTouchPos.y - startTouchPos.y;
        Debug.Log($"[Input Log] Variação Vertical (Delta Y): {deltaY} | Limite (MinSwipe): {minSwipeDistance}");

        // 1. Swipe Up (Pulo)
        if (deltaY >= minSwipeDistance && !isGroundPounding)
        {
            Debug.Log("[Input Log] Decisão: PULO (Swipe Up)");
            jumpRequested = true;
        }
        // 2. Swipe Down (Slide)
        else if (deltaY <= -minSwipeDistance && !isGroundPounding)
        {
            Debug.Log("[Input Log] Decisão: SLIDE (Swipe Down)");
            StartSlide();
        }
        // 3. Toque Simples (Ataque) - No chão ou no ar!
        else if (Mathf.Abs(deltaY) < minSwipeDistance && !isGroundPounding)
        {
            Debug.Log("[Input Log] Decisão: ATAQUE BÁSICO (Toque Simples)");
            TriggerAttack();
        }
    }

    public void TriggerAttack()
    {
        if (isAttacking)
        {
            Debug.Log("[Input Log] Ataque ignorado: O jogador já está atacando.");
            return;
        }

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(true);
            Debug.Log("<color=green>[Ataque Log] Hitbox do Ataque ATIVADA!</color>");
        }
        else
        {
            Debug.LogError("[Ataque Log] ERRO: Referência de attackHitboxObject está NULL!");
        }

        yield return new WaitForSeconds(attackDuration);

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(false);
            Debug.Log("<color=red>[Ataque Log] Hitbox do Ataque DESATIVADA!</color>");
        }

        isAttacking = false;
    }

    private void ExecuteGroundPound()
    {
        if (isGroundPounding) return;

        isGroundPounding = true;

        if (isSliding)
        {
            StopAllCoroutines();
            ResetCollider();
            isSliding = false;
        }

        rb.linearVelocity = new Vector2(runSpeed, -groundPoundForce);
    }

    /// <summary>
    /// Método auxiliar para ler a posição atual do ponteiro/toque.
    /// </summary>
    private Vector2 GetInputPosition()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.position.ReadValue();
        }

        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            return UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0].screenPosition;
        }

        return Vector2.zero;
    }

    private void StartSlide()
    {
        if (isSliding || isGroundPounding) return;

        if (!isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -groundPoundForce * 0.5f);
        }

        StartCoroutine(SlideRoutine());
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        if (capsuleCollider != null)
        {
            capsuleCollider.size = new Vector2(originalColliderSize.x, originalColliderSize.y * 0.5f);
            capsuleCollider.offset = new Vector2(originalColliderOffset.x, originalColliderOffset.y - (originalColliderSize.y * 0.25f));
        }

        yield return new WaitForSeconds(slideDuration);

        ResetCollider();
        isSliding = false;
    }

    private void ResetCollider()
    {
        if (capsuleCollider != null)
        {
            capsuleCollider.size = originalColliderSize;
            capsuleCollider.offset = originalColliderOffset;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}