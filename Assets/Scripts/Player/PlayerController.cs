using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla a física, as ações e as animações do jogador no runner 2D.
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
    [SerializeField] private float attackDuration = 0.2f;

    [Header("Configurações de Impacto (Bounce)")]
    [Tooltip("Força do pulo de resposta ao esmagar um inimigo com o Ground Pound.")]
    [SerializeField] private float bounceForce = 6f;

    [Header("Configurações de Slide & Dash")]
    [Tooltip("Tempo em segundos que o personagem permanece agachado e acelerado.")]
    [SerializeField] private float slideDuration = 0.8f;

    [Tooltip("Velocidade extra adicionada horizontalmente durante o Slide.")]
    [SerializeField] private float dashBonusSpeed = 5f;

    [Header("Configurações de Ground Pound")]
    [Tooltip("Força com que o jogador é lançado para baixo no Ground Pound.")]
    [SerializeField] private float groundPoundForce = 25f;

    [Tooltip("Janela máxima em segundos entre os toques no ar para acionar o Ground Pound.")]
    [SerializeField] private float doubleTapThreshold = 0.25f;

    [Header("Configurações de Swipe (Sensibilidade)")]
    [Tooltip("Distância mínima em pixels para considerar um gesto de deslize.")]
    [SerializeField] private float minSwipeDistance = 30f;

    [Header("Verificação de Chão")]
    [Tooltip("Ponto de onde será feito o raio de detecção do chão.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Raio da esfera de detecção do chão.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("Camada (Layer) correspondente ao chão.")]
    [SerializeField] private LayerMask groundLayer;

    // Componentes internos
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private Animator animator;

    // Hashes dos parâmetros do Animator (otimização de desempenho)
    private readonly int isGroundedHash = Animator.StringToHash("isGrounded");
    private readonly int isSlidingHash = Animator.StringToHash("isSliding");
    private readonly int isGroundPoundingHash = Animator.StringToHash("isGroundPounding");
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");

    // Corrotinas de controle
    private Coroutine attackCoroutine;
    private Coroutine pendingAirAttackCoroutine;

    // Estados de movimento
    private bool isGrounded;
    private bool jumpRequested;
    private bool isSliding;
    private bool isGroundPounding;
    private bool isAttacking;

    public bool IsGroundPounding => isGroundPounding;

    // Controle de toque e gestos
    private float lastAirTapTime = -10f;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector2 startTouchPos;
    private Vector2 currentTouchPos;
    private bool isTouching;
    private bool consumedByGroundPound = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();

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
        // 1. Verificação de Chão
        if (groundCheck != null)
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (isGrounded && !wasGrounded)
            {
                isGroundPounding = false;
                consumedByGroundPound = false;
                CancelPendingAirAttack();
            }
        }

        // 2. Atualização dos parâmetros do Animator
        UpdateAnimator();

        // 3. Leitura contínua de Entrada
        HandleInputLifecycle();
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

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isSlidingHash, isSliding);
        animator.SetBool(isGroundPoundingHash, isGroundPounding);
    }

    private void HandleInputLifecycle()
    {
        // Mobile Touch
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                ProcessTouchStart(touch.screenPosition);
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                currentTouchPos = touch.screenPosition;
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                currentTouchPos = touch.screenPosition;
                ProcessTouchEnd();
            }
            return;
        }

        // Mouse (Editor / PC)
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessTouchStart(Mouse.current.position.ReadValue());
            }
            else if (Mouse.current.leftButton.isPressed)
            {
                currentTouchPos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && isTouching)
            {
                currentTouchPos = Mouse.current.position.ReadValue();
                ProcessTouchEnd();
            }
        }
    }

    private void ProcessTouchStart(Vector2 position)
    {
        startTouchPos = position;
        currentTouchPos = position;
        isTouching = true;

        if (!isGrounded)
        {
            float timeSinceLastTap = Time.time - lastAirTapTime;
            if (timeSinceLastTap <= doubleTapThreshold)
            {
                consumedByGroundPound = true;
                CancelPendingAirAttack();
                ExecuteGroundPound();
                lastAirTapTime = -10f;
                return;
            }
            lastAirTapTime = Time.time;
        }
    }

    private void ProcessTouchEnd()
    {
        isTouching = false;
        EvaluateGesture();
    }

    private void EvaluateGesture()
    {
        if (consumedByGroundPound)
        {
            consumedByGroundPound = false;
            return;
        }

        if (isGroundPounding) return;

        float deltaY = currentTouchPos.y - startTouchPos.y;

        // Pulo (Swipe Up)
        if (deltaY >= minSwipeDistance)
        {
            CancelPendingAirAttack();
            jumpRequested = true;
        }
        // Slide (Swipe Down)
        else if (deltaY <= -minSwipeDistance)
        {
            CancelPendingAirAttack();
            StartSlide();
        }
        // Toque Simples (Ataque)
        else if (Mathf.Abs(deltaY) < minSwipeDistance)
        {
            if (isGrounded)
            {
                TriggerAttack();
            }
            else
            {
                CancelPendingAirAttack();
                pendingAirAttackCoroutine = StartCoroutine(WaitAirAttackRoutine());
            }
        }
    }

    private IEnumerator WaitAirAttackRoutine()
    {
        yield return new WaitForSeconds(doubleTapThreshold);

        if (!isGroundPounding && !consumedByGroundPound)
        {
            TriggerAttack();
        }
        pendingAirAttackCoroutine = null;
    }

    private void CancelPendingAirAttack()
    {
        if (pendingAirAttackCoroutine != null)
        {
            StopCoroutine(pendingAirAttackCoroutine);
            pendingAirAttackCoroutine = null;
        }
    }

    public void TriggerAttack()
    {
        if (isAttacking || isGroundPounding) return;
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        if (animator != null)
        {
            animator.SetTrigger(attackTriggerHash);
        }

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(true);
        }

        yield return new WaitForSeconds(attackDuration);

        if (attackHitboxObject != null)
        {
            attackHitboxObject.SetActive(false);
        }
        isAttacking = false;
    }

    public void Bounce()
    {
        isGroundPounding = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
    }

    private void ExecuteGroundPound()
    {
        if (isGroundPounding) return;
        isGroundPounding = true;

        CancelPendingAirAttack();

        if (isAttacking)
        {
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            if (attackHitboxObject != null) attackHitboxObject.SetActive(false);
            isAttacking = false;
        }

        if (isSliding)
        {
            StopAllCoroutines();
            ResetCollider();
            isSliding = false;
        }

        rb.linearVelocity = new Vector2(runSpeed, -groundPoundForce);
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